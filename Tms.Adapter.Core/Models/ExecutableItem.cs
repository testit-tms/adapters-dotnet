namespace Tms.Adapter.Core.Models;

public abstract class ExecutableItem
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public Status Status { get; set; }

    public Stage Stage { get; set; }

    public List<StepResult> Steps { get; set; } = [];

    public List<string> Attachments { get; set; } = [];

    public Dictionary<string, string> Parameters { get; set; } = [];

    public long Start { get; set; }

    public long Stop { get; set; }
}