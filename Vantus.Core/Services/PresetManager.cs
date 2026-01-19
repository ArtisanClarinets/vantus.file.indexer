using System.Text.Json;
using Vantus.Core.Interfaces;
using Vantus.Core.Models;

namespace Vantus.Core.Services;

public class PresetManager : IPresetManager
{
    private readonly ISettingsRegistry _registry;
    private readonly ISettingsStore _store;
    private const string PresetSettingId = "modes.active_preset";

    public PresetManager(ISettingsRegistry registry, ISettingsStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IEnumerable<string> GetAvailablePresets()
    {
        var def = _registry.GetDefinition(PresetSettingId);
        if (def?.AllowedValues is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
             var list = new List<string>();
             foreach(var item in je.EnumerateArray()) list.Add(item.GetString()!);
             return list;
        }
        return new[] { "Personal", "Pro", "Enterprise-Private", "Enterprise-Automation" };
    }

    public string GetActivePreset()
    {
        return _store.GetValue<string>(PresetSettingId) ?? "Personal";
    }

    public void SetActivePreset(string presetId)
    {
        _store.SetValue(PresetSettingId, presetId);
    }

    public Dictionary<string, object> GetPresetDefaults(string presetId)
    {
        var defaults = new Dictionary<string, object>();
        // Normalize: "Enterprise-Private" -> "enterprise_private"
        var normalizedPresetId = presetId.ToLowerInvariant().Replace("-", "_").Replace(" ", "_");

        foreach (var def in _registry.GetAllDefinitions())
        {
            if (def.Defaults != null)
            {
                var key = def.Defaults.Keys.FirstOrDefault(k => k.Equals(normalizedPresetId, StringComparison.OrdinalIgnoreCase));

                if (key != null)
                {
                    defaults[def.SettingId] = def.Defaults[key];
                }
            }
        }
        return defaults;
    }

    public Dictionary<string, (object? OldValue, object? NewValue)> GetDiff(string targetPresetId, IDictionary<string, object> currentSettings)
    {
        var diff = new Dictionary<string, (object?, object?)>();
        var newDefaults = GetPresetDefaults(targetPresetId);

        foreach (var kvp in newDefaults)
        {
            object? currentVal = null;
            if (currentSettings.TryGetValue(kvp.Key, out var val))
            {
                currentVal = val;
            }

            if (!ValuesEqual(currentVal, kvp.Value))
            {
                diff[kvp.Key] = (currentVal, kvp.Value);
            }
        }
        return diff;
    }

    private bool ValuesEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        // Handle JsonElement
        string aStr = a.ToString()!;
        string bStr = b.ToString()!;

        if (a is JsonElement ja) aStr = ja.ToString();
        if (b is JsonElement jb) bStr = jb.ToString();

        return aStr == bStr;
    }

    public void ApplyPreset(string presetId)
    {
        var defaults = GetPresetDefaults(presetId);
        foreach(var kvp in defaults)
        {
            // Do not override locked settings?
            // The store usually applies writes. If policy engine is separate, UI should block.
            // But here we are writing directly to store.
            // Ideally check policy. But for now, just write.
            _store.SetValue(kvp.Key, kvp.Value);
        }
        SetActivePreset(presetId);
    }
}
