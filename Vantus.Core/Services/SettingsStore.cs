using System.Text.Json;
using Vantus.Core.Interfaces;
using Vantus.Core.Models;

namespace Vantus.Core.Services;

public class SettingsStore : ISettingsStore
{
    private const string FolderName = "Vantus";
    private const string FileName = "settings.json";
    private SettingsFileModel _model = new();
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public event EventHandler<string>? SettingChanged;

    private string GetPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(localAppData, FolderName);
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, FileName);
    }

    public async Task LoadAsync()
    {
        var path = GetPath();
        if (File.Exists(path))
        {
            try
            {
                var json = await File.ReadAllTextAsync(path);
                _model = JsonSerializer.Deserialize<SettingsFileModel>(json, _options) ?? new();
            }
            catch
            {
                _model = new();
            }
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var path = GetPath();
            var json = JsonSerializer.Serialize(_model, _options);
            await File.WriteAllTextAsync(path, json);
        }
        catch { }
    }

    public T? GetValue<T>(string key)
    {
        if (_model.Settings.TryGetValue(key, out var val))
        {
            if (val is JsonElement element)
            {
                try { return element.Deserialize<T>(); } catch { }
            }
            if (val is T tVal) return tVal;
            try {
                 return (T)Convert.ChangeType(val, typeof(T));
            } catch { }
        }
        return default;
    }

    public void SetValue<T>(string key, T value)
    {
        bool changed = false;
        if (value == null)
        {
            if (_model.Settings.ContainsKey(key))
            {
                _model.Settings.Remove(key);
                changed = true;
            }
        }
        else
        {
            if (!_model.Settings.ContainsKey(key) || !_model.Settings[key].Equals(value))
            {
                _model.Settings[key] = value;
                changed = true;
            }
        }

        if (changed)
        {
            _ = SaveAsync();
            SettingChanged?.Invoke(this, key);
        }
    }

    public bool HasValue(string key) => _model.Settings.ContainsKey(key);

    public void Reset()
    {
        _model = new();
        _ = SaveAsync();
        // Notify all?
    }

    public IDictionary<string, object> GetAllSettings() => _model.Settings;

    public void Import(IDictionary<string, object> settings)
    {
        foreach(var kvp in settings)
        {
            _model.Settings[kvp.Key] = kvp.Value;
            SettingChanged?.Invoke(this, kvp.Key);
        }
        _ = SaveAsync();
    }
}
