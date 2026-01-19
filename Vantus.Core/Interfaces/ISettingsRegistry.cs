using Vantus.Core.Models;

namespace Vantus.Core.Interfaces;

public interface ISettingsRegistry
{
    Task InitializeAsync();
    IEnumerable<SettingDefinition> GetAllDefinitions();
    IEnumerable<SettingDefinition> GetDefinitionsByPage(string pageId);
    SettingDefinition? GetDefinition(string settingId);
    IEnumerable<SettingDefinition> Search(string query);
}
