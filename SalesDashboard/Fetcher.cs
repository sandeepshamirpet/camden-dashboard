using System.Net.Http.Headers;
using System.Text.Json;

// ── Data records injected into the HTML dashboard ──────────────────────────
public record DailyCount(string Date, int Count);
public record MonthlySrc(string Month, string Src, int Count);
public record MonthlyAoi(string Month, string Aoi, int Count);
public record OppRow(string Date, string Owner, string Stage, string Comm, string Src);

public class DashboardData
{
    public List<DailyCount> LeadsDaily   { get; init; } = new();
    public List<MonthlySrc> LeadsSrc     { get; init; } = new();
    public List<MonthlyAoi> LeadsAoi     { get; init; } = new();
    public List<DailyCount> ApptsDaily   { get; init; } = new();  // appointments by day
    public List<MonthlyAoi> ApptsAoi     { get; init; } = new();  // appointments by month/community
    public List<MonthlySrc> ApptsSrc     { get; init; } = new();  // appointments by month/source
    public List<OppRow>     Opps         { get; init; } = new();
    public string           DataAsOf     { get; init; } = "";
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

        Console.WriteLine("  Fetching daily lead counts...");
        var leadsDaily = await FetchLeadsDailyAsync(http);

        Console.WriteLine("  Fetching monthly leads by source...");
        var leadsSrc = await FetchLeadsBySourceAsync(http);

        Console.WriteLine("  Fetching monthly leads by area of interest...");
        var leadsAoi = await FetchLeadsByAoiAsync(http);

        Console.WriteLine("  Fetching appointment counts by day...");
        var apptsDaily = await FetchApptsDailyAsync(http);

        Console.WriteLine("  Fetching appointments by community...");
        var apptsAoi = await FetchApptsByAoiAsync(http);

        Console.WriteLine("  Fetching appointments by source...");
        var apptsSrc = await FetchApptsBySrcAsync(http);

        Console.WriteLine("  Fetching all opportunities...");
        var opps = await FetchOppsAsync(http);

        string dataAsOf = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CST)
                                      .ToString("MMM dd, yyyy h:mm tt") + " CST";

        return new DashboardData
        {
            LeadsDaily = leadsDaily,
            LeadsSrc   = leadsSrc,
            LeadsAoi   = leadsAoi,
            ApptsDaily = apptsDaily,
            ApptsAoi   = apptsAoi,
            ApptsSrc   = apptsSrc,
            Opps       = opps,
            DataAsOf   = dataAsOf
        };
    }

    // Salesforce aggregate (GROUP BY) queries don't support queryMore() pagination.
    // Querying year-by-year keeps every batch ≤ 366 rows — well under the 2,000 limit.
    private static int StartYear => 2019;
    private static int CurrentYear => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CST).Year;

    // ── 1. Daily lead counts grouped by CST date (year by year) ───────────
    private static async Task<List<DailyCount>> FetchLeadsDailyAsync(HttpClient http)
    {
        var result = new List<DailyCount>();
        for (int yr = StartYear; yr <= CurrentYear; yr++)
        {
            string soql =
                $"SELECT DAY_ONLY(ConvertTimezone(CreatedDate)) d, COUNT(Id) cnt " +
                $"FROM Lead " +
                $"WHERE IsConverted = false " +
                $"AND CALENDAR_YEAR(ConvertTimezone(CreatedDate)) = {yr} " +
                $"GROUP BY DAY_ONLY(ConvertTimezone(CreatedDate)) " +
                $"ORDER BY DAY_ONLY(ConvertTimezone(CreatedDate)) ASC";

            foreach (var r in await QueryBatchAsync(http, soql))
            {
                string date = r.TryGetProperty("d",   out var d) ? (d.GetString() ?? "") : "";
                int    cnt  = r.TryGetProperty("cnt", out var c) ? c.GetInt32() : 0;
                if (!string.IsNullOrEmpty(date)) result.Add(new DailyCount(date, cnt));
            }
        }
        return result;
    }

    // ── 2. Monthly leads by lead source (year by year) ────────────────────
    private static async Task<List<MonthlySrc>> FetchLeadsBySourceAsync(HttpClient http)
    {
        var result = new List<MonthlySrc>();
        for (int yr = StartYear; yr <= CurrentYear; yr++)
        {
            string soql =
                $"SELECT CALENDAR_MONTH(ConvertTimezone(CreatedDate)) mo, " +
                $"LeadSource src, COUNT(Id) cnt " +
                $"FROM Lead " +
                $"WHERE IsConverted = false " +
                $"AND CALENDAR_YEAR(ConvertTimezone(CreatedDate)) = {yr} " +
                $"GROUP BY CALENDAR_MONTH(ConvertTimezone(CreatedDate)), LeadSource " +
                $"ORDER BY CALENDAR_MONTH(ConvertTimezone(CreatedDate)) ASC";

            foreach (var r in await QueryBatchAsync(http, soql))
            {
                int    mo  = r.TryGetProperty("mo",  out var m) ? m.GetInt32()   : 0;
                string src = r.TryGetProperty("src", out var s) ? (s.GetString() ?? "Unknown") : "Unknown";
                int    cnt = r.TryGetProperty("cnt", out var c) ? c.GetInt32()   : 0;
                if (mo > 0) result.Add(new MonthlySrc($"{yr}-{mo:D2}", src, cnt));
            }
        }
        return result;
    }

    // ── 3. Monthly leads by area of interest (year by year) ───────────────
    private static async Task<List<MonthlyAoi>> FetchLeadsByAoiAsync(HttpClient http)
    {
        var result = new List<MonthlyAoi>();
        for (int yr = StartYear; yr <= CurrentYear; yr++)
        {
            string soql =
                $"SELECT CALENDAR_MONTH(ConvertTimezone(CreatedDate)) mo, " +
                $"Area_of_Interest__c aoi, COUNT(Id) cnt " +
                $"FROM Lead " +
                $"WHERE IsConverted = false " +
                $"AND CALENDAR_YEAR(ConvertTimezone(CreatedDate)) = {yr} " +
                $"GROUP BY CALENDAR_MONTH(ConvertTimezone(CreatedDate)), Area_of_Interest__c " +
                $"ORDER BY CALENDAR_MONTH(ConvertTimezone(CreatedDate)) ASC";

            foreach (var r in await QueryBatchAsync(http, soql))
            {
                int    mo  = r.TryGetProperty("mo",  out var m) ? m.GetInt32()   : 0;
                string aoi = r.TryGetProperty("aoi", out var a) ? (a.GetString() ?? "Unknown") : "Unknown";
                int    cnt = r.TryGetProperty("cnt", out var c) ? c.GetInt32()   : 0;
                if (mo > 0) result.Add(new MonthlyAoi($"{yr}-{mo:D2}", string.IsNullOrWhiteSpace(aoi) ? "Unknown" : aoi, cnt));
            }
        }
        return result;
    }

    // ── 4a. Daily appointment counts (year by year) ───────────────────────
    // Field: Appointment_Request_Date_Time__c on Lead object.
    // Wrapped in try/catch — returns empty list if field doesn't exist in this org.
    private static async Task<List<DailyCount>> FetchApptsDailyAsync(HttpClient http)
    {
        var result = new List<DailyCount>();
        try
        {
            for (int yr = StartYear; yr <= CurrentYear; yr++)
            {
                string soql =
                    $"SELECT DAY_ONLY(ConvertTimezone(Appointment_Request_Date_Time__c)) d, COUNT(Id) cnt " +
                    $"FROM Lead " +
                    $"WHERE IsConverted = false " +
                    $"AND Appointment_Request_Date_Time__c != null " +
                    $"AND CALENDAR_YEAR(ConvertTimezone(Appointment_Request_Date_Time__c)) = {yr} " +
                    $"GROUP BY DAY_ONLY(ConvertTimezone(Appointment_Request_Date_Time__c)) " +
                    $"ORDER BY DAY_ONLY(ConvertTimezone(Appointment_Request_Date_Time__c)) ASC";

                foreach (var r in await QueryBatchAsync(http, soql))
                {
                    string date = r.TryGetProperty("d",   out var d) ? (d.GetString() ?? "") : "";
                    int    cnt  = r.TryGetProperty("cnt", out var c) ? c.GetInt32() : 0;
                    if (!string.IsNullOrEmpty(date)) result.Add(new DailyCount(date, cnt));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Appointment daily query failed (field may not exist): {ex.Message.Split('\n')[0]}");
        }
        return result;
    }

    // ── 4b. Monthly appointments by community (year by year) ─────────────
    private static async Task<List<MonthlyAoi>> FetchApptsByAoiAsync(HttpClient http)
    {
        var result = new List<MonthlyAoi>();
        try
        {
            for (int yr = StartYear; yr <= CurrentYear; yr++)
            {
                string soql =
                    $"SELECT CALENDAR_MONTH(ConvertTimezone(Appointment_Request_Date_Time__c)) mo, " +
                    $"Area_of_Interest__c aoi, COUNT(Id) cnt " +
                    $"FROM Lead " +
                    $"WHERE IsConverted = false " +
                    $"AND Appointment_Request_Date_Time__c != null " +
                    $"AND CALENDAR_YEAR(ConvertTimezone(Appointment_Request_Date_Time__c)) = {yr} " +
                    $"GROUP BY CALENDAR_MONTH(ConvertTimezone(Appointment_Request_Date_Time__c)), Area_of_Interest__c " +
                    $"ORDER BY CALENDAR_MONTH(ConvertTimezone(Appointment_Request_Date_Time__c)) ASC";

                foreach (var r in await QueryBatchAsync(http, soql))
                {
                    int    mo  = r.TryGetProperty("mo",  out var m) ? m.GetInt32()   : 0;
                    string aoi = r.TryGetProperty("aoi", out var a) ? (a.GetString() ?? "Unknown") : "Unknown";
                    int    cnt = r.TryGetProperty("cnt", out var c) ? c.GetInt32()   : 0;
                    if (mo > 0) result.Add(new MonthlyAoi($"{yr}-{mo:D2}", string.IsNullOrWhiteSpace(aoi) ? "Unknown" : aoi, cnt));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Appointment by community query failed: {ex.Message.Split('\n')[0]}");
        }
        return result;
    }

    // ── 4c. Monthly appointments by lead source (year by year) ───────────
    private static async Task<List<MonthlySrc>> FetchApptsBySrcAsync(HttpClient http)
    {
        var result = new List<MonthlySrc>();
        try
        {
            for (int yr = StartYear; yr <= CurrentYear; yr++)
            {
                string soql =
                    $"SELECT CALENDAR_MONTH(ConvertTimezone(Appointment_Request_Date_Time__c)) mo, " +
                    $"LeadSource src, COUNT(Id) cnt " +
                    $"FROM Lead " +
                    $"WHERE IsConverted = false " +
                    $"AND Appointment_Request_Date_Time__c != null " +
                    $"AND CALENDAR_YEAR(ConvertTimezone(Appointment_Request_Date_Time__c)) = {yr} " +
                    $"GROUP BY CALENDAR_MONTH(ConvertTimezone(Appointment_Request_Date_Time__c)), LeadSource " +
                    $"ORDER BY CALENDAR_MONTH(ConvertTimezone(Appointment_Request_Date_Time__c)) ASC";

                foreach (var r in await QueryBatchAsync(http, soql))
                {
                    int    mo  = r.TryGetProperty("mo",  out var m) ? m.GetInt32()   : 0;
                    string src = r.TryGetProperty("src", out var s) ? (s.GetString() ?? "Unknown") : "Unknown";
                    int    cnt = r.TryGetProperty("cnt", out var c) ? c.GetInt32()   : 0;
                    if (mo > 0) result.Add(new MonthlySrc($"{yr}-{mo:D2}", string.IsNullOrWhiteSpace(src) ? "Unknown" : src, cnt));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Appointment by source query failed: {ex.Message.Split('\n')[0]}");
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
