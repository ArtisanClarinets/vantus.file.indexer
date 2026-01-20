using Moq;
using Vantus.Core.Services;
using Microsoft.Extensions.Logging;
using Xunit;
using Vantus.Core.Models;

namespace Vantus.Tests;

public class SettingsStoreTests : IDisposable
{
    private readonly Mock<ILogger<SettingsStore>> _loggerMock;
    private readonly string _settingsPath;

    public SettingsStoreTests()
    {
        _loggerMock = new Mock<ILogger<SettingsStore>>();

        // Use a temporary file path
        _settingsPath = Path.Combine(Path.GetTempPath(), $"settings_{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_settingsPath)) File.Delete(_settingsPath);
    }

    [Fact]
    public async Task SaveAsync_WritesToFile()
    {
        var store = new SettingsStore(_loggerMock.Object, _settingsPath);
        store.SetValue("test.key", "test_value");

        // SaveAsync is called by SetValue (fire and forget), but we want to ensure it completes.
        // Since SetValue is async void (fire-forget), we might need to wait or call SaveAsync explicitly.
        // SetValue calls SaveAsync but doesn't await it.
        // We can call SaveAsync manually to ensure it's written.
        await store.SaveAsync();

        Assert.True(File.Exists(_settingsPath));
        var content = await File.ReadAllTextAsync(_settingsPath);
        Assert.Contains("test.key", content);
        Assert.Contains("test_value", content);
    }

    [Fact]
    public async Task LoadAsync_ReadsFromFile()
    {
        var json = @"{ ""schema_version"": 1, ""settings"": { ""loaded.key"": ""loaded_value"" } }";
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        await File.WriteAllTextAsync(_settingsPath, json);

        var store = new SettingsStore(_loggerMock.Object, _settingsPath);
        await store.LoadAsync();

        var val = store.GetValue<string>("loaded.key");
        Assert.Equal("loaded_value", val);
    }

    [Fact]
    public async Task GetValue_LogsWarning_OnConversionError()
    {
        var json = @"{ ""schema_version"": 1, ""settings"": { ""bad.int"": ""not_an_int"" } }";
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        await File.WriteAllTextAsync(_settingsPath, json);

        var store = new SettingsStore(_loggerMock.Object, _settingsPath);
        await store.LoadAsync();

        var val = store.GetValue<int>("bad.int"); // Should fail conversion
        Assert.Equal(0, val); // default

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to convert") || v.ToString()!.Contains("Failed to deserialize")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadAsync_LogsError_WhenFileCorrupt()
    {
         Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
         await File.WriteAllTextAsync(_settingsPath, "{ invalid json }");

         var store = new SettingsStore(_loggerMock.Object, _settingsPath);
         await store.LoadAsync();

         _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to load settings file")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

         // Should reset to empty
         Assert.False(store.HasValue("any.key"));
    }
}
