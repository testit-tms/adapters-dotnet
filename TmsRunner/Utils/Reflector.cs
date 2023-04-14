using System.Reflection;
using TmsRunner.Models;

namespace TmsRunner.Utils;

public class Reflector
{
    public MethodMetadata GetMethodMetadata(string assemblyPath, string methodName,
        Dictionary<string, string>? parameters)
    {
        var assembly = Assembly.LoadFrom(assemblyPath);

        var fullyQualifiedNameArray = methodName.Split(".");

        var type = assembly.GetType(string.Join(".", fullyQualifiedNameArray[..^1]));

        // TODO: need to check parameters name and position
        //        var para = method.GetParameters();
        //          var par = para[0];
        var methods = type.GetMethods()
            .Where(m => m.Name.Equals(fullyQualifiedNameArray[^1])).ToList();

        if (parameters is not null)
        {
            methods = methods.Where(m => m.GetParameters().Length == parameters.Count).ToList();
        }

        var method = methods.FirstOrDefault();

        if (method is null)
        {
            throw new ApplicationException(
                $"Method {fullyQualifiedNameArray[^1]} not found!");
        }

        var attributes = method.GetCustomAttributes(false)
            .Select(a => (Attribute)a)
            .ToList();

        return new MethodMetadata()
        {
            Name = method.Name,
            Namespace = string.Join(".", fullyQualifiedNameArray[..(fullyQualifiedNameArray.Length - 2)]),
            Classname = fullyQualifiedNameArray[fullyQualifiedNameArray.Length - 2],
            Attributes = attributes
        };
    }
}