using System.Reflection;
using TmsRunner.Entities;

namespace TmsRunner.Utils;

public sealed class Reflector
{
    public static MethodMetadata GetMethodMetadata(string assemblyPath,
                                                   string methodName,
                                                   Dictionary<string, string>? parameters)
    {
        var assembly = Assembly.LoadFrom(assemblyPath);
        var fullyQualifiedNameArray = methodName.Split(".");
        var type = assembly.GetType(string.Join(".", fullyQualifiedNameArray[..^1]));
        var methods = type?.GetMethods()
            .Where(m => m.Name.Equals(fullyQualifiedNameArray[^1])).ToList();

        if (parameters is not null)
        {
            methods = methods?
                .Where(m => CompareParameters(m.GetParameters(), parameters))
                .ToList();
        }

        var method = (methods?.FirstOrDefault()) ?? throw new ApplicationException($"Method {fullyQualifiedNameArray[^1]} not found!");
        var attributes = method.GetCustomAttributes(false)
            .Select(a => (Attribute)a)
            .ToList();

        return new MethodMetadata
        {
            Name = method.Name,
            Namespace = string.Join(".", fullyQualifiedNameArray[..^2]),
            Classname = fullyQualifiedNameArray[^2],
            Attributes = attributes
        };
    }

    private static bool CompareParameters(IReadOnlyList<ParameterInfo> methodParameters,
                                          Dictionary<string, string> parameters)
    {
        if (methodParameters.Count != parameters.Count)
        {
            return false;
        }

        var i = 0;
        foreach (var parameter in parameters)
        {
            if (parameter.Key != methodParameters[i].Name)
            {
                return false;
            }

            i++;
        }

        return true;
    }
}