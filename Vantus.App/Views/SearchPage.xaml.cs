using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using Vantus.App.ViewModels;
using Vantus.Core.Models;

namespace Vantus.App.Views;

public partial class SearchPage : Page
{
    public SearchViewModel ViewModel { get; }

    public SearchPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<SearchViewModel>();
        DataContext = ViewModel;
    }

    private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is FileResult file)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = file.Path,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch { }
        }
    }
}
