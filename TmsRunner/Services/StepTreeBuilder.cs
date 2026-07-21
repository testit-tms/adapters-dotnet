using Tms.Adapter.Models;
using TmsRunner.Entities;

namespace TmsRunner.Services;

internal sealed class StepTreeBuilder
{
    private readonly List<StepModel> _roots = [];
    private readonly Dictionary<Guid, StepModel> _steps = [];
    private readonly Dictionary<Guid, List<StepModel>> _pendingChildren = [];
    private StepModel? _legacyParent;
    private int _legacyNestingLevel = 1;

    public IReadOnlyList<StepModel> Roots => _roots;

    public bool AddStep(StepModel step)
    {
        if (!_steps.TryAdd(step.Guid, step))
        {
            return false;
        }

        if (step.ProtocolVersion >= StepDto.CurrentProtocolVersion)
        {
            AddExplicitStep(step);
        }
        else
        {
            AddLegacyStep(step);
        }

        AttachPendingChildren(step);
        return true;
    }

    public bool ApplyResult(StepResult result)
    {
        if (!_steps.TryGetValue(result.Guid, out var step))
        {
            return false;
        }

        // Old versions of the aspect can emit Passed after Failed for the same async invocation.
        // A later successful callback must not hide the observed failure.
        var preserveFailure = IsFailed(step.Outcome) && !IsFailed(result.Outcome);
        if (!preserveFailure)
        {
            step.CompletedOn = result.CompletedOn;
            step.Duration = result.Duration;
            step.Result = result.Result;
            step.Outcome = result.Outcome;
        }

        if (step.ProtocolVersion < StepDto.CurrentProtocolVersion)
        {
            _legacyParent = step.ParentStep;
            _legacyNestingLevel = (_legacyParent?.NestingLevel ?? 0) + 1;
        }

        return true;
    }

    public StepModel? GetAttachmentStep(Guid? stepGuid)
    {
        return stepGuid.HasValue ? _steps.GetValueOrDefault(stepGuid.Value) : _legacyParent;
    }

    public List<StepModel> Build()
    {
        foreach (var children in _pendingChildren.Values)
        {
            foreach (var child in children)
            {
                child.ParentStep = null;
                child.NestingLevel = 1;
                _roots.Add(child);
            }
        }

        _pendingChildren.Clear();
        return _roots;
    }

    private void AddExplicitStep(StepModel step)
    {
        if (step.ParentGuid is not { } parentGuid)
        {
            step.NestingLevel = 1;
            _roots.Add(step);
            return;
        }

        if (_steps.TryGetValue(parentGuid, out var parent))
        {
            Attach(parent, step);
            return;
        }

        if (!_pendingChildren.TryGetValue(parentGuid, out var children))
        {
            children = [];
            _pendingChildren[parentGuid] = children;
        }

        children.Add(step);
    }

    private void AttachPendingChildren(StepModel parent)
    {
        if (!_pendingChildren.Remove(parent.Guid, out var children))
        {
            return;
        }

        foreach (var child in children)
        {
            Attach(parent, child);
            AttachPendingChildren(child);
        }
    }

    private static void Attach(StepModel parent, StepModel child)
    {
        child.ParentStep = parent;
        child.NestingLevel = parent.NestingLevel + 1;
        parent.Steps.Add(child);
    }

    private void AddLegacyStep(StepModel step)
    {
        if ((step.CallerMethodType != null && _legacyParent == null) ||
            (step.CurrentMethodType != null && _legacyParent == null))
        {
            AddLegacyRoot(step);
            return;
        }

        var calledMethod = GetCalledMethod(step.CallerMethod);
        while (_legacyParent != null && calledMethod != null && _legacyParent.CurrentMethod != calledMethod)
        {
            _legacyParent = _legacyParent.ParentStep;
            _legacyNestingLevel--;
        }

        if (_legacyParent == null)
        {
            AddLegacyRoot(step);
            return;
        }

        step.ParentStep = _legacyParent;
        step.NestingLevel = _legacyNestingLevel;
        _legacyParent.Steps.Add(step);
        _legacyParent = step;
        _legacyNestingLevel++;
    }

    private void AddLegacyRoot(StepModel step)
    {
        step.NestingLevel = _legacyNestingLevel = 1;
        _roots.Add(step);
        _legacyParent = step;
        _legacyNestingLevel++;
    }

    private static string? GetCalledMethod(string? calledMethod)
    {
        if (calledMethod == null || !calledMethod.Contains('<'))
        {
            return calledMethod;
        }

        var start = calledMethod.IndexOf('<') + 1;
        var end = calledMethod.LastIndexOf('>');
        return end > start ? calledMethod[start..end] : calledMethod;
    }

    private static bool IsFailed(string? outcome) =>
        string.Equals(outcome, "Failed", StringComparison.OrdinalIgnoreCase);
}