using System.Text;
using Tms.Adapter.Core.Models;

namespace Tms.Adapter.Core.Service;

public static class Adapter
{
    public static void AddMessage(string message)
    {
        AdapterManager.Instance.AddMessage(message);
    }

    public static void AddLinks(params Link[] links)
    {
        AdapterManager.Instance.AddLinks(links);
    }

    public static void AddLinks(string url, string? title = null, string? description = null,
        LinkType? type = null)
    {
        AddLinks(new Link(url, title, description, type));
    }

    public static void AddAttachments(string pathToFile)
    {
        using var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read);
        AdapterManager.Instance.AddAttachments(Path.GetFileName(pathToFile), fs);
    }

    public static void AddAttachments(IEnumerable<string> pathToFile)
    {
        foreach (var path in pathToFile)
        {
            AddAttachments(path);
        }
    }

    public static void AddAttachments(string content, string filename)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

        AdapterManager.Instance.AddAttachments(filename, ms);
    }
}