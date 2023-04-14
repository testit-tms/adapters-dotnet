namespace TmsRunner.Models;

public class MethodMetadata
{
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string Classname { get; set; }
    public List<Attribute> Attributes { get; set; }
}   