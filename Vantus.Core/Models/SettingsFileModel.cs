using System.Text.Json.Serialization;

namespace Vantus.Core.Models;

public class SettingsFileModel
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("settings")]
    public Dictionary<string, object> Settings { get; set; } = new();
}
