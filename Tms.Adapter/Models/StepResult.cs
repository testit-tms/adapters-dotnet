namespace Tms.Adapter.Models;

public class StepResult
{
#pragma warning disable CA1720
    public Guid Guid { get; set; }
#pragma warning restore CA1720
    public DateTime? CompletedOn { get; set; }
    public long Duration { get; set; }
    public string? Result { get; set; }
    public string Outcome { get; set; }
}