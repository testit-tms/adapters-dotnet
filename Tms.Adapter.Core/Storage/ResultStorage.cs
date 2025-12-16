using System.Collections.Concurrent;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Service;

namespace Tms.Adapter.Core.Storage;

public class ResultStorage
{
    private readonly ConcurrentDictionary<string, LinkedList<string>> _stepStorage = new();

    private readonly ConcurrentDictionary<string, object> _storage = new();

    private LinkedList<string> Steps => _stepStorage.GetOrAdd(
        AdapterManager.CurrentTestIdGetter(),
        new LinkedList<string>()
    );

    public T Get<T>(string id)
    {
        return (T)_storage[id];
    }

    public T Put<T>(string uuid, T item)
    {
        return (T)_storage.GetOrAdd(uuid, item!);
    }

    public T Remove<T>(string id)
    {
        _storage.TryRemove(id, out var value);

        return (T)value;
    }

    public void ClearStepContext()
    {
        Steps.Clear();
    }

    public void StartStep(string id)
    {
        Steps.AddFirst(id);
    }

    public void StopStep()
    {
        Steps.RemoveFirst();
    }

    public string? GetRootStep()
    {
        return Steps.Last?.Value;
    }

    public string? GetCurrentStep()
    {
        return Steps.First?.Value;
    }

    public void AddStep(string parentId, string id, StepResult stepResult)
    {
        Put(id, stepResult);
        Get<ExecutableItem>(parentId).Steps.Add(stepResult);
    }
}