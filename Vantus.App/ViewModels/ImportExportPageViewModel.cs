using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vantus.Core.Interfaces;
using Vantus.Core.Services;

namespace Vantus.App.ViewModels;

public partial class ImportExportPageViewModel : ObservableObject
{
    private readonly IImportExportService _ioService;
    private readonly ISettingsStore _store;
    private readonly IPresetManager _presets; // Using for Diff logic
    private readonly ISettingsRegistry _registry;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private List<DiffGroup> _importDiffs = new();

    private Dictionary<string, object> _pendingImport = new();

    public ImportExportPageViewModel(IImportExportService ioService, ISettingsStore store, IPresetManager presets, ISettingsRegistry registry)
    {
        _ioService = ioService;
        _store = store;
        _presets = presets;
        _registry = registry;
    }

    [RelayCommand]
    public async Task ExportSettings()
    {
        // In real app, FileSavePicker. For now, fixed path or user input simulation.
        // We can't use Pickers easily in this context without UI thread and window handle access properly setup.
        // We'll simulate exporting to a known location or temp.
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VantusSettings.json");
        await _ioService.ExportAsync(path);
        StatusMessage = $"Exported to {path}";
    }

    [RelayCommand]
    public async Task ImportSettings()
    {
         // Simulate picking a file
         var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VantusSettings.json");
         if (!File.Exists(path))
         {
             StatusMessage = "No export file found in Documents.";
             return;
         }

         try
         {
             _pendingImport = await _ioService.LoadImportAsync(path);

             // Calculate Diff
             var current = _store.GetAllSettings();
             var diffDict = new Dictionary<string, (object?, object?)>();

             foreach(var kvp in _pendingImport)
             {
                 object? currentVal = null;
                 if (current.TryGetValue(kvp.Key, out var val)) currentVal = val;

                 // reuse simple equality check from logic or just string compare
                 if (currentVal?.ToString() != kvp.Value?.ToString())
                 {
                     diffDict[kvp.Key] = (currentVal, kvp.Value);
                 }
             }

             // Grouping (reuse logic from ModesPageVM ideally, but duplicate for speed)
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
                    OldValue = kvp.Value.Item1?.ToString() ?? "null",
                    NewValue = kvp.Value.Item2?.ToString() ?? "null"
                });
             }

             ImportDiffs = groups.Select(g => new DiffGroup
             {
                Page = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(g.Key.Replace("_", " ")),
                Items = g.Value
             }).ToList();

             StatusMessage = "Previewing import changes. Click Apply to finish.";
         }
         catch (Exception ex)
         {
             StatusMessage = $"Error: {ex.Message}";
         }
    }

    [RelayCommand]
    public void ApplyImport()
    {
        if (_pendingImport != null && _pendingImport.Count > 0)
        {
            _ioService.ApplyImport(_pendingImport);
            ImportDiffs.Clear();
            _pendingImport.Clear();
            StatusMessage = "Settings imported successfully.";
        }
    }
}
