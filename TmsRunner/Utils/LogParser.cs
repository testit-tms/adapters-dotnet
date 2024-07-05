using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using Newtonsoft.Json;

using System.Text.RegularExpressions;
using Tms.Adapter.Attributes;
using Tms.Adapter.Models;
using Tms.Adapter.Utils;
using TmsRunner.Entities;
using TmsRunner.Entities.AutoTest;
using TmsRunner.Extensions;

namespace TmsRunner.Utils;

public sealed class LogParser(Replacer replacer)
{
    public static Dictionary<string, string>? GetParameters(string traceJson)
    {
        var pattern = $"{MessageType.TmsParameters}:\\s*([^\\n\\r]*)";
        var regex = new Regex(pattern);
        var match = regex.Match(traceJson);
        var json = match.Groups[1].Value;

        return string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
    }

    public static string GetMessage(string traceJson)
    {
        var pattern = $"{MessageType.TmsStepMessage}:\\s*([^\\n\\r]*)";
        var messageRegex = new Regex(pattern);
        var messages = messageRegex.Matches(traceJson).Select(m => m.Groups[1].Value);
        var message = string.Join(Environment.NewLine, messages);

        return message;
    }

    public static List<Link>? GetLinks(string traceJson)
    {
        var pattern = $"{MessageType.TmsStepLinks}:\\s*\\[([^\\n\\r]*)\\]";
        var regex = new Regex(pattern);
        var matches = regex.Matches(traceJson);

        if (matches.Count == 0)
        {
            return null;
        }

        var linksJsonArray = $"[ {string.Join(", ", matches.Select(m => m.Groups[1].Value))} ]";

        return JsonConvert.DeserializeObject<List<Link>>(linksJsonArray);
    }

    // TODO: write unit tests
    public AutoTest GetAutoTest(TestResult testResult, Dictionary<string, string>? parameters)
    {
        var methodFullName = GetFullyQualifiedMethodName(testResult.TestCase.FullyQualifiedName);
        var method = Reflector.GetMethodMetadata(testResult.TestCase.Source, methodFullName, parameters);

        var autoTest = new AutoTest
        {
            Namespace = method.Namespace,
            Classname = method.Classname,
            MethodName = method.Name
        };

        var attributes = method.Attributes ?? [];

        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case ExternalIdAttribute externalId:
                    autoTest.ExternalId = replacer.ReplaceParameters(externalId.Value, parameters);
                    break;
                case DisplayNameAttribute displayName:
                    autoTest.Name = replacer.ReplaceParameters(displayName.Value, parameters);
                    break;
                case TitleAttribute title:
                    autoTest.Title = replacer.ReplaceParameters(title.Value, parameters);
                    break;
                case DescriptionAttribute description:
                    autoTest.Description = replacer.ReplaceParameters(description.Value, parameters);
                    break;
                case WorkItemIdsAttribute ids:
                    {
                        var workItemIds = ids.Value?
                            .Select(id => replacer.ReplaceParameters(id, parameters))
                            .ToList();

                        autoTest.WorkItemIds = workItemIds ?? [];
                        break;
                    }
                case LinksAttribute links:
                    {
                        if (links.Value is not null)
                        {
                            links.Value.Title = replacer.ReplaceParameters(links.Value.Title, parameters);
                            links.Value.Url = replacer.ReplaceParameters(links.Value.Url, parameters);
                            links.Value.Description =
                                replacer.ReplaceParameters(links.Value.Description, parameters);

                            autoTest.Links?.Add(links.Value);
                        }

                        break;
                    }
                case LabelsAttribute labels:
                    {
                        autoTest.Labels = labels.Value;
                        break;
                    }
            }
        }

        if (string.IsNullOrEmpty(autoTest.ExternalId))
        {
            autoTest.ExternalId = methodFullName.ComputeHash();
        }

        if (string.IsNullOrEmpty(autoTest.Name))
        {
            autoTest.Name = method.Name;
        }

        return autoTest;
    }

    // TODO: write unit tests
    public static List<MessageMetadata> GetMessages(string traceJson)
    {
        var messages = new List<MessageMetadata>();

        const string pattern = "([^\\n\\r\\:]*): \\s*([^\\n\\r]*)";
        var regex = new Regex(pattern);
        var matches = regex.Matches(traceJson);

        foreach (Match match in matches)
        {
            if (Enum.TryParse(match.Groups[1].Value, true, out MessageType type))
            {
                messages.Add(new MessageMetadata
                {
                    Type = type,
                    Value = match.Groups[2].Value
                });
            }
        }

        return messages;
    }

    private static string GetFullyQualifiedMethodName(string testName)
    {
        const string pattern = "([^(]*)";

        var regex = new Regex(pattern);
        var fullyQualifiedNameArray = regex
            .Matches(testName)[0]
            .Groups[0].Value;

        return fullyQualifiedNameArray;
    }
}