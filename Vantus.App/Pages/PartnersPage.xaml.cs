using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vantus.App.ViewModels;

namespace Vantus.App.Pages;

public sealed partial class PartnersPage : Page
{
    public PartnersPageViewModel ViewModel { get; }

    public PartnersPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<PartnersPageViewModel>();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PartnerItem item)
        {
            ViewModel.RemovePartner(item);
        }
    }
}
