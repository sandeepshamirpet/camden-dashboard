using System.Text.Json;

public static class Auth
{
    // Credentials: env vars take priority (GitHub Actions), fall back to hardcoded for local use
    private static string Require(string envVar) =>
        Environment.GetEnvironmentVariable(envVar) is { Length: > 0 } v
            ? v
            : throw new Exception($"Missing required environment variable: {envVar}\nSet it before running.");

    public static async Task<(string token, string instanceUrl)> LoginAsync()
    {
        // All credentials must be set as environment variables.
        // For local use: set SF_CLIENT_ID, SF_CLIENT_SECRET, SF_USERNAME, SF_PASSWORD
        // For GitHub Actions: add them as repository secrets.
        string clientId     = Require("SF_CLIENT_ID");
        string clientSecret = Require("SF_CLIENT_SECRET");
        string username     = Require("SF_USERNAME");
        string password     = Require("SF_PASSWORD");

        using var http = new HttpClient();
        var resp = await http.PostAsync(
            "https://login.salesforce.com/services/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "password",
                ["client_id"]     = clientId,
                ["client_secret"] = clientSecret,
                ["username"]      = username,
                ["password"]      = password
            }));

        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Salesforce auth failed: {(int)resp.StatusCode}\n{body}");

        var json = JsonSerializer.Deserialize<JsonElement>(body);
        return (json.GetProperty("access_token").GetString()!,
                json.GetProperty("instance_url").GetString()!);
    }
}
