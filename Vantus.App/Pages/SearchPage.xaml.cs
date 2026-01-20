using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vantus.App.ViewModels;
using Vantus.Core.Models;

namespace Vantus.App.Pages;

public sealed partial class SearchPage : Page
{
    public SearchPageViewModel ViewModel { get; }

    public SearchPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<SearchPageViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string query)
        {
            await ViewModel.SearchAsync(query);
        }
    }

    private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is FileResult result)
        {
            if (!ViewModel.OpenFile(result))
            {
                var dialog = new ContentDialog
                {
                    Title = "Error Opening File",
                    Content = $"Could not open '{result.Name}'. It may have been moved or deleted.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}
