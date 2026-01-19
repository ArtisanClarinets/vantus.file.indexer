using CommunityToolkit.Mvvm.ComponentModel;
using Vantus.Core.Interfaces;
using Vantus.Core.Models;

namespace Vantus.App.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ISettingsRegistry _registry;

    public ShellViewModel(ISettingsRegistry registry)
    {
        _registry = registry;
    }

    public IEnumerable<SettingDefinition> Search(string query)
    {
        return _registry.Search(query);
    }
}
