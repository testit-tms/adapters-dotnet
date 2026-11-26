using System.Text.RegularExpressions;

namespace Tms.Adapter.Core.SyncStorage;

/// <summary>
/// Floor version + channel for Sync Storage release selection.
/// <c>v0.4.3</c> — numeric tags <c>v0.4.*</c> only;
/// <c>v0.4.3-tms-6.0</c> — tags <c>v0.4.*-tms-6.0</c> only.
/// </summary>
internal sealed class SyncStorageVersionConstraint
{
    private static readonly Regex TagPattern = new(
        @"^v?(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-tms-(?<tms>[\d.]+))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private SyncStorageVersionConstraint(int major, int minor, int patch, string tmsChannel, string raw)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        TmsChannel = tmsChannel ?? string.Empty;
        Raw = raw;
    }

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string TmsChannel { get; }
    public string Raw { get; }

    public bool HasTmsChannel => TmsChannel.Length > 0;

    public string NormalizedTag =>
        HasTmsChannel
            ? $"v{Major}.{Minor}.{Patch}-tms-{TmsChannel}"
            : $"v{Major}.{Minor}.{Patch}";

    public static SyncStorageVersionConstraint Parse(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Sync Storage version is empty", nameof(tag));

        var trimmed = tag.Trim();
        var match = TagPattern.Match(trimmed);
        if (!match.Success)
            throw new ArgumentException($"Unsupported Sync Storage version: {trimmed}", nameof(tag));

        return new SyncStorageVersionConstraint(
            int.Parse(match.Groups["major"].Value),
            int.Parse(match.Groups["minor"].Value),
            int.Parse(match.Groups["patch"].Value),
            match.Groups["tms"].Success ? match.Groups["tms"].Value : string.Empty,
            trimmed.StartsWith("v", StringComparison.Ordinal) ? trimmed : "v" + trimmed);
    }

    public bool MatchesChannel(SyncStorageVersionConstraint other) =>
        string.Equals(TmsChannel, other.TmsChannel, StringComparison.Ordinal);

    public bool SameMinorLine(SyncStorageVersionConstraint other) =>
        Major == other.Major && Minor == other.Minor;

    public bool IsAtLeastFloor(SyncStorageVersionConstraint floor) =>
        CompareSemver(this, floor) >= 0;

    public static int CompareSemver(SyncStorageVersionConstraint a, SyncStorageVersionConstraint b)
    {
        var major = a.Major.CompareTo(b.Major);
        if (major != 0) return major;
        var minor = a.Minor.CompareTo(b.Minor);
        if (minor != 0) return minor;
        return a.Patch.CompareTo(b.Patch);
    }
}
