using Microsoft.UI.Xaml.Controls;
using Vantus.App.ViewModels;

namespace Vantus.App.Pages;

public sealed partial class ModesPage : Page
{
    public ModesPageViewModel ViewModel { get; }

    public ModesPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<ModesPageViewModel>();
    }
}
