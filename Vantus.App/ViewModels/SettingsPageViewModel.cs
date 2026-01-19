using CommunityToolkit.Mvvm.ComponentModel;
using Vantus.Core.Interfaces;
using Vantus.Core.Models;

namespace Vantus.App.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly ISettingsRegistry _registry;
    private readonly ISettingsStore _store;
    private readonly IPolicyEngine _policy;

    [ObservableProperty]
    private string _pageTitle = string.Empty;

    [ObservableProperty]
    private List<SettingGroup> _groups = new();

    public SettingsPageViewModel(ISettingsRegistry registry, ISettingsStore store, IPolicyEngine policy)
    {
        _registry = registry;
        _store = store;
        _policy = policy;
    }

    public void Load(string pageId)
    {
        // Cleanup old?
        if (Groups != null)
        {
            foreach(var g in Groups)
                foreach(var s in g.Settings)
                    s.Dispose();
        }

        var defs = _registry.GetDefinitionsByPage(pageId);
        if (!defs.Any()) return;

        PageTitle = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(pageId.Replace("_", " "));

        var grouped = defs.GroupBy(d => d.Section);
        var groups = new List<SettingGroup>();

        foreach(var g in grouped)
        {
            var settingVMs = g.Select(d => new SettingViewModel(d, _store, _policy.GetLock(d.SettingId))).ToList();
            groups.Add(new SettingGroup(g.Key, settingVMs));
        }

        Groups = groups;
    }
}

public class SettingGroup
{
    public string Header { get; }
    public List<SettingViewModel> Settings { get; }

    public SettingGroup(string header, List<SettingViewModel> settings)
    {
        Header = header;
        Settings = settings;
    }
}

public class SettingViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsStore _store;
    public SettingDefinition Definition { get; }
    public PolicyLock? Lock { get; }

    public SettingViewModel(SettingDefinition def, ISettingsStore store, PolicyLock? policyLock)
    {
        Definition = def;
        _store = store;
        Lock = policyLock;
        _store.SettingChanged += OnSettingChanged;
    }

    private void OnSettingChanged(object? sender, string key)
    {
        if (key == Definition.SettingId)
        {
             // Marshaling to UI thread is responsibility of the view or specific service.
             // Here we just notify.
             OnPropertyChanged(nameof(Value));
        }
    }

    public object? Value
    {
        get
        {
            if (Lock != null) return Lock.LockedValue;
            return _store.GetValue<object>(Definition.SettingId);
        }
        set
        {
            if (Lock != null) return;
            // Optimistic update
            _store.SetValue(Definition.SettingId, value);
            OnPropertyChanged();
        }
    }

    public bool IsLocked => Lock != null;
    public string LockReason => Lock?.Reason ?? "";

    public void Dispose()
    {
        _store.SettingChanged -= OnSettingChanged;
    }
}
