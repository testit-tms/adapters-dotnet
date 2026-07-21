using Tms.Adapter.Models;

namespace Tms.Adapter.Attributes;

internal sealed class StepInvocation
{
    public Guid Guid { get; set; }
    public DateTime StartedOn { get; set; }
    public CallerMethodType Phase { get; set; }
    public StepInvocation? Parent { get; set; }
    public int CompletionWritten;
}