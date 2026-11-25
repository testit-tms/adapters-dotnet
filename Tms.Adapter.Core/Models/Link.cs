namespace Tms.Adapter.Core.Models;

public class Link
{
    public string? Title { get; set; }
    public string Url { get; set; }
    public string? Description { get; set; }
    public LinkType? Type { get; set; }

    public Link(string url, string? title = null, string? description = null, LinkType? type = null)
    {
        Url = url;
        // Optional fields: empty → null so POST/PUT store the same value in TMS.
        Title = string.IsNullOrEmpty(title) ? null : title;
        Description = string.IsNullOrEmpty(description) ? null : description;
        Type = type;
    }
}