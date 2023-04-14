using Tms.Adapter.Models;

namespace TmsRunner.Models;

public class MessageMetadata
{
    public MessageType Type { get; set; }
    public string Value { get; set; }
}