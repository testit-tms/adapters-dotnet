namespace Tms.Adapter.Core.Models;

public class TestContainer : ExecutableItem
{
    public string Id { get; set; } = null!;
    public string ExternalId { get; set; } = null!;
    public string? Title { get; set; }
    public List<string> WorkItemIds { get; set; } = new ();
    public string ClassName { get; set; } = null!;
    public string Namespace { get; set; } = null!;
    public List<string> Labels { get; set; } = new ();
    public List<Link> Links { get; set; } = new ();
    public List<Link> ResultLinks { get; set; } = new ();
    public string? Message { get; set; }
    public string? Trace { get; set; }
    public string? ExternalKey { get; set; }
}