using System;

namespace Tms.Adapter.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BaseAttribute<T> : Attribute
    {
        public T? Value { get; set; }
    }
}
