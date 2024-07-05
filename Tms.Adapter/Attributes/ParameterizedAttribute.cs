using MethodBoundaryAspect.Fody.Attributes;

using Newtonsoft.Json;

using Tms.Adapter.Models;

namespace Tms.Adapter.Attributes;

public class ParameterizedAttribute : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs arg)
    {
        var parameterNames = arg.Method.GetParameters().Select(x => x.Name.ToString());
        var arguments = arg.Arguments.ToStringList();
        var args = parameterNames
            .Zip(arguments, (k, v) => new { k, v })
            .ToDictionary(x => x.k, x => x.v);
            
        Console.WriteLine($"{MessageType.TmsParameters}: " + JsonConvert.SerializeObject(args));
    }
}

public static class ObjectArrayExtension
{
    public static IEnumerable<string>? ToStringList(this object[] objects)
    {
        var result = new List<string>();
            
        foreach (var obj in objects)
        {
            if (obj is Array array)
            {
                result.Add(string.Join(", ", array.Cast<object>()));
            }
            else
            {
                result.Add(obj.ToString());
            }
        }
            
        return result;
    }
}