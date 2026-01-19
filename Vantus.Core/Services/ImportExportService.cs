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

    public ImportExportService(ISettingsStore store)
    {
        _store = store;
    }

    public async Task ExportAsync(string path)
    {
        var settings = _store.GetAllSettings();
        var exportModel = new {
             exported_at = DateTime.UtcNow,
             settings = settings
        };
        var json = JsonSerializer.Serialize(exportModel, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<Dictionary<string, object>> LoadImportAsync(string path)
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
        return new Dictionary<string, object>();
    }

    public void ApplyImport(Dictionary<string, object> settings)
    {
        _store.Import(settings);
    }
}
