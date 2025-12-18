using System.Text;
using Tms.Adapter.Models;

namespace TmsRunner.Entities;

public sealed class StepModel : StepDto
{
    public string? Result { get; set; }
    public DateTime? CompletedOn { get; set; }
    public long Duration { get; set; }
    public List<StepModel> Steps { get; set; } = [];
    public StepModel? ParentStep { get; set; }
    public int NestingLevel { get; set; }
    public List<Link> Links { get; set; } = [];
    public List<Guid> Attachments { get; set; } = [];
    public string? Outcome { get; set; }
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