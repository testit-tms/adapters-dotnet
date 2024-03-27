using Tms.Adapter.Models;

namespace TmsRunner.Entities;

public sealed record MessageMetadata
{
    public MessageType Type;
    public string? Value;
}