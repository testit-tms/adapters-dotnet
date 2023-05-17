namespace Tms.Adapter.Core.Models;

public class TestResultContainer
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<FixtureResult> Befores { get; set; } = new ();
    public List<FixtureResult> Afters { get; set; } = new ();
    public List<string> Children { get; set; } = new ();
    public long Start { get; set; }
    public long Stop { get; set; }
    public string Message { get; set; }
    public string Trace { get; set; }
}