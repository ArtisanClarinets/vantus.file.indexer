using Microsoft.Extensions.Logging;
using System.Text.Json;
using Vantus.Core.Interfaces;
using Vantus.Core.Models;

namespace Vantus.Core.Services;

public class PolicyEngine : IPolicyEngine
{
    private Dictionary<string, PolicyLock> _locks = new();
    private readonly ILogger<PolicyEngine> _logger;
    private bool _managed;

    public PolicyEngine(ILogger<PolicyEngine> logger)
    {
        _logger = logger;
    }

    public bool IsManaged => _managed;

    public async Task InitializeAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "policies.json");

        if (!File.Exists(path))
        {
             // Try looking up one level (sometimes needed in tests or debug)
             var altPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "policies.json");
             if (File.Exists(altPath)) path = altPath;
             else if (File.Exists("policies.json")) path = "policies.json";
             else
             {
                 _logger.LogInformation("Policy file not found.");
                 return;
             }
        }

        if (File.Exists(path))
        {
            try
            {
                var json = await File.ReadAllTextAsync(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var policy = JsonSerializer.Deserialize<PolicyFile>(json, options);
                if (policy != null && policy.Managed)
                {
                    _managed = true;
                    _locks = policy.Locks.ToDictionary(l => l.SettingId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load policy file.");
            }
        }
    }

    public PolicyLock? GetLock(string settingId)
    {
        return _locks.TryGetValue(settingId, out var l) ? l : null;
    }

    public bool IsLocked(string settingId)
    {
        return _locks.ContainsKey(settingId);
    }
}
