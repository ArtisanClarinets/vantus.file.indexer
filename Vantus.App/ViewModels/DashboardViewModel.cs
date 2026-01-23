using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Vantus.Core.Engine;

namespace Vantus.App.ViewModels;

public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly IEngineClient _engine;
    private readonly PeriodicTimer _timer;
    private CancellationTokenSource _cts = new();

    [ObservableProperty] private string _status = "Unknown";
    [ObservableProperty] private long _indexedCount;
    [ObservableProperty] private bool _isCrawling;
    [ObservableProperty] private string _statusMessage = "Initializing...";

    public DashboardViewModel(IEngineClient engine)
    {
        _engine = engine;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        _ = PollStatusAsync();
    }

    private async Task PollStatusAsync()
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                try
                {
                    var status = await _engine.GetIndexStatusAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Status = status.State;
                        IndexedCount = status.IndexedCount;
                        IsCrawling = status.IsCrawling;
                        StatusMessage = IsCrawling ? "Indexing files..." : "System Idle";
                    });
                }
                catch {}
            }
        }
        catch (OperationCanceledException) { }
    }

    [RelayCommand]
    private async Task Undo()
    {
        await _engine.UndoLastActionAsync();
        StatusMessage = "Undo command sent.";
    }

    public void Dispose()
    {
        _cts.Cancel();
        _timer.Dispose();
    }
}
