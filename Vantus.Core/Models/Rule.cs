namespace Vantus.Core.Models;

public class Rule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConditionType { get; set; } = string.Empty;
    public string ConditionValue { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ActionValue { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
