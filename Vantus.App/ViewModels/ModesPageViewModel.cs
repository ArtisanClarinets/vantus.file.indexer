using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Vantus.Core.Interfaces;

namespace Vantus.App.ViewModels;

public partial class ModesPageViewModel : ObservableObject
{
    private readonly IPresetManager _presets;
    private readonly ISettingsStore _store;
    private readonly ISettingsRegistry _registry;

    [ObservableProperty]
    private ObservableCollection<string> _availablePresets;

    [ObservableProperty]
    private string _selectedPreset;

    [ObservableProperty]
    private string _activePreset;

    [ObservableProperty]
    private List<DiffGroup> _diffs;

    public ModesPageViewModel(IPresetManager presets, ISettingsStore store, ISettingsRegistry registry)
    {
        _presets = presets;
        _store = store;
        _registry = registry;

        AvailablePresets = new ObservableCollection<string>(_presets.GetAvailablePresets());
        ActivePreset = _presets.GetActivePreset();
        SelectedPreset = ActivePreset;
        Diffs = new List<DiffGroup>();
    }

    [RelayCommand]
    public void PreviewChanges()
    {
        var current = _store.GetAllSettings();
        var diffDict = _presets.GetDiff(SelectedPreset, current);

        var groups = new Dictionary<string, List<DiffItem>>();

        foreach(var kvp in diffDict)
        {
            var def = _registry.GetDefinition(kvp.Key);
            var page = def?.Page ?? "Unknown";
            var label = def?.Label ?? kvp.Key;

            if (!groups.ContainsKey(page)) groups[page] = new List<DiffItem>();

            groups[page].Add(new DiffItem
            {
                Label = label,
                OldValue = kvp.Value.OldValue?.ToString() ?? "null",
                NewValue = kvp.Value.NewValue?.ToString() ?? "null"
            });
        }

        Diffs = groups.Select(g => new DiffGroup
        {
            Page = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(g.Key.Replace("_", " ")),
            Items = g.Value
        }).ToList();
    }

    [RelayCommand]
    public void ApplyPreset()
    {
        _presets.ApplyPreset(SelectedPreset);
        ActivePreset = SelectedPreset;
        Diffs = new List<DiffGroup>(); // Clear
    }
}

public class DiffGroup
{
    public string Page { get; set; } = string.Empty;
    public List<DiffItem> Items { get; set; } = new();
}

public class DiffItem
{
    public string Label { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}
