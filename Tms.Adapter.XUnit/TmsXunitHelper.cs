using Tms.Adapter.Core.Attributes;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Service;
using Tms.Adapter.Core.Utils;
using Xunit.Abstractions;

namespace Tms.Adapter.XUnit;

public static class TmsXunitHelper
{
    public static void StartTestContainer(ITestCaseStarting testCaseStarting)
    {
        if (testCaseStarting.TestCase is not ITmsAccessor testResults)
        {
            return;
        }

        StartTestContainer(testResults);
    }

    public static void StartTestCase(ITestCaseMessage testCaseMessage)
    {
        if (testCaseMessage.TestCase is not ITmsAccessor testResults)
        {
            return;
        }

        var testCase = testCaseMessage.TestCase;
        testResults.TestResult = new TestContainer
        {
            Id = Hash.NewId(),
            ClassName = GetClassName(testCase.TestMethod.TestClass.Class.Name),
            Namespace = GetNameSpace(testCase.TestMethod.TestClass.Class.Name),
            Parameters = testCase.TestMethod.Method.GetParameters()
                .Zip(testCase.TestMethodArguments ?? Array.Empty<object>(), (parameter, value) => new
                {
                    parameter,
                    value
                })
                .ToDictionary(x => x.parameter.Name, x => x.value.ToString())
        };
        UpdateTestDataFromAttributes(testResults.TestResult, testCase);
        AdapterManager.Instance.StartTestCase(testResults.ClassContainer.Id, testResults.TestResult);
    }

    public static void MarkTestCaseAsFailed(ITestFailed testFailed)
    {
        if (testFailed.TestCase is not ITmsAccessor testResults)
        {
            return;
        }

        testResults.TestResult.Message = string.Join('\n', testFailed.Messages);
        testResults.TestResult.Trace = string.Join('\n', testFailed.StackTraces);
        testResults.TestResult.Status = Status.Failed;
    }

    public static void MarkTestCaseAsPassed(ITestPassed testPassed)
    {
        if (testPassed.TestCase is not ITmsAccessor testResults)
        {
            return;
        }

        testResults.TestResult.Status = Status.Passed;
    }

    public static void MarkTestCaseAsSkipped(ITestCaseMessage testCaseMessage)
    {
        if (testCaseMessage.TestCase is not ITmsAccessor testResults)
        {
            return;
        }

        testResults.TestResult.Message = testCaseMessage.TestCase.SkipReason;
        testResults.TestResult.Status = Status.Skipped;
    }

    public static void FinishTestCase(ITestCaseMessage testCaseMessage)
    {
        if (testCaseMessage.TestCase is not ITmsAccessor testResults)
        {
            return;
        }

        AdapterManager.Instance.StopTestCase(testResults.TestResult.Id);
        AdapterManager.Instance.StopTestContainer(testResults.ClassContainer.Id);
        AdapterManager.Instance.WriteTestCase(testResults.TestResult.Id, testResults.ClassContainer.Id);
    }

    private static void StartTestContainer(ITmsAccessor testResult)
    {
        var uuid = Hash.NewId();
        testResult.ClassContainer = new ClassContainer
        {
            Id = uuid
        };
        AdapterManager.Instance.StartTestContainer(testResult.ClassContainer);
    }

    private static void UpdateTestDataFromAttributes(TestContainer testResult, ITestCase testCase)
    {
        var methodAttributes = testCase.TestMethod.Method.GetCustomAttributes(typeof(ITmsAttribute));

        foreach (var attribute in methodAttributes)
        {
            switch (((IReflectionAttributeInfo)attribute).Attribute)
            {
                case DescriptionAttribute description:
                    testResult.Description = ReplaceParameters(description.Value, testResult.Parameters);
                    break;

                case DisplayNameAttribute displayName:
                    testResult.DisplayName = ReplaceParameters(displayName.Value, testResult.Parameters);
                    break;

                case ExternalIdAttribute externalId:
                    testResult.ExternalId = ReplaceParameters(externalId.Value, testResult.Parameters);
                    break;

                case LabelsAttribute labels:
                    testResult.Labels = labels.Lables;
                    break;

                case TitleAttribute title:
                    testResult.Title = ReplaceParameters(title.Value, testResult.Parameters);
                    break;

                case WorkItemIdsAttribute workItemIds:
                    testResult.WorkItemIds = workItemIds.Ids;
                    break;

                case LinksAttribute links:
                    testResult.Links.Add(links.Link);
                    break;
            }
        }

        if (string.IsNullOrEmpty(testResult.DisplayName))
        {
            testResult.DisplayName = testCase.DisplayName.Split('.')[^1];
        }

        if (string.IsNullOrEmpty(testResult.ExternalId))
        {
            testResult.ExternalId =
                Hash.GetStringSha256Hash(testResult.Namespace + "." + testResult.ClassName + "." + testResult.DisplayName);
        }
    }

    private static string GetClassName(string value)
    {
        var items = value.Split('.');

        return items[^1];
    }

    private static string GetNameSpace(string value)
    {
        var items = value.Split('.');

        return string.Join('.', items.Take(items.Length - 1));
    }

    private static string ReplaceParameters(string? value, Dictionary<string, string?> parameters)
    {
        if (string.IsNullOrEmpty(value) || parameters is null) return value;

        foreach (var pair in parameters)
        {
            var key = $"{{{pair.Key}}}";
            value = value.Replace(key, pair.Value);
        }

        return value;
    }
}