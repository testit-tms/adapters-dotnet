using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

namespace TmsRunner.Utils;

public sealed class TestAssemblyLoadContext : AssemblyLoadContext
{
    private static readonly ConcurrentDictionary<string, (TestAssemblyLoadContext Context, Assembly Assembly)> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> _skippedAssembly =
    [
        "Tms.Adapter"
    ];

    private readonly string _basePath;

    private TestAssemblyLoadContext(string basePath) : base(isCollectible: false)
    {
        _basePath = basePath;
    }

    public static Assembly LoadTestAssembly(string assemblyPath)
    {
        var fullPath = Path.GetFullPath(assemblyPath);

        var entry = _cache.GetOrAdd(fullPath, static path =>
        {
            var context = new TestAssemblyLoadContext(Path.GetDirectoryName(path) ?? string.Empty);
            var assembly = context.LoadFromAssemblyPath(path);
            return (context, assembly);
        });

        return entry.Assembly;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (_skippedAssembly.Contains(assemblyName.Name ?? string.Empty))
        {
            return null;
        }

        var candidatePath = Path.Combine(_basePath, assemblyName.Name + ".dll");

        return File.Exists(candidatePath) ? LoadFromAssemblyPath(candidatePath) : null;
    }
}
