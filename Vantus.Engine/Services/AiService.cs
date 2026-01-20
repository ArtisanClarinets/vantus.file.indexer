using Dapper;
using Microsoft.Extensions.Logging;

namespace Vantus.Engine.Services;

public class AiService
{
    private readonly TagService _tagService;
    private readonly ILogger<AiService> _logger;

    public AiService(TagService tagService, ILogger<AiService> logger)
    {
        _tagService = tagService;
        _logger = logger;
    }

    public async Task ProcessFileAsync(string filePath, string content)
    {
        // Simulate AI Classification (Keyword based for now to avoid huge deps)
        // In real impl, this would load ONNX Runtime and run a model.

        var tags = ClassifyContent(content);
        foreach (var tag in tags)
        {
            await _tagService.TagFileAsync(filePath, tag, 0.85); // 0.85 confidence
            _logger.LogInformation("AI Tagged {Path} with {Tag}", filePath, tag);
        }

        // Simulate Embeddings (placeholder)
        // Store embeddings in DB if we had vector search (SQLite has vss extension but requires binary load)
    }

    private List<string> ClassifyContent(string content)
    {
        var tags = new List<string>();
        if (string.IsNullOrWhiteSpace(content)) return tags;

        var lower = content.ToLowerInvariant();

        if (lower.Contains("invoice") || lower.Contains("total due")) tags.Add("Finance");
        if (lower.Contains("contract") || lower.Contains("agreement")) tags.Add("Legal");
        if (lower.Contains("meeting") || lower.Contains("minutes")) tags.Add("Meeting");
        if (lower.Contains("c#") || lower.Contains("dotnet")) tags.Add("Development");
        if (lower.Contains("secret") || lower.Contains("confidential")) tags.Add("Sensitive");

        return tags;
    }
}
