using System.Text.Json;
using Vantus.Core.Interfaces;
using Vantus.Core.Models;

namespace Vantus.Core.Services;

public class SettingsRegistry : ISettingsRegistry
{
    private List<SettingDefinition> _definitions = new();
    private Dictionary<string, SettingDefinition> _byId = new();
    private Dictionary<string, List<SettingDefinition>> _byPage = new();

    public async Task InitializeAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "settings_definitions.json");
        if (!File.Exists(path))
        {
             // Try looking up one level (sometimes needed in tests or debug)
             var altPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "settings_definitions.json");
             if (File.Exists(altPath)) path = altPath;
             else if (File.Exists("settings_definitions.json")) path = "settings_definitions.json";
             else return; // Or throw
        }

        var json = await File.ReadAllTextAsync(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _definitions = JsonSerializer.Deserialize<List<SettingDefinition>>(json, options) ?? new();

        // Build indexes
        _byId = new Dictionary<string, SettingDefinition>();
        foreach(var def in _definitions)
        {
            if(!string.IsNullOrEmpty(def.SettingId))
                _byId[def.SettingId] = def;
        }

        _byPage = _definitions.GroupBy(d => d.Page).ToDictionary(g => g.Key, g => g.ToList());
    }

    public IEnumerable<SettingDefinition> GetAllDefinitions() => _definitions;

    public IEnumerable<SettingDefinition> GetDefinitionsByPage(string pageId)
    {
        return _byPage.TryGetValue(pageId, out var list) ? list : Enumerable.Empty<SettingDefinition>();
    }

    public SettingDefinition? GetDefinition(string settingId)
    {
        return _byId.TryGetValue(settingId, out var def) ? def : null;
    }

    public IEnumerable<SettingDefinition> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<SettingDefinition>();
        query = query.ToLowerInvariant();
        return _definitions.Where(d =>
            d.Label.ToLowerInvariant().Contains(query) ||
            d.HelperText.ToLowerInvariant().Contains(query)
        );
    }
}
