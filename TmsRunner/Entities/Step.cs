using System.Text;
using Tms.Adapter.Models;

namespace TmsRunner.Entities;

public sealed class Step : StepDto
{
    public string? Result;
    public DateTime? CompletedOn;
    public long Duration;
    public List<Step> Steps = [];
    public Step? ParentStep;
    public int NestingLevel;
    public List<Link> Links = [];
    public List<Guid> Attachments = [];
    public string? Outcome;
    private string? _stackTrace;

    public string StackTrace()
    {
        if (_stackTrace != null)
        {
            return _stackTrace;
        }

        var sb = new StringBuilder();
        var parent = ParentStep;
        _ = sb.Append(CurrentMethod);

        while (parent != null)
        {
            _ = sb.Insert(0, parent.CurrentMethod + Environment.NewLine);
            parent = parent.ParentStep;
        }

        _stackTrace = sb.ToString();

        return _stackTrace;
    }
}