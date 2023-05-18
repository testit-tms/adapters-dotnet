using Tms.Adapter.Core.Models;

namespace Tms.Adapter.Core.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class LinksAttribute : Attribute, ITmsAttribute
{
    public Link Link { get; }

    public LinksAttribute(string url, string title, string description, LinkType type)
    {
        Link = new Link(url, title, description, type);
    }
    
    public LinksAttribute(string url)
    {
        Link = new Link(url);
    }
}