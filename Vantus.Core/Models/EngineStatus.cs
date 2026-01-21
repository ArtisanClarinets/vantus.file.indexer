namespace Vantus.Core.Models;

public class EngineStatus
{
    public string State { get; set; } = "Unknown";
    public long IndexedCount { get; set; }
    public bool IsCrawling { get; set; }
}
