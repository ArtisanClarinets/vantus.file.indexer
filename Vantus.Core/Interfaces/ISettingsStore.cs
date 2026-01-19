namespace Vantus.Core.Interfaces;

public interface ISettingsStore
{
    Task LoadAsync();
    Task SaveAsync();
    T? GetValue<T>(string key);
    void SetValue<T>(string key, T value);
    bool HasValue(string key);
    void Reset();
    IDictionary<string, object> GetAllSettings();
    void Import(IDictionary<string, object> settings);
    event EventHandler<string> SettingChanged;
}
