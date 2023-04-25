namespace Tms.Adapter.Core.Models;

public abstract class ExecutableItem
{
    #region Private fields

    public string Name { get; set; }

    public Status Status { get; set; }

    public Stage Stage { get; set; }

    public List<StepResult> Steps { get; set; }

    public List<string> Attachments { get; set; }

    public List<Parameter> Parameters { get; set; }

    public long Start { get; set; }

    public long Stop { get; set; }

    #endregion

    public ExecutableItem()
    {
        Parameters = new List<Parameter>();
        Attachments = new List<string>();
        Steps = new List<StepResult>();
    }
}