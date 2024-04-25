namespace Tms.Adapter.Models;

public class StepResult
{
    public Guid Guid { get; set; }
    public DateTime? CompletedOn { get; set; }
    public long Duration { get; set; }
    public string? Result { get; set; }
    public string Outcome { get; set; }
}