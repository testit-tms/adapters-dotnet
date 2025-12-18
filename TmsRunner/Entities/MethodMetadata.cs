namespace TmsRunner.Entities;

public sealed record MethodMetadata
{
    public string? Name { get; set; }
    public string? Namespace { get; set; }
    public string? Classname { get; set; }
    public List<Attribute>? Attributes { get; set; }
    public List<string?> Parameters { get; set; } = [];
}