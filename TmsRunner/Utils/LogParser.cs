using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Tms.Adapter.Attributes;
using Tms.Adapter.Models;
using Tms.Adapter.Utils;
using TmsRunner.Extensions;
using TmsRunner.Models;

namespace TmsRunner.Utils;

public class LogParser
{
    private readonly Replacer _replacer;
    private readonly Reflector _reflector;

    public LogParser(Replacer replacer, Reflector reflector)
    {
        _replacer = replacer;
        _reflector = reflector;
    }

    public Dictionary<string, string>? GetParameters(string traceJson)
    {
        var pattern = $"{MessageType.TmsParameters}:\\s*([^\\n\\r]*)";
        var regex = new Regex(pattern);
        var match = regex.Match(traceJson);
        var json = match.Groups[1].Value;

        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }

    public string GetMessage(string traceJson)
    {
        var pattern = $"{MessageType.TmsStepMessage}:\\s*([^\\n\\r]*)";
        var messageRegex = new Regex(pattern);
        var messages = messageRegex.Matches(traceJson).Select(m => m.Groups[1].Value);
        var message = string.Join(Environment.NewLine, messages);

        return message;
    }

    public List<Link>? GetLinks(string traceJson)
    {
        var pattern = $"{MessageType.TmsStepLinks}:\\s*\\[([^\\n\\r]*)\\]";
        var regex = new Regex(pattern);
        var matches = regex.Matches(traceJson);

        if (matches.Count == 0)
        {
            return null;
        }

        var linksJsonArray = $"[ {string.Join(", ", matches.Select(m => m.Groups[1].Value))} ]";
        
        return JsonSerializer.Deserialize<List<Link>>(linksJsonArray);
    }

    // TODO: write unit tests
    public AutoTest GetAutoTest(TestResult testResult, Dictionary<string, string>? parameters)
    {
        var methodFullName = GetFullyQualifiedMethodName(testResult.TestCase.FullyQualifiedName);
        var method = _reflector.GetMethodMetadata(testResult.TestCase.Source, methodFullName, parameters);

        var autoTest = new AutoTest
        {
            Namespace = method.Namespace,
            Classname = method.Classname,
            MethodName = method.Name
        };

        foreach (var attribute in method.Attributes)
        {
            switch (attribute)
            {
                case ExternalIdAttribute externalId:
                    autoTest.ExternalId = _replacer.ReplaceParameters(externalId.Value, parameters);
                    break;
                case DisplayNameAttribute displayName:
                    autoTest.Name = _replacer.ReplaceParameters(displayName.Value, parameters);
                    break;
                case TitleAttribute title:
                    autoTest.Title = _replacer.ReplaceParameters(title.Value, parameters);
                    break;
                case DescriptionAttribute description:
                    autoTest.Description = _replacer.ReplaceParameters(description.Value, parameters);
                    break;
                case WorkItemIdsAttribute ids:
                {
                    var workItemIds = ids.Value
                        .Select(id => _replacer.ReplaceParameters(id, parameters))
                        .ToList();

                    autoTest.WorkItemIds = workItemIds;
                    break;
                }
                case LinksAttribute links:
                {
                    if (links.Value is not null)
                    {
                        links.Value.Title = _replacer.ReplaceParameters(links.Value.Title, parameters);
                        links.Value.Url = _replacer.ReplaceParameters(links.Value.Url, parameters);
                        links.Value.Description =
                            _replacer.ReplaceParameters(links.Value.Description, parameters);

                        autoTest.Links.Add(links.Value);
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
    public List<MessageMetadata> GetMessages(string traceJson)
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