using Tms.Adapter.Core.Attributes;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Service;
using Xunit.Abstractions;

namespace Tms.Adapter.XUnit;

public static class TmsXunitHelper
{
    public static void StartTestContainer(ITestCaseStarting testCaseStarting)
    {
        if (testCaseStarting.TestCase is not ITestResultAccessor testResults)
        {
            return;
        }

        StartTestContainer(testCaseStarting.TestClass, testResults);
    }

    public static void StartTestCase(ITestCaseMessage testCaseMessage)
    {
        if (testCaseMessage.TestCase is not ITestResultAccessor testResults)
        {
            return;
        }

        var testCase = testCaseMessage.TestCase;
        testResults.TestResult = new TestResult
        {
            Id = NewUuid(testCase.DisplayName),
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
        AdapterManager.Instance.StartTestCase(testResults.TestResultContainer.Id, testResults.TestResult);
    }

    public static void MarkTestCaseAsFailed(ITestFailed testFailed)
    {
        if (testFailed.TestCase is not ITestResultAccessor testResults)
        {
            return;
        }

        testResults.TestResult.Message = string.Join('\n', testFailed.Messages);
        testResults.TestResult.Trace = string.Join('\n', testFailed.StackTraces);
        testResults.TestResult.Status = Status.Failed;
    }

    public static void MarkTestCaseAsPassed(ITestPassed testPassed)
    {
        if (testPassed.TestCase is not ITestResultAccessor testResults)
        {
            return;
        }

        testResults.TestResult.Status = Status.Passed;
    }

    public static void MarkTestCaseAsSkipped(ITestCaseMessage testCaseMessage)
    {
        if (testCaseMessage.TestCase is not ITestResultAccessor testResults)
        {
            return;
        }

        testResults.TestResult.Message = testCaseMessage.TestCase.SkipReason;
        testResults.TestResult.Status = Status.Skipped;
    }

    public static void FinishTestCase(ITestCaseMessage testCaseMessage)
    {
        if (testCaseMessage.TestCase is not ITestResultAccessor testResults)
        {
            return;
        }

        AdapterManager.Instance.StopTestCase(testResults.TestResult.Id);
        AdapterManager.Instance.StopTestContainer(testResults.TestResultContainer.Id);
        AdapterManager.Instance.WriteTestCase(testResults.TestResult.Id, testResults.TestResultContainer.Id);
    }

    private static void StartTestContainer(ITestClass testClass, ITestResultAccessor testResult)
    {
        var uuid = NewUuid(testClass.Class.Name);
        testResult.TestResultContainer = new()
        {
            Id = uuid,
            Name = testClass.Class.Name
        };
        AdapterManager.Instance.StartTestContainer(testResult.TestResultContainer);
    }

    private static string NewUuid(string name)
    {
        var uuid = string.Concat(Guid.NewGuid().ToString(), "-", name);
        return uuid;
    }

    private static string GetStringSha256Hash(string text)
    {
        using var sha = new System.Security.Cryptography.SHA256Managed();
        var textData = System.Text.Encoding.UTF8.GetBytes(text);
        var hash = sha.ComputeHash(textData);
        return BitConverter.ToString(hash).Replace("-", String.Empty);
    }

    private static void UpdateTestDataFromAttributes(TestResult testResult, ITestCase testCase)
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
            testResult.DisplayName = testCase.DisplayName;
        }

        if (string.IsNullOrEmpty(testResult.ExternalId))
        {
            testResult.ExternalId =
                GetStringSha256Hash(testResult.Namespace + "." + testResult.ClassName + "." + testResult.DisplayName);
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