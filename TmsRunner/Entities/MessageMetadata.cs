using Tms.Adapter.Models;

namespace TmsRunner.Entities;

public sealed record MessageMetadata
{
    public MessageType Type { get; init; }
    public string? Value { get; init; }
}