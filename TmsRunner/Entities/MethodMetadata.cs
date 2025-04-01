namespace TmsRunner.Entities;

public sealed record MethodMetadata
{
    public string? Name;
    public string? Namespace;
    public string? Classname;
    public List<Attribute>? Attributes;
    public List<string?> Parameters;
}