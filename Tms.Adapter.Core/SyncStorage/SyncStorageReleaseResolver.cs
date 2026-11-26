using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Tms.Adapter.Core.SyncStorage;

/// <summary>
/// Resolves the newest Sync Storage release on the floor's major.minor line and channel.
/// </summary>
internal static class SyncStorageReleaseResolver
{
    internal const string ReleasesApiUrl =
        "https://api.github.com/repos/testit-tms/sync-storage-public/releases?per_page=100";

    public static string ResolveLatest(string floorConstraint, ILogger? logger = null)
    {
        var floor = SyncStorageVersionConstraint.Parse(floorConstraint);
        try
        {
            var tags = FetchReleaseTagsAsync().GetAwaiter().GetResult();
            var selected = SelectLatest(floor, tags);
            if (selected != null)
            {
                logger?.LogInformation(
                    "Sync Storage: resolved {Floor} → {Selected} (channel={Channel})",
                    floor.NormalizedTag,
                    selected,
                    floor.HasTmsChannel ? "tms-" + floor.TmsChannel : "numeric");
                return selected;
            }

            logger?.LogWarning("Sync Storage: no matching release for {Floor}, using floor", floor.NormalizedTag);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(
                ex,
                "Sync Storage: failed to resolve latest release for {Floor}, using floor",
                floor.NormalizedTag);
        }

        return floor.NormalizedTag;
    }

    internal static string? SelectLatest(
        SyncStorageVersionConstraint floor,
        IEnumerable<string> tags)
    {
        SyncStorageVersionConstraint? best = null;
        foreach (var tag in tags)
        {
            SyncStorageVersionConstraint candidate;
            try
            {
                candidate = SyncStorageVersionConstraint.Parse(tag);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (!floor.MatchesChannel(candidate) || !floor.SameMinorLine(candidate) || !candidate.IsAtLeastFloor(floor))
                continue;

            if (best == null || SyncStorageVersionConstraint.CompareSemver(candidate, best) > 0)
                best = candidate;
        }

        return best?.NormalizedTag;
    }

    private static async Task<List<string>> FetchReleaseTagsAsync()
    {
        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromSeconds(30);
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.github+json");
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "TestIT-DotNet-Adapter");

        var json = await http.GetStringAsync(ReleasesApiUrl).ConfigureAwait(false);
        var root = JArray.Parse(json);
        var tags = new List<string>();
        foreach (var node in root)
        {
            if (node.Value<bool?>("draft") == true || node.Value<bool?>("prerelease") == true)
                continue;

            var tag = node.Value<string>("tag_name");
            if (!string.IsNullOrEmpty(tag))
                tags.Add(tag);
        }

        return tags;
    }
}
