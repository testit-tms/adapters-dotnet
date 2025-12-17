using System.Reflection;
using AspectInjector.Broker;
using Tms.Adapter.Core.Attributes;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Utils;
using Tms.Adapter.XUnit.Attributes;

namespace Tms.Adapter.XUnit.Aspects;

[Aspect(Scope.Global)]
public class StepAspect
{
    private static readonly MethodInfo AsyncHandler =
        typeof(StepAspect).GetMethod(nameof(WrapAsync), BindingFlags.NonPublic | BindingFlags.Static);

    private static readonly MethodInfo SyncHandler =
        typeof(StepAspect).GetMethod(nameof(WrapSync), BindingFlags.NonPublic | BindingFlags.Static);


    [Advice(Kind.Around)]
    public object Around([Argument(Source.Name)] string name,
        [Argument(Source.Arguments)] object[] args,
        [Argument(Source.Target)] Func<object[], object> target,
        [Argument(Source.Metadata)] MethodBase metadata,
        [Argument(Source.ReturnType)] Type returnType)
    {
        object executionResult;

        var stepParameters = metadata.GetParameters()
            .Zip(args, (parameter, value) => new
            {
                parameter,
                value
            })
            .ToDictionary(x => x.parameter.Name, x => x.value.ToString());

        var stepName = metadata.GetCustomAttribute<TitleAttribute>()?.Value ?? name;
        stepName = Replacer.ReplaceParameters(stepName, stepParameters);

        try
        {
            StartFixture(metadata, stepName);
            StartStep(metadata, stepName, stepParameters);

            executionResult = GetStepExecutionResult(returnType, target, args);

            PassStep(metadata);
            PassFixture(metadata);
        }
        catch (Exception)
        {
            ThrowStep(metadata);
            ThrowFixture(metadata);
            throw;
        }

        return executionResult;
    }

    private static void StartStep(MethodBase metadata, string stepName, Dictionary<string, string> stepParameters)
    {
        if (metadata.GetCustomAttribute<StepAttribute>() != null)
        {
            StepManager.StartStep(stepName, step =>
            {
                step.Parameters = stepParameters;
                var description = metadata.GetCustomAttribute<DescriptionAttribute>();

                if (description == null) return;

                step.Description = Replacer.ReplaceParameters(description.Value, stepParameters);
            });
        }
    }

    private static void PassStep(MethodBase metadata)
    {
        if (metadata.GetCustomAttribute<StepAttribute>() != null)
        {
            StepManager.PassStep();
        }
    }

    private static void ThrowStep(MethodBase metadata)
    {
        if (metadata.GetCustomAttribute<StepAttribute>() != null)
        {
            StepManager.FailStep();
        }
    }

    private static void StartFixture(MethodBase metadata, string stepName)
    {
        if (metadata.GetCustomAttribute<BeforeAttribute>() != null)
        {
            StepManager.StartBeforeFixture(stepName);
        }

        if (metadata.GetCustomAttribute<AfterAttribute>() != null)
        {
            StepManager.StartAfterFixture(stepName);
        }
    }

    private static void PassFixture(MethodBase metadata)
    {
        if (metadata.GetCustomAttribute<BeforeAttribute>() == null &&
            metadata.GetCustomAttribute<AfterAttribute>() == null) return;

        StepManager.StopFixture(result => result.Status = Status.Passed);
    }

    private static void ThrowFixture(MethodBase metadata)
    {
        if (metadata.GetCustomAttribute<BeforeAttribute>() == null &&
            metadata.GetCustomAttribute<AfterAttribute>() == null) return;
        
        StepManager.StopFixture(result => { result.Status = Status.Failed; });
    }

    private object GetStepExecutionResult(Type returnType, Func<object[], object> target, object[] args)
    {
        if (typeof(Task).IsAssignableFrom(returnType))
        {
            var syncResultType = returnType.IsConstructedGenericType
                ? returnType.GenericTypeArguments[0]
                : typeof(object);
            return AsyncHandler.MakeGenericMethod(syncResultType)
                .Invoke(this, new object[] { target, args });
        }

        if (typeof(void).IsAssignableFrom(returnType))
        {
            return target(args);
        }

        return SyncHandler.MakeGenericMethod(returnType)
            .Invoke(this, new object[] { target, args });
    }

    private static T WrapSync<T>(Func<object[], object> target, object[] args)
    {
        try
        {
            return (T)target(args);
        }
        catch (Exception)
        {
            return default(T);
        }
    }

    private static async Task<T> WrapAsync<T>(Func<object[], object> target, object[] args)
    {
        try
        {
            return await ((Task<T>)target(args)).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return default!;
        }
    }
}