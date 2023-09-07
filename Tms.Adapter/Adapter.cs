using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Tms.Adapter.Models;
using File = Tms.Adapter.Models.File;

namespace Tms.Adapter
{
    public static class Adapter
    {
        public static void AddLinks(params Link[] links)
        {
            Console.WriteLine($"{MessageType.TmsStepLinks}: " + JsonSerializer.Serialize(links));
        }

        public static void AddLinks(string url, string? title = null, string? description = null,
            LinkType? type = null)
        {
            Console.WriteLine($"{MessageType.TmsStepLinks}: " + JsonSerializer.Serialize(new List<Link>
            {
                new Link
                {
                    Title = title,
                    Url = url,
                    Description = description,
                    Type = type
                }
            }));

            if (type == LinkType.Defect)
            {
                AddMessage($"There is a bug: {url}");
            }
        }

        public static void AddAttachments(string pathToFile)
        {
            var stackTrace = new StackTrace();
            var memberName = stackTrace.GetFrame(1)
                .GetMethod().Name
                .Replace("$_executor_", string.Empty);

            var fullPath = Path.GetFullPath(pathToFile);

            Console.WriteLine($"{MessageType.TmsStepAttachment}: " +
                              JsonSerializer.Serialize(new Models.File
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
                              JsonSerializer.Serialize(new Models.File
                                  { Name = name, Content = content, CallerMemberName = memberName }));
        }

        public static void AddMessage(string message)
        {
            Console.WriteLine($"{MessageType.TmsStepMessage}: " + message);
        }
    }
}