using System.Text.Json.Serialization;

namespace Vantus.Core.Models;

public class PolicyFile
{
    [JsonPropertyName("managed")]
    public bool Managed { get; set; }

    [JsonPropertyName("locks")]
    public List<PolicyLock> Locks { get; set; } = new();
}

public class PolicyLock
{
    [JsonPropertyName("setting_id")]
    public string SettingId { get; set; } = string.Empty;

    [JsonPropertyName("locked_value")]
    public object? LockedValue { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
}
