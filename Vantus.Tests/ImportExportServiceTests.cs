using Moq;
using Vantus.Core.Services;
using Vantus.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Vantus.Tests;

public class ImportExportServiceTests : IDisposable
{
    private readonly Mock<ISettingsStore> _storeMock;
    private readonly Mock<ILogger<ImportExportService>> _loggerMock;
    private readonly string _exportPath;

    public ImportExportServiceTests()
    {
        _storeMock = new Mock<ISettingsStore>();
        _loggerMock = new Mock<ILogger<ImportExportService>>();
        _exportPath = "test_export.json";
        if (File.Exists(_exportPath)) File.Delete(_exportPath);
    }

    public void Dispose()
    {
        if (File.Exists(_exportPath)) File.Delete(_exportPath);
    }

    [Fact]
    public async Task ExportAsync_WritesSettingsToFile()
    {
        var settings = new Dictionary<string, object> { { "key", "value" } };
        _storeMock.Setup(s => s.GetAllSettings()).Returns(settings);

        var service = new ImportExportService(_storeMock.Object, _loggerMock.Object);
        await service.ExportAsync(_exportPath);

        Assert.True(File.Exists(_exportPath));
        var content = await File.ReadAllTextAsync(_exportPath);
        Assert.Contains("key", content);
        Assert.Contains("value", content);
    }

    [Fact]
    public async Task LoadImportAsync_ReadsSettingsFromFile()
    {
        var json = @"{ ""settings"": { ""imported.key"": ""imported_value"" } }";
        await File.WriteAllTextAsync(_exportPath, json);

        var service = new ImportExportService(_storeMock.Object, _loggerMock.Object);
        var result = await service.LoadImportAsync(_exportPath);

        Assert.True(result.ContainsKey("imported.key"));
        Assert.Equal("imported_value", result["imported.key"].ToString());
    }

    [Fact]
    public async Task ExportAsync_LogsError_OnFailure()
    {
        // Invalid path to cause exception
        var invalidPath = "/invalid/path/test.json";

        var service = new ImportExportService(_storeMock.Object, _loggerMock.Object);

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => service.ExportAsync(invalidPath));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to export")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
