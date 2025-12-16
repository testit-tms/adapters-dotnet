namespace Tms.Adapter.Core.Models;

public class ClassContainer
{
    public string Id { get; set; } = string.Empty;
    public List<FixtureResult> Befores { get; set; } = new ();
    public List<FixtureResult> Afters { get; set; } = new ();
    public List<string> Children { get; set; } = new ();
    public long Start { get; set; }
    public long Stop { get; set; }
}