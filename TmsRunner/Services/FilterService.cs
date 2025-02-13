using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Reflection;
using System.Text.RegularExpressions;
using Tms.Adapter.Attributes;
using Tms.Adapter.Utils;
using TmsRunner.Entities.Configuration;
using TmsRunner.Extensions;

namespace TmsRunner.Services;

public sealed class FilterService(ILogger<FilterService> logger, Replacer replacer)
{
    private static readonly Regex ParametersRegex = new("\\((.*)\\)");

    // TODO: write unit tests
    public List<TestCase> FilterTestCases(string? assemblyPath,
                                          IReadOnlyCollection<string?>? externalIds,
                                          IReadOnlyCollection<TestCase> testCases)
    {
        var testCasesToRun = new List<TestCase>();
        var assembly = Assembly.LoadFrom(assemblyPath ?? string.Empty);
        var allTestMethods = new List<MethodInfo>(assembly.GetExportedTypes().SelectMany(type => type.GetMethods()));

        foreach (var testCase in testCases)
        {
            var testMethod = allTestMethods.FirstOrDefault(
                m => (m.DeclaringType!.FullName + "." + m.Name).Contains(ParametersRegex.Replace(testCase.FullyQualifiedName, string.Empty))
            );

            if (testMethod == null)
            {
                logger.LogError("TestMethod {@FullyQualifiedName} not found", testCase.FullyQualifiedName);
                continue;
            }

            var externalId = GetExternalId(testMethod, testCase);

            if (externalIds?.Contains(externalId) ?? false)
            {
                testCasesToRun.Add(testCase);
            }
        }

        return testCasesToRun;
    }

    private string GetExternalId(MethodInfo testMethod, TestCase testCase)
    {
        var attributes = testMethod.GetCustomAttributes(false);

        foreach (var attribute in attributes)
        {
            if (attribute is ExternalIdAttribute externalId)
            {
                var parameterNames = testMethod.GetParameters().Select(x => x.Name?.ToString());
                var parameterValues = ParametersRegex.Match(testCase.DisplayName).Groups[1].Value.Split(',').Select(x => x.Replace("\"", string.Empty));
                var parameterDictionary = parameterNames
                    .Select(x => x ?? string.Empty)
                    .Zip(parameterValues, (k, v) => new { k, v })
                    .ToDictionary(x => x.k, x => x.v);
                return Replacer.ReplaceParameters(externalId.Value, parameterDictionary);
            }
        }

        return (testMethod.DeclaringType!.FullName + "." + testMethod.Name).ComputeHash();
    }

    public List<TestCase> FilterTestCasesByLabels(AdapterConfig config, IReadOnlyCollection<TestCase> testCases)
    {
        var labelsToRun = config.TmsLabelsOfTestsToRun?.Split(',').Select(x => x.Trim()).ToList();
        var testCasesToRun = new List<TestCase>();
        var assembly = Assembly.LoadFrom(config.TestAssemblyPath ?? string.Empty);
        var allTestMethods = new List<MethodInfo>(assembly.GetExportedTypes().SelectMany(type => type.GetMethods()));

        foreach (var testCase in testCases)
        {
            var testMethod = allTestMethods.FirstOrDefault(
                m => (m.DeclaringType!.FullName + "." + m.Name).Contains(ParametersRegex.Replace(testCase.FullyQualifiedName, string.Empty))
            );

            if (testMethod == null)
            {
                logger.LogError("TestMethod {@FullyQualifiedName} not found", testCase.FullyQualifiedName);
                continue;
            }

            var customAttributes = testMethod.GetCustomAttributes(false);

            foreach (var attribute in customAttributes)
            {
                if (attribute is LabelsAttribute labelsAttr)
                {
                    if (labelsAttr.Value?.Any(x => labelsToRun?.Contains(x) ?? false) ?? false)
                    {
                        if (testCase != null)
                        {
                            testCasesToRun.Add(testCase);
                        }
                    }
                }
            }
        }

        return testCasesToRun;
    }
}
