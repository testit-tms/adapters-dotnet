namespace Tms.Adapter.Models;

public class Link : IEquatable<Link>
{
    public LinkType? Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;


    public override bool Equals(object? obj) => Equals(obj as Link);

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Title, Url, Description);
    }

    public bool Equals(Link? other)
    {
        return Type == other?.Type && Title == other?.Title && Url == other.Url && Description == other.Description;
    }
}