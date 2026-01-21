namespace Vantus.Engine.Services.Ai;

public interface IAiModel
{
    string Name { get; }
    Task InitializeAsync();
    Task<List<string>> ClassifyAsync(string content);
    Task<float[]> GenerateEmbeddingsAsync(string content);
}
