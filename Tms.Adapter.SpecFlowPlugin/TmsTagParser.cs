using System.Text.Json;
using System.Text.Json.Nodes;

using Newtonsoft.Json;

using TechTalk.SpecFlow;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Utils;

using JsonException = System.Text.Json.JsonException;

namespace Tms.Adapter.SpecFlowPlugin;

public static class TmsTagParser
{
    private const string TagDelimiter = "=";
    private const string TagValueDelimiter = ",";
    private const string ExternalId = "EXTERNALID";
    private const string Title = "TITLE";
    private const string DisplayName = "DISPLAYNAME";
    private const string Description = "DESCRIPTION";
    private const string Labels = "LABELS";
    private const string Links = "LINKS";
    private const string WorkItemIds = "WORKITEMIDS";

    public static TestContainer AddTags(TestContainer testContainer, FeatureInfo featureInfo,
        ScenarioInfo scenarioInfo, Dictionary<string, string> parameters)
    {
        var tags = scenarioInfo.Tags
            .Union(featureInfo.Tags)
            .Distinct(StringComparer.CurrentCultureIgnoreCase);

        foreach (var tag in tags)
        {
            if (!tag.Contains(TagDelimiter))
            {
                continue;
            }

            var tagInfo = tag.Split(TagDelimiter, 2);

            if (tagInfo.Length < 2 || string.IsNullOrWhiteSpace(tagInfo[0]) ||
                string.IsNullOrWhiteSpace(tagInfo[1]))
            {
                continue;
            }

            var tagName = tagInfo[0];
            var tagValue = tagInfo[1];

            switch (tagName.ToUpper())
            {
                case ExternalId:
                    testContainer.ExternalId = Replacer.ReplaceParameters(tagValue, parameters);
                    break;
                case Title:
                    testContainer.Title = Replacer.ReplaceParameters(tagValue, parameters);
                    break;
                case DisplayName:
                    testContainer.DisplayName = Replacer.ReplaceParameters(tagValue, parameters);
                    break;
                case Description:
                    testContainer.Description = Replacer.ReplaceParameters(tagValue, parameters);
                    break;
                case Labels:
                    testContainer.Labels = tagValue
                        .Split(TagValueDelimiter)
                        .ToList();
                    break;
                case Links:
                    if (IsJson(tagValue))
                    {
                        var link = GetLink(tagValue);
                        if (link == null)
                            continue;
                        testContainer.Links.Add(new Link(link.Url, link.Title, link.Description,
                            Enum.Parse<LinkType>(link.Type)));
                    }
                    else if (IsJsonArray(tagValue))
                    {
                        var links = GetLinks(tagValue);

                        links?.ForEach(link =>
                        {
                            if (link != null)
                            {
                                testContainer.Links.Add(new Link(link.Url, link.Title, link.Description,
                                    Enum.Parse<LinkType>(link.Type)));
                            }
                        });
                    }

                    break;
                case WorkItemIds:
                    testContainer.WorkItemIds = tagValue
                        .Split(TagValueDelimiter)
                        .ToList();
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(testContainer.DisplayName))
        {
            testContainer.DisplayName = scenarioInfo.Title;
        }

        if (string.IsNullOrWhiteSpace(testContainer.ExternalId))
        {
            testContainer.ExternalId = Hash.GetStringSha256Hash(featureInfo.Title + testContainer.DisplayName);
        }

        return testContainer;
    }

    private static bool IsJson(this string? source)
    {
        if (source == null)
            return false;

        try
        {
            JsonDocument.Parse(source);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsJsonArray(this string? source)
    {
        if (source == null)
            return false;

        try
        {
            JsonNode.Parse(source);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static LinkItem? GetLink(string source)
    {
        return JsonConvert.DeserializeObject<LinkItem>(source);
    }

    private static List<LinkItem?>? GetLinks(string source)
    {
        return JsonConvert.DeserializeObject<List<LinkItem?>>(source);
    }
}