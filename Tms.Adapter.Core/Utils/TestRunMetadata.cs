using Newtonsoft.Json;
using TestIT.AdaptersApi.Model;
using Tms.Adapter.Core.Models;
using ApiLinkType = TestIT.AdaptersApi.Model.LinkType;

namespace Tms.Adapter.Core.Utils;

public static class TestRunMetadata
{
    public static bool HasAny(IReadOnlyCollection<string>? tags, IReadOnlyCollection<TestRunLinkConfig>? links)
        => (tags?.Count > 0) || (links?.Count > 0);

    public static List<string> ParseTags(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var value = raw.Trim();
        if (value.StartsWith('['))
        {
            try
            {
                return (JsonConvert.DeserializeObject<List<string>>(value) ?? [])
                    .Select(t => t?.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Cast<string>()
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
            }
            catch (JsonException)
            {
                return [];
            }
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public static List<TestRunLinkConfig> ParseLinks(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return (JsonConvert.DeserializeObject<List<TestRunLinkConfig>>(raw) ?? [])
                .Where(l => !string.IsNullOrWhiteSpace(l?.Url))
                .Select(l =>
                {
                    l.Url = l.Url.Trim();
                    return l;
                })
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static List<string> MergeTags(IEnumerable<string>? existing, IEnumerable<string>? configured)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var tag in (existing ?? []).Concat(configured ?? []))
        {
            if (string.IsNullOrWhiteSpace(tag) || !seen.Add(tag))
            {
                continue;
            }

            result.Add(tag);
        }

        return result;
    }

    public static ApiLinkType ParseLinkType(string? type)
        => Enum.TryParse<ApiLinkType>(type, ignoreCase: true, out var parsed)
            ? parsed
            : ApiLinkType.Related;

    public static List<CreateLinkApiModel> ToCreateLinks(IEnumerable<TestRunLinkConfig>? links)
        => (links ?? [])
            .Where(l => !string.IsNullOrWhiteSpace(l.Url))
            .Select(l => new CreateLinkApiModel(
                title: l.Title,
                url: l.Url,
                description: l.Description,
                type: ParseLinkType(l.Type)))
            .ToList();

    public static List<UpdateLinkApiModel> MergeLinks(
        IEnumerable<LinkApiResult>? existing,
        IEnumerable<TestRunLinkConfig>? configured)
    {
        var result = (existing ?? [])
            .Select(link => new UpdateLinkApiModel(
                id: link.Id,
                title: link.Title,
                url: link.Url,
                description: link.Description,
                type: link.Type))
            .ToList();

        var urls = new HashSet<string>(result.Select(l => l.Url), StringComparer.Ordinal);

        foreach (var link in configured ?? [])
        {
            if (string.IsNullOrWhiteSpace(link.Url) || !urls.Add(link.Url))
            {
                continue;
            }

            result.Add(new UpdateLinkApiModel(
                title: link.Title,
                url: link.Url,
                description: link.Description,
                type: ParseLinkType(link.Type)));
        }

        return result;
    }
}
