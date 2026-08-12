namespace Tms.Adapter.Core.Models;

public sealed class TestRunLinkConfig
{
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
}
