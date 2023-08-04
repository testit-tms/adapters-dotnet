using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Tms.Adapter.Attributes;
using Tms.Adapter.Utils;
using TmsRunner.Extensions;
using TmsRunner.Options;

namespace TmsRunner.Services;

public class FilterService
{
    private readonly Replacer _replacer;

    public FilterService(Replacer replacer)
    {
        _replacer = replacer;
    }

    // TODO: write unit tests
    public List<TestCase> FilterTestCases(
        string assemblyPath,
        IEnumerable<string> externalIds,
        IEnumerable<TestCase> testCases)
    {
        var testCasesName = testCases.Select(t => t.FullyQualifiedName);
        var testCasesToRun = new List<TestCase>();
        var assembly = Assembly.LoadFrom(assemblyPath);
        var testMethods = new List<MethodInfo>(
            assembly.GetExportedTypes()
                .SelectMany(type => type.GetMethods())
                .Where(m => testCasesName.Contains(m.DeclaringType!.FullName + "." + m.Name))
        );

        foreach (var testMethod in testMethods)
        {
            string? id = null;
            var fullName = testMethod.DeclaringType!.FullName + "." + testMethod.Name;
            var attributes = testMethod.GetCustomAttributes(false);

            foreach (var attribute in attributes)
            {
                if (attribute is ExternalIdAttribute externalId)
                {
                    id = externalId.Value;
                }
            }

            if (string.IsNullOrEmpty(id))
            {
                id = fullName.ComputeHash();
            }
            else
            {
                var parameterNames = testMethod.GetParameters().Select(x => x.Name?.ToString());
                var testCase = testCases.FirstOrDefault(x => x.FullyQualifiedName == fullName);
                var parametersRegex = new Regex("\\((.*)\\)");
                var parameterValues = parametersRegex.Match(testCase!.DisplayName).Groups[1].Value.Split(',');
                var parameterDictionary = parameterNames
                    .Zip(parameterValues, (k, v) => new { k, v })
                    .ToDictionary(x => x.k, x => x.v);
                id = _replacer.ReplaceParameters(id, parameterDictionary!);
            }

            if (externalIds.Contains(id))
            {
                testCasesToRun.AddRange(testCases.Where(x => x.FullyQualifiedName == fullName).ToList());
            }
        }

        return testCasesToRun;
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
