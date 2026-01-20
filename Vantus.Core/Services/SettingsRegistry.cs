using Microsoft.Extensions.Logging;
using System.Text.Json;
using Vantus.Core.Interfaces;
using Vantus.Core.Models;

namespace Vantus.Core.Services;

public class SettingsRegistry : ISettingsRegistry
{
    private List<SettingDefinition> _definitions = new();
    private Dictionary<string, SettingDefinition> _byId = new();
    private Dictionary<string, List<SettingDefinition>> _byPage = new();
    private readonly ILogger<SettingsRegistry> _logger;
    private readonly string? _customPath;

    public SettingsRegistry(ILogger<SettingsRegistry> logger, string? definitionsPath = null)
    {
        _logger = logger;
        _customPath = definitionsPath;
    }

    public async Task InitializeAsync()
    {
        var path = _customPath ?? Path.Combine(AppContext.BaseDirectory, "settings_definitions.json");

        // If custom path is not provided or file doesn't exist there, fall back to search logic (only if custom path is null)
        if (_customPath == null)
        {
            if (!File.Exists(path))
            {
                 // Try looking up one level (sometimes needed in tests or debug)
                 var altPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "settings_definitions.json");
                 if (File.Exists(altPath)) path = altPath;
                 else if (File.Exists("settings_definitions.json")) path = "settings_definitions.json";
                 else
                 {
                     _logger.LogCritical("settings_definitions.json not found.");
                     return;
                 }
            }
        }
        else if (!File.Exists(path))
        {
             _logger.LogCritical("settings_definitions.json not found at {Path}", path);
             return;
        }

        try
        {
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
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to load or parse settings_definitions.json");
        }
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
