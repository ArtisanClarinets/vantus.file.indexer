using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vantus.App.ViewModels;

namespace Vantus.App.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }
    private System.Threading.Timer? _timer;

    public DashboardPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<DashboardViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.RefreshStatusAsync();
        // Auto-refresh every 2 seconds
        _timer = new System.Threading.Timer(async _ =>
        {
            // Marshal to UI thread if needed? Mvvm toolkit usually handles property change on UI thread? No.
            // DispatcherQueue needed.
            this.DispatcherQueue.TryEnqueue(async () => await ViewModel.RefreshStatusAsync());
        }, null, 2000, 2000);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _timer?.Dispose();
    }
}
