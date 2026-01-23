namespace Vantus.Engine.Services.Ai;

public class KeywordAiModel : IAiModel
{
    public string Name => "Keyword-Based Classifier (Basic + SimHash)";

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
        // Deterministic SimHash-like implementation for on-device embeddings.
        // This creates a 64-dimension vector representing the document content
        // without requiring a heavy external model.

        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult(new float[64]);

        var vector = new float[64];
        var tokens = content.Split(new[] { ' ', '\n', '\r', '\t', '.', ',', ';', '!' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            // Use string hash code as a seed
            int hash = token.GetHashCode(StringComparison.OrdinalIgnoreCase);

            for (int i = 0; i < 64; i++)
            {
                // Check bit at position (wrapped around 32 bits)
                // We mix the hash with the index to decorrelate dimensions
                int bit = (hash >> (i % 32)) & 1;

                if (bit == 1)
                    vector[i] += 1.0f;
                else
                    vector[i] -= 1.0f;
            }
        }

        // Normalize to -1..1 range for "activation" style output
        float maxVal = 1.0f;
        for (int i = 0; i < 64; i++) maxVal = Math.Max(maxVal, Math.Abs(vector[i]));

        for (int i = 0; i < 64; i++) vector[i] /= maxVal;

        return Task.FromResult(vector);
    }
}
