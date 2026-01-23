using Vantus.Engine.Services.Ai;
using Xunit;

namespace Vantus.Tests;

public class AiTests
{
    [Fact]
    public async Task KeywordAiModel_GenerateEmbeddingsAsync_ReturnsDeterministicVector()
    {
        var model = new KeywordAiModel();
        var content1 = "This is a test document.";
        var content2 = "This is a test document.";
        var content3 = "Different document content.";

        var embedding1 = await model.GenerateEmbeddingsAsync(content1);
        var embedding2 = await model.GenerateEmbeddingsAsync(content2);
        var embedding3 = await model.GenerateEmbeddingsAsync(content3);

        Assert.NotNull(embedding1);
        Assert.Equal(64, embedding1.Length);

        // Deterministic
        Assert.Equal(embedding1, embedding2);

        // Different content -> Different vector (high probability)
        Assert.NotEqual(embedding1, embedding3);
    }

    [Fact]
    public async Task KeywordAiModel_GenerateEmbeddingsAsync_HandlesEmpty()
    {
        var model = new KeywordAiModel();
        var embedding = await model.GenerateEmbeddingsAsync("");
        Assert.NotNull(embedding);
        Assert.Equal(64, embedding.Length);
        Assert.All(embedding, f => Assert.Equal(0f, f));
    }
}
