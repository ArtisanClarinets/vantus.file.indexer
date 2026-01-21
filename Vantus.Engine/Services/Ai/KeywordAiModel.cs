namespace Vantus.Engine.Services.Ai;

public class KeywordAiModel : IAiModel
{
    public string Name => "Keyword-Based Classifier (Basic)";

    public Task InitializeAsync()
    {
        // No-op for keyword model
        return Task.CompletedTask;
    }

    public Task<List<string>> ClassifyAsync(string content)
    {
        var tags = new List<string>();
        if (string.IsNullOrWhiteSpace(content)) return Task.FromResult(tags);

        var lower = content.ToLowerInvariant();

        // Finance
        if (lower.Contains("invoice") || lower.Contains("total due") || lower.Contains("receipt") || lower.Contains("tax"))
            tags.Add("Finance");

        // Legal
        if (lower.Contains("contract") || lower.Contains("agreement") || lower.Contains("nda") || lower.Contains("terms of service"))
            tags.Add("Legal");

        // Meeting
        if (lower.Contains("meeting") || lower.Contains("minutes") || lower.Contains("agenda") || lower.Contains("attendees"))
            tags.Add("Meeting");

        // Development
        if (lower.Contains("c#") || lower.Contains("dotnet") || lower.Contains("python") || lower.Contains("javascript") || lower.Contains("api"))
            tags.Add("Development");

        // Sensitive
        if (lower.Contains("secret") || lower.Contains("confidential") || lower.Contains("password") || lower.Contains("private key"))
            tags.Add("Sensitive");

        // Personal
        if (lower.Contains("resume") || lower.Contains("cv") || lower.Contains("personal") || lower.Contains("diary"))
            tags.Add("Personal");

        return Task.FromResult(tags);
    }

    public Task<float[]> GenerateEmbeddingsAsync(string content)
    {
        // Not supported in Basic model
        return Task.FromResult(Array.Empty<float>());
    }
}
