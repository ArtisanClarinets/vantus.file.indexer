using Microsoft.UI.Xaml.Controls;
using Vantus.App.ViewModels;

namespace Vantus.App.Pages;

public sealed partial class ImportExportPage : Page
{
    public ImportExportPageViewModel ViewModel { get; }

    public ImportExportPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<ImportExportPageViewModel>();
    }
}
