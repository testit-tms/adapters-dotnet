using System.Reflection;
using System.Runtime.Loader;

namespace TmsRunner.Utils;

public sealed class TestAssemblyLoadContext : AssemblyLoadContext
{
    private static readonly string[] _skippedAssemblyNames =
    [
        nameof(Tms.Adapter)
    ];

    private readonly string _basePath;

    public TestAssemblyLoadContext(string assemblyPath) : base(isCollectible: true)
    {
        _basePath = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (_skippedAssemblyNames.Contains(assemblyName.Name ?? string.Empty))
        {
            return null;
        }

        var candidatePath = Path.Combine(_basePath, assemblyName.Name + ".dll");

        return File.Exists(candidatePath) ? LoadFromAssemblyPath(candidatePath) : null;
    }
}
