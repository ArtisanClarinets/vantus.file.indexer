using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vantus.App.ViewModels;

namespace Vantus.App.Pages;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; }

    public ShellPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<ShellViewModel>();
    }

    private void NavView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        foreach(var item in NavView.MenuItems)
        {
             if (item is NavigationViewItem navItem && navItem.Tag != null)
             {
                 NavView.SelectedItem = navItem;
                 if (navItem.Tag.ToString() == "modes_presets")
                     ContentFrame.Navigate(typeof(ModesPage));
                 else if (navItem.Tag.ToString() == "import_export")
                     ContentFrame.Navigate(typeof(ImportExportPage));
                 else
                     ContentFrame.Navigate(typeof(SettingsPage), navItem.Tag);
                 break;
             }
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            var tag = item.Tag as string;
            if (tag != null)
            {
                if (tag == "modes_presets")
                {
                    ContentFrame.Navigate(typeof(ModesPage));
                }
                else if (tag == "import_export")
                {
                    ContentFrame.Navigate(typeof(ImportExportPage));
                }
                else
                {
                    ContentFrame.Navigate(typeof(SettingsPage), tag);
                }
            }
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
             sender.ItemsSource = ViewModel.Search(sender.Text);
        }
    }

    private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is Vantus.Core.Models.SettingDefinition def)
        {
            // Navigate to page and highlight
            ContentFrame.Navigate(typeof(SettingsPage), new NavigationParameter { PageId = def.Page, HighlightSettingId = def.SettingId });

            // Sync NavView selection (optional but good)
            // ... (requires finding the item with Tag == def.Page)
        }
    }
}

public class NavigationParameter
{
    public string PageId { get; set; } = string.Empty;
    public string HighlightSettingId { get; set; } = string.Empty;
}
