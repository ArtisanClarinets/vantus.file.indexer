using Microsoft.Extensions.Logging;
using System.Text.Json;
using Vantus.Core.Interfaces;

namespace Vantus.Core.Services;

public interface IImportExportService
{
    Task ExportAsync(string path);
    Task<Dictionary<string, object>> LoadImportAsync(string path);
    void ApplyImport(Dictionary<string, object> settings);
}

public class ImportExportService : IImportExportService
{
    private readonly ISettingsStore _store;
    private readonly ILogger<ImportExportService> _logger;

    public ImportExportService(ISettingsStore store, ILogger<ImportExportService> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task ExportAsync(string path)
    {
        try
        {
            var settings = _store.GetAllSettings();
            var exportModel = new {
                 exported_at = DateTime.UtcNow,
                 settings = settings
            };
            var json = JsonSerializer.Serialize(exportModel, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export settings to {Path}", path);
            throw; // Re-throw to let caller handle/display error
        }
    }

    public async Task<Dictionary<string, object>> LoadImportAsync(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("settings", out var settingsElement))
            {
                 var dict = new Dictionary<string, object>();
                 foreach(var prop in settingsElement.EnumerateObject())
                 {
                     // We need to deserialize to object or keep as JsonElement
                     dict[prop.Name] = prop.Value.Clone();
                 }
                 return dict;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import settings from {Path}", path);
            throw;
        }
        return new Dictionary<string, object>();
    }

    public void ApplyImport(Dictionary<string, object> settings)
    {
        _store.Import(settings);
    }
}
