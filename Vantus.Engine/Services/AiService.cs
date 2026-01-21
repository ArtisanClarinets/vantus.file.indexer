using Dapper;
using Microsoft.Extensions.Logging;
using Vantus.Engine.Services.Ai;

namespace Vantus.Engine.Services;

public class AiService
{
    private readonly TagService _tagService;
    private readonly ILogger<AiService> _logger;
    private readonly IAiModel _model;

    public AiService(TagService tagService, ILogger<AiService> logger)
    {
        _tagService = tagService;
        _logger = logger;
        // Default to Basic model. In future, load from settings or factory.
        _model = new KeywordAiModel();
    }

    public async Task InitializeAsync()
    {
        await _model.InitializeAsync();
    }

    public async Task ProcessFileAsync(string filePath, string content)
    {
        try
        {
            // Classification
            var tags = await _model.ClassifyAsync(content);
            foreach (var tag in tags)
            {
                await _tagService.TagFileAsync(filePath, tag, 0.85); // 0.85 confidence
                _logger.LogInformation("AI Tagged {Path} with {Tag} (Model: {Model})", filePath, tag, _model.Name);
            }

            // Embeddings (Placeholder for future vector search)
            var embeddings = await _model.GenerateEmbeddingsAsync(content);
            if (embeddings.Length > 0)
            {
                // TODO: Save to database when vector search is enabled
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running AI processing for {Path}", filePath);
        }
    }
}
