using System.Windows.Controls;
using Vantus.App.ViewModels;

namespace Vantus.App.Views;

public partial class RulesEditor : Page
{
    public RulesEditorViewModel ViewModel { get; }

    public RulesEditor()
    {
        InitializeComponent();
        ViewModel = App.GetService<RulesEditorViewModel>();
        DataContext = ViewModel;
    }
}
