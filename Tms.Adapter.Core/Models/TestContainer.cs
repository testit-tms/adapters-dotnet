namespace Tms.Adapter.Core.Models;

public class TestContainer : ExecutableItem
{
    public string? Id { get; set; }
    public string? ExternalId { get; set; }
    public string? Title { get; set; }
    public List<string> WorkItemIds { get; set; } = [];
    public string? ClassName { get; set; }
    public string? Namespace { get; set; }
    public List<string> Labels { get; set; } = [];
    public List<Link> Links { get; set; } = [];
    public List<Link> ResultLinks { get; set; } = [];
    public string? Message { get; set; }
    public string? Trace { get; set; }
    public string? ExternalKey { get; set; }
}