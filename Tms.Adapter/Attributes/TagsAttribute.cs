namespace Tms.Adapter.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class TagsAttribute : BaseAttribute<List<string>>
{
    public TagsAttribute(params string[] tags)
    {
        Value = tags.ToList();
    }
}