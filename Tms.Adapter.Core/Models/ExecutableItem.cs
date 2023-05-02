namespace Tms.Adapter.Core.Models;

public abstract class ExecutableItem
{
    public string Name { get; set; }

    public Status Status { get; set; }

    public Stage Stage { get; set; }

    public List<StepResult> Steps { get; set; }

    public List<string> Attachments { get; set; }

    public Dictionary<string, string> Parameters { get; set; }

    public long Start { get; set; }

    public long Stop { get; set; }

    public ExecutableItem()
    {
        Parameters = new Dictionary<string, string>();
        Attachments = new List<string>();
        Steps = new List<StepResult>();
    }
}