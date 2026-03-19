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
        Console.WriteLine($"   Leads daily rows  : {data.LeadsDaily.Count:N0}");
        Console.WriteLine($"   Leads by source   : {data.LeadsSrc.Count:N0}");
        Console.WriteLine($"   Leads by AOI      : {data.LeadsAoi.Count:N0}");
        Console.WriteLine($"   Opportunities     : {data.Opps.Count:N0}");
        Console.WriteLine($"   Data as of        : {data.DataAsOf}");

        Console.WriteLine("3. Generating dashboard HTML...");
        string outFile = Generator.Generate(data);
        Console.WriteLine($"   Saved -> {outFile}");
    }
}
