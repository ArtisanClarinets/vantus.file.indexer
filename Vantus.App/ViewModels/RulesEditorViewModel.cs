using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using Vantus.Core.Engine;
using Vantus.Core.Models;

namespace Vantus.App.ViewModels;

public partial class RulesEditorViewModel : ObservableObject
{
    private readonly IEngineClient _engine;
    public ObservableCollection<Rule> Rules { get; } = new();

    public RulesEditorViewModel(IEngineClient engine)
    {
        _engine = engine;
        _ = LoadRulesAsync();
    }

    private async Task LoadRulesAsync()
    {
        try
        {
            var rules = await _engine.GetRulesAsync();
            Application.Current.Dispatcher.Invoke(() =>
            {
                Rules.Clear();
                foreach(var r in rules) Rules.Add(r);
            });
        }
        catch {}
    }
}
