using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vantus.App.ViewModels;

namespace Vantus.App.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; }

    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<SettingsPageViewModel>();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string pageId)
        {
            ViewModel.Load(pageId);
        }
        else if (e.Parameter is NavigationParameter navParam)
        {
            ViewModel.Load(navParam.PageId);
            // Future: Scroll to navParam.HighlightSettingId
        }
    }
}
