using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tms.Adapter.Core.Attributes;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Service;
using Tms.Adapter.Core.Utils;

namespace Tms.Adapter.MSTest;

public static class TmsHelper
{
    public static string StartTestCase(string containerId, ITestMethod testCase)
    {
        var testResult = new TestContainer
        {
            Id = Hash.NewId(),
            ClassName = GetClassName(testCase.TestClassName),
            Namespace = GetNameSpace(testCase.TestClassName),
            Parameters = testCase.MethodInfo.GetParameters()
                .Zip(testCase.Arguments ?? Array.Empty<object>(), (parameter, value) => new
                {
                    parameter,
                    value
                })
                .ToDictionary(x => x.parameter.Name, x => x.value.ToString())
        };

        UpdateTestDataFromAttributes(testResult, testCase);
        AdapterManager.Instance.StartTestCase(containerId, testResult);

        return testResult.Id;
    }

    public static void UpdateTestCase(string testResultId, TestResult testResult)
    {
        if (testResult.Outcome == UnitTestOutcome.Failed)
        {
            var trace = testResult.TestFailureException.InnerException.StackTrace;
            var message = testResult.TestFailureException.Message;

            AdapterManager.Instance
                .UpdateTestCase(testResultId, x => x.Trace = trace)
                .UpdateTestCase(testResultId, x => x.Message = message)
                .UpdateTestCase(testResultId, x => x.Status = Status.Failed);

            return;
        }

        AdapterManager.Instance.UpdateTestCase(testResultId,
            x => x.Status = x.Status == Status.Undefined ? Status.Passed : x.Status);
    }

    public static void FinishTestCase(string testResultId, string containerId)
    {
        AdapterManager.Instance.StopTestCase(testResultId);
        AdapterManager.Instance.StopTestContainer(containerId);
        AdapterManager.Instance.WriteTestCase(testResultId, containerId);
    }

    public static string StartTestContainer()
    {
        var uuid = Hash.NewId();
        var classContainer = new ClassContainer
        {
            Id = uuid
        };
        AdapterManager.Instance.StartTestContainer(classContainer);

        return classContainer.Id;
    }

    private static void UpdateTestDataFromAttributes(TestContainer testResult, ITestMethod testCase)
    {
        var methodAttributes = testCase.MethodInfo.GetCustomAttributes(false);

        foreach (var attribute in methodAttributes)
        {
            switch (attribute)
            {
                case Tms.Adapter.Core.Attributes.DescriptionAttribute description:
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
            testResult.DisplayName = testCase.MethodInfo.Name.Split('.')[^1];
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