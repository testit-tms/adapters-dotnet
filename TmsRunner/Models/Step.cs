using System.Text;
using Tms.Adapter.Models;

namespace TmsRunner.Models
{
    public class Step : StepDto
    {
        public string? Result { get; set; }
        public DateTime? CompletedOn { get; set; }
        public long Duration { get; set; }
        public List<Step> Steps { get; set; } = new();
        public Step? ParentStep { get; set; }
        public int NestingLevel { get; set; }
        public List<Link> Links { get; set; } = new();
        public List<Guid> Attachments { get; set; } = new();
        public string Outcome { get; set; }

        string _stackTrace;

        public string StackTrace()
        {
            if (_stackTrace != null) return _stackTrace;

            var sb = new StringBuilder();
            var parent = ParentStep;
            sb.Append(CurrentMethod);

            while (parent != null)
            {
                sb.Insert(0, parent.CurrentMethod + Environment.NewLine);
                parent = parent.ParentStep;
            }

            _stackTrace = sb.ToString();

            return _stackTrace;
        }
    }
}