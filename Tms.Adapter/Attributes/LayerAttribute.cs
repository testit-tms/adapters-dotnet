namespace Tms.Adapter.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class LayerAttribute : BaseAttribute<string>
{
    public LayerAttribute(string layer)
    {
        Value = layer;
    }
}
