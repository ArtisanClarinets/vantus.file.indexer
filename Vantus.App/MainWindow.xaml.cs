using Microsoft.UI.Xaml;

namespace Vantus.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        Title = "Vantus File Indexer";
        ExtendsContentIntoTitleBar = true;
    }
}
