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
    private MethodBase? _callerMethod;
    private string? _title;
    private string? _description;
    private CallerMethodType? _callerMethodType;
    private CallerMethodType? _currentMethodType;
    private DateTime? _startedAt;
    private Guid _guid;

    public override void OnEntry(MethodExecutionArgs arg)
    {
        _startedAt = DateTime.UtcNow;
        _guid = Guid.NewGuid();

        var currentMethod = arg.Method;
        _callerMethod = GetCallerMethod(currentMethod);

        var arguments = arg.Arguments
            .Select(Convert.ToString)
            .ToList();
        var parameters = arg.Method.GetParameters()
            .Select(x => x.Name.ToString())
            .Zip(arguments, (k, v) => new { k, v })
            .ToDictionary(x => x.k, x => x.v);

        var currentMethodAttributes = currentMethod.GetCustomAttributes(false);

        if (currentMethodAttributes is not null)
        {
            foreach (var attribute in currentMethodAttributes)
            {
                switch (attribute)
                {
                    case TitleAttribute title:
                        _title = title.Value;
                        break;
                    case DescriptionAttribute description:
                        _description = description.Value;
                        break;
                }

                var name = attribute.GetType().Name;
                switch (name)
                {
                    case "TestInitializeAttribute" or "SetUpAttribute" or "ClassInitializeAttribute":
                        _currentMethodType = CallerMethodType.Setup;
                        _callerMethodType = CallerMethodType.Setup;
                        break;
                    case "TestCleanupAttribute" or "TearDownAttribute" or "ClassCleanupAttribute":
                        _currentMethodType = CallerMethodType.Teardown;
                        _callerMethodType = CallerMethodType.Teardown;
                        break;
                }
            }
        }

        if (_callerMethodType == default)
        {
            if (_callerMethod == null)
            {
                _callerMethodType = CallerMethodType.TestMethod;
            }
            else
            {
                var callerMethodAttributes = _callerMethod.GetCustomAttributes(false);
                if (callerMethodAttributes is not null)
                {
                    foreach (var attribute in callerMethodAttributes)
                    {
                        var name = attribute.GetType().Name;
                        _callerMethodType = name switch
                        {
                            "TestInitializeAttribute" or "SetUpAttribute" or "ClassInitializeAttribute" =>
                                CallerMethodType.Setup,
                            "TestMethodAttribute" or "FactAttribute" or "TestCaseAttribute" or "TestAttribute" =>
                                CallerMethodType.TestMethod,
                            "TestCleanupAttribute" or "TearDownAttribute" or "ClassCleanupAttribute" =>
                                CallerMethodType.Teardown,
                            _ => _callerMethodType
                        };
                    }
                }
            }
        }
        
        var newTitle = string.IsNullOrEmpty(_title) ? currentMethod.Name : _title;
        var newDescription = string.IsNullOrEmpty(_description)
            ? null
            : Replacer.ReplaceParameters(_description, parameters);

        var step = new StepDto
        {
            Guid = _guid,
            StartedOn = _startedAt,
            Title = Replacer.ReplaceParameters(newTitle, parameters),
            Description = newDescription,
            CurrentMethod = currentMethod.Name,
            CallerMethod = GetCallerDisplayName(_callerMethod),
            Instance = arg.Instance?.GetType().Name,
            Args = parameters,
            CallerMethodType = _callerMethodType,
            CurrentMethodType = _currentMethodType
        };

        Console.WriteLine($"{MessageType.TmsStep}: " + JsonConvert.SerializeObject(step));
    }

    public override void OnExit(MethodExecutionArgs arg)
    {
        WriteData("Passed", arg.ReturnValue);
    }

    public override void OnException(MethodExecutionArgs arg)
    {
        WriteData("Failed");
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
            if (method == null)
            {
                continue;
            }

            if (method.DeclaringType == aspectType)
            {
                continue;
            }

            if (method == currentMethod)
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

        var name = method.Name.Replace("$_executor_", "");
        var typeName = method.DeclaringType?.Name ?? "";
        var match = Regex.Match(typeName, @"^<(.+)>d__\d+$");
        return match.Success ? match.Groups[1].Value : name;
    }

    private void WriteData(string outcome, object? result = null)
    {
        var completedAt = DateTime.UtcNow;

        var stepResult = new StepResult
        {
            Guid = _guid,
            CompletedOn = completedAt,
            Duration = (long)((TimeSpan)(completedAt! - _startedAt!)).TotalMilliseconds,
            Result = result?.ToString(),
            Outcome = outcome
        };

        Console.WriteLine($"{MessageType.TmsStepResult}: " + JsonConvert.SerializeObject(stepResult));
    }
}