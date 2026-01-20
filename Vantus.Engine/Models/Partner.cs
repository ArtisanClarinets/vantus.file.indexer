namespace Vantus.Engine.Models;

public class Partner
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Domains { get; set; } = string.Empty; // e.g. "acme.com, acme.org"
    public string Keywords { get; set; } = string.Empty; // e.g. "Acme Corp, Road Runner"
}
