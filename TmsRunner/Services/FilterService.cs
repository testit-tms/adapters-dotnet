using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Serilog;
using Tms.Adapter.Attributes;
using Tms.Adapter.Utils;
using TmsRunner.Extensions;
using TmsRunner.Logger;
using TmsRunner.Options;

namespace TmsRunner.Services;

public class FilterService
{
    private readonly Replacer _replacer;
    private readonly ILogger _log;
    private static readonly Regex _parametersRegex = new Regex("\\((.*)\\)");

    public FilterService(Replacer replacer)
    {
        _replacer = replacer;
        _log = LoggerFactory.GetLogger(false).ForContext<FilterService>();
    }

    // TODO: write unit tests
    public List<TestCase> FilterTestCases(
        string assemblyPath,
        IEnumerable<string> externalIds,
        IEnumerable<TestCase> testCases)
    {
        var testCasesToRun = new List<TestCase>();
        var assembly = Assembly.LoadFrom(assemblyPath);
        var allTestMethods = new List<MethodInfo>(assembly.GetExportedTypes().SelectMany(type => type.GetMethods()));

        foreach (var testCase in testCases)
        {
            var testMethod = allTestMethods.FirstOrDefault(
                m => (m.DeclaringType!.FullName + "." + m.Name).Contains(_parametersRegex.Replace(testCase.FullyQualifiedName, string.Empty))
            );

            if (testMethod == null)
            {
                _log.Error("TestMethod {@FullyQualifiedName} not found", testCase.FullyQualifiedName);
                continue;
            }

            var externalId = GetExternalId(testCase, testMethod);

            if (externalIds.Contains(externalId))
            {
                testCasesToRun.Add(testCase);
            }
        }

        return testCasesToRun;
    }

    private string GetExternalId(TestCase testCase, MethodInfo testMethod)
    {
        var attributes = testMethod.GetCustomAttributes(false);

        foreach (var attribute in attributes)
        {
            if (attribute is ExternalIdAttribute externalId)
            {
                var parameterNames = testMethod.GetParameters().Select(x => x.Name?.ToString());
                var parameterValues = _parametersRegex.Match(testCase.DisplayName).Groups[1].Value.Split(',').Select(x => x.Replace("\"", string.Empty));
                var parameterDictionary = parameterNames
                    .Zip(parameterValues, (k, v) => new { k, v })
                    .ToDictionary(x => x.k, x => x.v);
                return _replacer.ReplaceParameters(externalId.Value, parameterDictionary!);
            }
        }

        return (testMethod.DeclaringType!.FullName + "." + testMethod.Name).ComputeHash();
    }

    public List<TestCase> FilterTestCasesByLabels(
        AdapterConfig config,
        IEnumerable<TestCase> testCases)
    {
        var labelsToRun = config.TmsLabelsOfTestsToRun.Split(',').Select(x => x.Trim()).ToList();
        var testCasesName = testCases.Select(t => t.FullyQualifiedName);
        var testCasesToRun = new List<TestCase>();
        var assembly = Assembly.LoadFrom(config.TestAssemblyPath);
        var testMethods = new List<MethodInfo>(
            assembly.GetExportedTypes()
                .SelectMany(type => type.GetMethods())
                .Where(m => testCasesName.Contains(m.DeclaringType!.FullName + "." + m.Name))
        );

        foreach (var testMethod in testMethods)
        {
            var fullName = testMethod.DeclaringType!.FullName + "." + testMethod.Name;

            foreach (var attribute in testMethod.GetCustomAttributes(false))
            {
                if (attribute is LabelsAttribute labelsAttr)
                {
                    if (labelsAttr.Value.Any(labelsToRun.Contains))
                    {
                        testCasesToRun.Add(testCases.FirstOrDefault(x => x.FullyQualifiedName == fullName));
                    }
                }
            } 
        }

        return testCasesToRun;
    }
}
