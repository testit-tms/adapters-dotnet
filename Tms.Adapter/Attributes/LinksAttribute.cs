using Tms.Adapter.Models;

namespace Tms.Adapter.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
// ReSharper disable once ClassNeverInstantiated.Global
public class LinksAttribute : BaseAttribute<Link>
{
    public LinksAttribute(string url, LinkType type = 0, string? title = null, string? description = null)
    {
        Value = new Link
        {
            Url = url,
            Type = type,
            Title = title!,
            Description = description!
        };
    }
}