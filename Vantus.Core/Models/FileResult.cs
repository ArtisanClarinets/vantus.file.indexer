namespace Vantus.Core.Models;

public class FileResult
{
    public long Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
}
