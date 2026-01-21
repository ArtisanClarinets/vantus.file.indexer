namespace Vantus.Core.Models;

public class EngineStatus
{
    public string State { get; set; } = "Unknown"; // e.g., "Idle", "Indexing", "Paused"
    public int IndexedCount { get; set; }
    public bool IsCrawling { get; set; }
}
