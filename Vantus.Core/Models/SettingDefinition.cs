using System.Text.Json.Serialization;

namespace Vantus.Core.Models;

public class SettingDefinition
{
    [JsonPropertyName("setting_id")]
    public string SettingId { get; set; } = string.Empty;

    [JsonPropertyName("page")]
    public string Page { get; set; } = string.Empty;

    [JsonPropertyName("section")]
    public string Section { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("helper_text")]
    public string HelperText { get; set; } = string.Empty;

    [JsonPropertyName("control_type")]
    public string ControlType { get; set; } = string.Empty;

    [JsonPropertyName("value_type")]
    public string ValueType { get; set; } = string.Empty;

    [JsonPropertyName("allowed_values")]
    public object? AllowedValues { get; set; }

    [JsonPropertyName("defaults")]
    public Dictionary<string, object>? Defaults { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = "global";

    [JsonPropertyName("requires_restart")]
    public bool RequiresRestart { get; set; }

    [JsonPropertyName("policy_lockable")]
    public bool PolicyLockable { get; set; }

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "all";

    [JsonPropertyName("dangerous_action")]
    public bool DangerousAction { get; set; }
}
