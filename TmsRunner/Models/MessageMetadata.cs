using Tms.Adapter.Models;

namespace TmsRunner.Models;

public sealed record MessageMetadata
{
    public MessageType Type { get; set; }
    public string? Value { get; set; }
}