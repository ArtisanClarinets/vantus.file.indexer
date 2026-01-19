namespace Vantus.Core.Interfaces;

public interface IPresetManager
{
    IEnumerable<string> GetAvailablePresets();
    string GetActivePreset();
    void SetActivePreset(string presetId);
    Dictionary<string, object> GetPresetDefaults(string presetId);
    Dictionary<string, (object? OldValue, object? NewValue)> GetDiff(string targetPresetId, IDictionary<string, object> currentSettings);
    void ApplyPreset(string presetId);
}
