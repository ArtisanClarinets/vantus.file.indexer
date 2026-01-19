using Vantus.Core.Models;

namespace Vantus.Core.Interfaces;

public interface IPolicyEngine
{
    Task InitializeAsync();
    PolicyLock? GetLock(string settingId);
    bool IsLocked(string settingId);
}
