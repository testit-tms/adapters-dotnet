namespace Tms.Adapter.Core.Models;

public class TestContainer : ExecutableItem
{
    public string Id { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> WorkItemIds { get; set; } = new ();
    public string ClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new ();
    public List<Link> Links { get; set; } = new ();
    public List<Link> ResultLinks { get; set; } = new ();
    public string Message { get; set; } = String.Empty;
    public string? Trace { get; set; }
    public string ExternalKey { get; set; } = string.Empty;
}