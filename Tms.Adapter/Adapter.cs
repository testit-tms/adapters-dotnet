using System.Diagnostics;

using Newtonsoft.Json;

using Tms.Adapter.Models;

namespace Tms.Adapter;

public static class Adapter
{
    public static void AddLinks(params Link[] links)
    {
        Console.WriteLine($"{MessageType.TmsStepLinks}: " + JsonConvert.SerializeObject(links));
    }

    public static void AddLinks(string url, string? title = null, string? description = null,
        LinkType? type = null)
    {
        Console.WriteLine($"{MessageType.TmsStepLinks}: " + JsonConvert.SerializeObject(new List<Link>
        {
            new()
            {
                Title = title,
                Url = url,
                Description = description,
                Type = type
            }
        }));
    }

    public static void AddAttachments(string pathToFile)
    {
        var stackTrace = new StackTrace();
        var memberName = stackTrace.GetFrame(1)
            .GetMethod().Name
            .Replace("$_executor_", string.Empty);

        var fullPath = Path.GetFullPath(pathToFile);

        Console.WriteLine($"{MessageType.TmsStepAttachment}: " +
                          JsonConvert.SerializeObject(new Models.File
                              { PathToFile = fullPath, CallerMemberName = memberName }));
    }

    public static void AddAttachments(IEnumerable<string> pathToFile)
    {
        foreach (var path in pathToFile)
        {
            AddAttachments(path);
        }
    }

    public static void AddAttachments(string content, string name)
    {
        var stackTrace = new StackTrace();
        var memberName = stackTrace.GetFrame(1)
            .GetMethod().Name
            .Replace("$_executor_", string.Empty);

        Console.WriteLine($"{MessageType.TmsStepAttachmentAsText}: " +
                          JsonConvert.SerializeObject(new Models.File
                              { Name = name, Content = content, CallerMemberName = memberName }));
    }

    public static void AddMessage(string message)
    {
        Console.WriteLine($"{MessageType.TmsStepMessage}: " + message);
    }
}