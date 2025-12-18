namespace Tms.Adapter.Core.Models;

public class ClassContainer
{
    public string Id { get; set; } = string.Empty;
    public List<FixtureResult> Befores { get; set; } = [];
    public List<FixtureResult> Afters { get; set; } = [];
    public List<string> Children { get; set; } = [];
    public long Start { get; set; }
    public long Stop { get; set; }
}