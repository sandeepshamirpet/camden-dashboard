using System.Net.Http.Headers;
using System.Text.Json;

// ── Data records injected into the HTML dashboard ──────────────────────────
// Raw per-record rows — JS aggregates these with exact date filtering
public record LeadRow(string Date, string Src, string Aoi);
public record ApptRow(string Date, string Aoi, string Src);
public record OppRow(string Date, string Owner, string Stage, string Comm, string Src);

public class DashboardData
{
    public List<LeadRow> Leads   { get; init; } = new();  // one row per lead
    public List<ApptRow> Appts   { get; init; } = new();  // one row per appointment (Event)
    public List<OppRow>  Opps    { get; init; } = new();  // one row per opportunity
    public string        DataAsOf { get; init; } = "";
}

// ── Fetcher ────────────────────────────────────────────────────────────────
public static class Fetcher
{
    private const string API = "/services/data/v58.0/query";

    // Central Time zone (works on both Windows and Linux in .NET 6+)
    private static readonly TimeZoneInfo CST =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Central Standard Time" : "America/Chicago");

    public static async Task<DashboardData> FetchAllAsync(string token, string instanceUrl)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        http.BaseAddress = new Uri(instanceUrl);

        Console.WriteLine("  Fetching all lead records (raw, for exact date filtering)...");
        var leads = await FetchLeadsAsync(http);

        Console.WriteLine("  Fetching appointments from Event object...");
        var appts = await FetchApptsAsync(http);

        Console.WriteLine("  Fetching all opportunities...");
        var opps = await FetchOppsAsync(http);

        string dataAsOf = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CST)
                                      .ToString("MMM dd, yyyy h:mm tt") + " CST";

        return new DashboardData
        {
            Leads    = leads,
            Appts    = appts,
            Opps     = opps,
            DataAsOf = dataAsOf
        };
    }

    // ── 1. All leads — raw records, aggregated in JS for exact date filtering ─
    // Fetching raw records (not GROUP BY) gives JavaScript exact per-day data
    // so every date-range filter (last week, last 10 days, etc.) is precise.
    private static async Task<List<LeadRow>> FetchLeadsAsync(HttpClient http)
    {
        const string soql =
            "SELECT CreatedDate, LeadSource, Area_of_Interest__c " +
            "FROM Lead " +
            "WHERE IsConverted = false " +
            "ORDER BY CreatedDate ASC";

        var records = await QueryAllAsync(http, soql);
        Console.WriteLine($"    → {records.Count} lead records fetched");

        var result = new List<LeadRow>(records.Count);
        foreach (var r in records)
        {
            string rawDt = r.TryGetProperty("CreatedDate", out var dt) ? (dt.GetString() ?? "") : "";
            string date  = UtcToCstDate(rawDt);
            if (string.IsNullOrEmpty(date)) continue;

            string src = "Unknown";
            string aoi = "Unknown";
            if (r.TryGetProperty("LeadSource",         out var s) && s.ValueKind != JsonValueKind.Null)
                src = s.GetString() ?? "Unknown";
            if (r.TryGetProperty("Area_of_Interest__c", out var a) && a.ValueKind != JsonValueKind.Null)
                aoi = a.GetString() ?? "Unknown";

            if (string.IsNullOrWhiteSpace(src)) src = "Unknown";
            if (string.IsNullOrWhiteSpace(aoi)) aoi = "Unknown";

            result.Add(new LeadRow(date, src, aoi));
        }
        return result;
    }

    // ── 4. Appointments from Event object ────────────────────────────────
    // Event.StartDateTime = appointment date/time (CST-converted in C#)
    // Event.Who (polymorphic) → Lead fields: Area_of_Interest__c, LeadSource
    // Returns three aggregated lists: daily counts, by month+community, by month+source
    // ── 3. Appointments — raw Event records, aggregated in JS for exact date filtering ─
    private static async Task<List<ApptRow>> FetchApptsAsync(HttpClient http)
    {
        var result = new List<ApptRow>();
        try
        {
            // TYPEOF Who: when Event is on a Lead, pull community + source fields.
            const string soql =
                "SELECT StartDateTime, " +
                "TYPEOF Who WHEN Lead THEN Area_of_Interest__c, LeadSource END " +
                "FROM Event " +
                "WHERE StartDateTime != null " +
                "ORDER BY StartDateTime ASC";

            var records = await QueryAllAsync(http, soql);
            Console.WriteLine($"    → {records.Count} event records fetched");

            foreach (var r in records)
            {
                string rawDt = r.TryGetProperty("StartDateTime", out var dt) ? (dt.GetString() ?? "") : "";
                string date  = UtcToCstDate(rawDt);
                if (string.IsNullOrEmpty(date)) continue;

                string aoiVal = "Unknown";
                string srcVal = "Unknown";
                if (r.TryGetProperty("Who", out var who) && who.ValueKind == JsonValueKind.Object)
                {
                    if (who.TryGetProperty("Area_of_Interest__c", out var a) && a.ValueKind != JsonValueKind.Null)
                        aoiVal = a.GetString() ?? "Unknown";
                    if (who.TryGetProperty("LeadSource", out var s) && s.ValueKind != JsonValueKind.Null)
                        srcVal = s.GetString() ?? "Unknown";
                }
                if (string.IsNullOrWhiteSpace(aoiVal)) aoiVal = "Unknown";
                if (string.IsNullOrWhiteSpace(srcVal)) srcVal = "Unknown";

                result.Add(new ApptRow(date, aoiVal, srcVal));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Event/Appointment query failed: {ex.Message.Split('\n')[0]}");
        }
        return result;
    }

    // ── 5. All opportunities (raw rows, C# aggregation for flexibility) ────
    private static async Task<List<OppRow>> FetchOppsAsync(HttpClient http)
    {
        // Use CreatedDate (UTC) — we convert to CST in C# for cross-platform reliability
        const string soql =
            "SELECT CreatedDate, Owner.Name, StageName, " +
            "Homes__r.City__c, LeadSource " +
            "FROM Opportunity " +
            "ORDER BY CreatedDate ASC";

        var records = await QueryAllAsync(http, soql);
        var result  = new List<OppRow>(records.Count);

        foreach (var r in records)
        {
            string rawDate = r.TryGetProperty("CreatedDate", out var dt) ? (dt.GetString() ?? "") : "";
            string date    = UtcToCstDate(rawDate);
            if (string.IsNullOrEmpty(date)) continue;

            string owner = "";
            if (r.TryGetProperty("Owner", out var o) && o.ValueKind == JsonValueKind.Object)
                o.TryGetProperty("Name", out var on);
            if (r.TryGetProperty("Owner", out var ow) && ow.ValueKind == JsonValueKind.Object &&
                ow.TryGetProperty("Name", out var owName))
                owner = owName.GetString() ?? "";

            string stage = r.TryGetProperty("StageName", out var st) ? (st.GetString() ?? "") : "";
            string src   = r.TryGetProperty("LeadSource", out var ls) ? (ls.GetString() ?? "Unknown") : "Unknown";

            string comm = "Unknown";
            if (r.TryGetProperty("Homes__r", out var h) && h.ValueKind == JsonValueKind.Object &&
                h.TryGetProperty("City__c", out var city))
                comm = city.GetString() ?? "Unknown";

            result.Add(new OppRow(date,
                                  string.IsNullOrWhiteSpace(owner) ? "Unknown" : owner,
                                  stage,
                                  string.IsNullOrWhiteSpace(comm)  ? "Unknown" : comm,
                                  string.IsNullOrWhiteSpace(src)   ? "Unknown" : src));
        }

        return result;
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    private static string UtcToCstDate(string utcStr)
    {
        // DateTimeOffset.TryParse correctly handles Salesforce's "2026-03-18T19:30:00.000+0000"
        // .UtcDateTime always has DateTimeKind.Utc which ConvertTimeFromUtc requires
        if (!DateTimeOffset.TryParse(utcStr, out var dto))
            return "";
        return TimeZoneInfo.ConvertTimeFromUtc(dto.UtcDateTime, CST).ToString("yyyy-MM-dd");
    }

    // For aggregate (GROUP BY) queries — single batch only, no queryMore()
    private static async Task<List<JsonElement>> QueryBatchAsync(HttpClient http, string soql)
    {
        string url  = $"{API}?q={Uri.EscapeDataString(soql)}";
        var resp    = await http.GetAsync(url);
        var body    = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"SOQL failed: {(int)resp.StatusCode}\nSOQL: {soql}\n{body}");

        var json = JsonSerializer.Deserialize<JsonElement>(body);
        var all  = new List<JsonElement>();
        foreach (var rec in json.GetProperty("records").EnumerateArray())
            all.Add(rec);
        return all;
    }

    // For regular (non-aggregate) queries — follows nextRecordsUrl pages
    private static async Task<List<JsonElement>> QueryAllAsync(HttpClient http, string soql)
    {
        var all = new List<JsonElement>();
        string url = $"{API}?q={Uri.EscapeDataString(soql)}";

        while (true)
        {
            var resp = await http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"SOQL failed: {(int)resp.StatusCode}\nSOQL: {soql}\n{body}");

            var json = JsonSerializer.Deserialize<JsonElement>(body);
            foreach (var rec in json.GetProperty("records").EnumerateArray())
                all.Add(rec);

            if (json.GetProperty("done").GetBoolean()) break;
            url = json.GetProperty("nextRecordsUrl").GetString()!;
        }

        return all;
    }
}
