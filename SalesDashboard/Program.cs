using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Camden Homes Sales Dashboard Generator (Salesforce Direct)");
        Console.WriteLine("============================================================");

        Console.WriteLine("1. Authenticating to Salesforce...");
        var (token, instanceUrl) = await Auth.LoginAsync();
        Console.WriteLine("   Authenticated OK.");

        Console.WriteLine("2. Fetching data from Salesforce...");
        var data = await Fetcher.FetchAllAsync(token, instanceUrl);
        Console.WriteLine($"   Leads (all, incl. converted) : {data.Leads.Count:N0}");
        Console.WriteLine($"   Appointments (Events)        : {data.Appts.Count:N0}");
        Console.WriteLine($"   Opportunities                : {data.Opps.Count:N0}");
        Console.WriteLine($"   Data as of                   : {data.DataAsOf}");

        // Quick spot-check: leads created in the last 7 calendar days (CST)
        var today = DateTime.UtcNow;
        var last7 = data.Leads
            .Where(l => DateTime.TryParse(l.Date, out var d) && (today.Date - d.Date).TotalDays < 7)
            .Count();
        Console.WriteLine($"   Leads in last 7 rolling days : {last7:N0}  (compare to Salesforce 'Last Week')");

        Console.WriteLine("3. Generating dashboard HTML...");
        string outFile = Generator.Generate(data);
        Console.WriteLine($"   Saved -> {outFile}");
    }
}
