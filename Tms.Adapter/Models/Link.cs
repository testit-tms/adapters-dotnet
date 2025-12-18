namespace Tms.Adapter.Models;

public sealed class Link : IEquatable<Link>
{
    public LinkType? Type { get; set; }

    public string? Title { get; set; }

    public string? Url { get; set; }

    public string? Description { get; set; }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Title, Url, Description);
    }
    
    public override bool Equals(object? obj) => Equals(obj as Link);

    public bool Equals(Link? other)
    {
        return Type == other?.Type && Title == other?.Title && Url == other?.Url && Description == other?.Description;
    }
}