namespace Tms.Adapter.Attributes
{
    public class DescriptionAttribute : BaseAttribute<string>
    {
        public DescriptionAttribute(string description)
        {
            Value = description;
        }
    }
}
