using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MethodBoundaryAspect.Fody.Attributes;
using Tms.Adapter.Models;

namespace Tms.Adapter.Attributes
{
    public class ParameterizedAttribute : OnMethodBoundaryAspect
    {
        public override void OnEntry(MethodExecutionArgs arg)
        {
            var parameterNames = arg.Method.GetParameters().Select(x => x.Name);
            var arguments = arg.Arguments.ToStringList();
            var args = parameterNames
                        .Zip(arguments, (k, v) => new { k, v })
                        .ToDictionary(x => x.k, x => x.v);
            
            Console.WriteLine($"{MessageType.TmsParameters}: " + JsonSerializer.Serialize(args));
        }
    }

    public static class ObjectArrayExtension
    {
        public static IEnumerable<string>? ToStringList(this object[] objects)
        {
            var result = new List<string>();
            
            foreach (var obj in objects)
            {
                switch (obj)
                {
                    case null:
                        result.Add("null");
                        break;
                    case Array array:
                        result.Add(string.Join(", ", array.Cast<object>()));
                        break;
                    default:
                        result.Add(obj.ToString());
                        break;
                }
            }
            
            return result;
        }
    }
}
