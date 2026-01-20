using Moq;
using Vantus.Core.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Vantus.Tests;

public class SettingsRegistryTests : IDisposable
{
    private readonly string _definitionPath;
    private readonly Mock<ILogger<SettingsRegistry>> _loggerMock;

    public SettingsRegistryTests()
    {
        _loggerMock = new Mock<ILogger<SettingsRegistry>>();
        _definitionPath = Path.Combine(Path.GetTempPath(), $"settings_definitions_{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_definitionPath)) File.Delete(_definitionPath);
    }

    [Fact]
    public async Task InitializeAsync_LogsCritical_WhenFileMissing()
    {
        // Use a path that definitely doesn't exist
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid()}.json");
        var registry = new SettingsRegistry(_loggerMock.Object, missingPath);
        await registry.InitializeAsync();

        // Verify logger was called with Critical level
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_LoadsDefinitions_WhenFileExists()
    {
        var json = @"[
          {
            ""setting_id"": ""test.setting"",
            ""page"": ""test_page"",
            ""section"": ""Test"",
            ""label"": ""Test Setting"",
            ""helper_text"": ""Helper"",
            ""control_type"": ""toggle"",
            ""value_type"": ""bool""
          }
        ]";
        await File.WriteAllTextAsync(_definitionPath, json);

        var registry = new SettingsRegistry(_loggerMock.Object, _definitionPath);
        await registry.InitializeAsync();

        var def = registry.GetDefinition("test.setting");
        Assert.NotNull(def);
        Assert.Equal("Test Setting", def!.Label);
    }

    [Fact]
    public async Task InitializeAsync_LogsCritical_WhenFileMalformed()
    {
        await File.WriteAllTextAsync(_definitionPath, "{ invalid json }");

        var registry = new SettingsRegistry(_loggerMock.Object, _definitionPath);
        await registry.InitializeAsync();

         _loggerMock.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to load or parse")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
