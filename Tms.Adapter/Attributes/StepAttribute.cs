using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using MethodBoundaryAspect.Fody.Attributes;

using Newtonsoft.Json;

using Tms.Adapter.Models;
using Tms.Adapter.Utils;

namespace Tms.Adapter.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class StepAttribute : OnMethodBoundaryAspect
{
    private static readonly MethodInfo WrapGenericTaskMethod = typeof(StepAttribute)
        .GetMethod(nameof(WrapGenericTaskAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo WrapGenericValueTaskMethod = typeof(StepAttribute)
        .GetMethod(nameof(WrapGenericValueTaskAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    public override void OnEntry(MethodExecutionArgs arg)
    {
        var currentMethod = arg.Method;
        var callerMethod = GetCallerMethod(currentMethod);
        var parent = StepExecutionContext.Current;
        var currentMethodType = GetFixtureType(currentMethod);
        var phase = currentMethodType
            ?? parent?.Phase
            ?? GetCallerType(callerMethod)
            ?? CallerMethodType.TestMethod;

        var invocation = new StepInvocation
        {
            Guid = Guid.NewGuid(),
            StartedOn = DateTime.UtcNow,
            Parent = parent,
            Phase = phase
        };

        arg.MethodExecutionTag = invocation;
        StepExecutionContext.Current = invocation;

        var arguments = arg.Arguments
            .Select(Convert.ToString)
            .ToList();
        var parameters = arg.Method.GetParameters()
            .Select(x => x.Name ?? string.Empty)
            .Zip(arguments, (key, value) => new { key, value })
            .ToDictionary(x => x.key, x => x.value);

        var title = currentMethod.GetCustomAttributes(false)
            .OfType<TitleAttribute>()
            .Select(x => x.Value)
            .FirstOrDefault();
        var description = currentMethod.GetCustomAttributes(false)
            .OfType<DescriptionAttribute>()
            .Select(x => x.Value)
            .FirstOrDefault();

        var step = new StepDto
        {
            Guid = invocation.Guid,
            StartedOn = invocation.StartedOn,
            Title = Replacer.ReplaceParameters(string.IsNullOrEmpty(title) ? currentMethod.Name : title, parameters),
            Description = string.IsNullOrEmpty(description) ? null : Replacer.ReplaceParameters(description, parameters),
            CurrentMethod = currentMethod.Name,
            CallerMethod = GetCallerDisplayName(callerMethod),
            Instance = arg.Instance?.GetType().Name,
            Args = parameters,
            CallerMethodType = phase,
            CurrentMethodType = currentMethodType,
            ProtocolVersion = StepDto.CurrentProtocolVersion,
            ParentGuid = parent?.Guid,
            Phase = phase
        };

        Console.WriteLine($"{MessageType.TmsStep}: " + JsonConvert.SerializeObject(step));
    }

#pragma warning disable CA2012 // The aspect must replace and return the intercepted ValueTask.
    public override void OnExit(MethodExecutionArgs arg)
    {
        if (arg.MethodExecutionTag is not StepInvocation invocation)
        {
            return;
        }

        RestoreParent(invocation);

        if (arg.ReturnValue is Task task)
        {
            arg.ReturnValue = WrapTask(task, invocation);
            return;
        }

        if (arg.ReturnValue is ValueTask valueTask)
        {
            arg.ReturnValue = WrapValueTaskAsync(valueTask, invocation);
            return;
        }

        var returnType = arg.ReturnValue?.GetType();
        if (returnType?.IsGenericType == true &&
            returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            arg.ReturnValue = WrapGenericValueTaskMethod
                .MakeGenericMethod(returnType.GetGenericArguments()[0])
                .Invoke(null, [arg.ReturnValue, invocation]);
            return;
        }

        WriteResult(invocation, "Passed", arg.ReturnValue);
    }
#pragma warning restore CA2012

    public override void OnException(MethodExecutionArgs arg)
    {
        if (arg.MethodExecutionTag is not StepInvocation invocation)
        {
            return;
        }

        RestoreParent(invocation);
        WriteResult(invocation, "Failed");
    }

    private static object WrapTask(Task task, StepInvocation invocation)
    {
        var taskResultType = GetTaskResultType(task.GetType());
        if (taskResultType == null)
        {
            return WrapTaskAsync(task, invocation);
        }

        return WrapGenericTaskMethod
            .MakeGenericMethod(taskResultType)
            .Invoke(null, [task, invocation])!;
    }

    private static Type? GetTaskResultType(Type taskType)
    {
        for (var type = taskType; type != null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static async Task WrapTaskAsync(Task task, StepInvocation invocation)
    {
        try
        {
            await task.ConfigureAwait(false);
            WriteResult(invocation, "Passed");
        }
        catch
        {
            WriteResult(invocation, "Failed");
            throw;
        }
    }

    private static async Task<T> WrapGenericTaskAsync<T>(Task<T> task, StepInvocation invocation)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            WriteResult(invocation, "Passed", result);
            return result;
        }
        catch
        {
            WriteResult(invocation, "Failed");
            throw;
        }
    }

    private static async ValueTask WrapValueTaskAsync(ValueTask task, StepInvocation invocation)
    {
        try
        {
            await task.ConfigureAwait(false);
            WriteResult(invocation, "Passed");
        }
        catch
        {
            WriteResult(invocation, "Failed");
            throw;
        }
    }

    private static async ValueTask<T> WrapGenericValueTaskAsync<T>(ValueTask<T> task, StepInvocation invocation)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            WriteResult(invocation, "Passed", result);
            return result;
        }
        catch
        {
            WriteResult(invocation, "Failed");
            throw;
        }
    }

    private static void RestoreParent(StepInvocation invocation)
    {
        if (ReferenceEquals(StepExecutionContext.Current, invocation))
        {
            StepExecutionContext.Current = invocation.Parent;
        }
    }

    private static CallerMethodType? GetFixtureType(MethodBase method)
    {
        foreach (var attribute in method.GetCustomAttributes(false))
        {
            switch (attribute.GetType().Name)
            {
                case "TestInitializeAttribute" or "SetUpAttribute" or "ClassInitializeAttribute" or "OneTimeSetUpAttribute":
                    return CallerMethodType.Setup;
                case "TestCleanupAttribute" or "TearDownAttribute" or "ClassCleanupAttribute" or "OneTimeTearDownAttribute":
                    return CallerMethodType.Teardown;
            }
        }

        return null;
    }

    private static CallerMethodType? GetCallerType(MethodBase? method)
    {
        if (method == null)
        {
            return null;
        }

        var fixtureType = GetFixtureType(method);
        if (fixtureType != null)
        {
            return fixtureType;
        }

        return method.GetCustomAttributes(false).Any(attribute => attribute.GetType().Name is
            "TestMethodAttribute" or "FactAttribute" or "TheoryAttribute" or
            "TestCaseAttribute" or "TestAttribute")
            ? CallerMethodType.TestMethod
            : null;
    }

    private static MethodBase? GetCallerMethod(MethodBase currentMethod)
    {
        var frames = new StackTrace().GetFrames();
        if (frames == null)
        {
            return null;
        }

        var aspectType = typeof(StepAttribute);
        foreach (var frame in frames)
        {
            var method = frame?.GetMethod();
            if (method == null || method.DeclaringType == aspectType || method == currentMethod)
            {
                continue;
            }

            return method;
        }

        return null;
    }

    private static string? GetCallerDisplayName(MethodBase? method)
    {
        if (method == null)
        {
            return null;
        }

        var name = method.Name.Replace("$_executor_", string.Empty);
        var typeName = method.DeclaringType?.Name ?? string.Empty;
        var match = Regex.Match(typeName, @"^<(.+)>d__\d+$");
        return match.Success ? match.Groups[1].Value : name;
    }

    private static void WriteResult(StepInvocation invocation, string outcome, object? result = null)
    {
        if (Interlocked.CompareExchange(ref invocation.CompletionWritten, 1, 0) != 0)
        {
            return;
        }

        var completedAt = DateTime.UtcNow;
        var stepResult = new StepResult
        {
            Guid = invocation.Guid,
            CompletedOn = completedAt,
            Duration = (long)(completedAt - invocation.StartedOn).TotalMilliseconds,
            Result = result?.ToString(),
            Outcome = outcome
        };

        Console.WriteLine($"{MessageType.TmsStepResult}: " + JsonConvert.SerializeObject(stepResult));
    }
}