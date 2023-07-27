using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using MethodBoundaryAspect.Fody.Attributes;
using Tms.Adapter.Models;
using Tms.Adapter.Utils;

namespace Tms.Adapter.Attributes
{
    public class StepAttribute : OnMethodBoundaryAspect
    {
        private MethodBase? _currentMethod;
        private MethodBase? _callerMethod;
        private string? _title;
        private string? _description;
        private CallerMethodType? _callerMethodType;
        private CallerMethodType? _currentMethodType;
        private DateTime? _startedAt;
        private DateTime? _completedAt;
        private Guid _guid;

        public override void OnEntry(MethodExecutionArgs arg)
        {
            _startedAt = DateTime.UtcNow;
            _guid = Guid.NewGuid();

            var stackTrace = new StackTrace();
            _currentMethod = arg.Method;
            var regex = new Regex("at (.*)\\.([^.]*)\\(");
            var caller = regex.Match(stackTrace.ToString().Split(Environment.NewLine)[2]);
            var type = arg.Instance?.GetType();

            if (type is not null)
            {
                _callerMethod = type.GetMethod(caller.Groups[2].Value);
                if (_callerMethod == null)
                    _callerMethod = type.GetMethod(caller.Groups[2].Value,
                        BindingFlags.Instance | BindingFlags.NonPublic);
            }

            var arguments = arg.Arguments
                .Select(x => x == null ? "null" : x.ToString())
                .ToList();
            var parameters = arg.Method.GetParameters()
                .Select(x => x.Name.ToString())
                .Zip(arguments, (k, v) => new { k, v })
                .ToDictionary(x => x.k, x => x.v);

            var currentMethodAttributes = _currentMethod.GetCustomAttributes(false);

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

            var replacer = new Replacer();

            var newTitle = string.IsNullOrEmpty(_title) ? _currentMethod.Name : _title;
            var newDescription = string.IsNullOrEmpty(_description)
                ? null
                : replacer.ReplaceParameters(_description, parameters);

            var step = new StepDto
            {
                Guid = _guid,
                StartedOn = _startedAt,
                Title = replacer.ReplaceParameters(newTitle, parameters),
                Description = newDescription,
                CurrentMethod = _currentMethod.Name,
                CallerMethod = _callerMethod?.Name.Replace("$_executor_", ""),
                Instance = arg.Instance?.GetType().Name,
                Args = parameters,
                CallerMethodType = _callerMethodType,
                CurrentMethodType = _currentMethodType
            };

            Console.WriteLine($"{MessageType.TmsStep}: " + JsonSerializer.Serialize(step));
        }

        public override void OnExit(MethodExecutionArgs arg)
        {
            WriteData("Passed", arg.ReturnValue);
        }

        public override void OnException(MethodExecutionArgs arg)
        {
            WriteData("Failed");
        }

        private void WriteData(string outcome, object result = null)
        {
            _completedAt = DateTime.UtcNow;

            var stepResult = new StepResult
            {
                Guid = _guid,
                CompletedOn = _completedAt,
                Duration = (long)((TimeSpan)(_completedAt! - _startedAt!)).TotalMilliseconds,
                Result = result?.ToString(),
                Outcome = outcome
            };

            Console.WriteLine($"{MessageType.TmsStepResult}: " + JsonSerializer.Serialize(stepResult));
        }
    }
}