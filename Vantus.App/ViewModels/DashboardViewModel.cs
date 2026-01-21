using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vantus.Core.Engine;
using Vantus.Core.Models;

namespace Vantus.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IEngineClient _engine;

    [ObservableProperty]
    private string _status = "Unknown";

    [ObservableProperty]
    private long _indexedCount;

    [ObservableProperty]
    private bool _isIndexing;

    public DashboardViewModel(IEngineClient engine)
    {
        _engine = engine;
    }

    [RelayCommand]
    public async Task RefreshStatusAsync()
    {
        var status = await _engine.GetIndexStatusAsync();
        Status = status.State;
        IndexedCount = status.IndexedCount;
        IsIndexing = status.IsCrawling;
    }

    [RelayCommand]
    public async Task RebuildIndexAsync()
    {
        await _engine.RequestRebuildIndexAsync();
        Status = "Rebuilding...";
        // Poll for updates?
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            await RefreshStatusAsync();
        });
    }

    // Auto-refresh logic could be added here or in the Page code-behind using a timer
}
