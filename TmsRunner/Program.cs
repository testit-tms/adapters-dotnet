using CommandLine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Tms.Adapter.Utils;
using TmsRunner.Client;
using TmsRunner.Configuration;
using TmsRunner.Extensions;
using TmsRunner.Logger;
using TmsRunner.Options;
using TmsRunner.Services;
using TmsRunner.Utils;

namespace TmsRunner;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        var config = GetAdapterConfiguration(args);
        var settings = ConfigurationManager.Configure(config.ToInternalConfig(), Path.GetDirectoryName(config.TestAssemblyPath)!);
        var log = LoggerFactory.GetLogger(config.IsDebug).ForContext<Program>();

        log.Information("Adapter works in {Mode} mode", settings.AdapterMode);
        log.Information("Count tests in parallel is {Count}", settings.AdapterParallelRunCount);
        log.Debug("Parameters:");
        log.Debug("Runner Path: {Path}", config.RunnerPath);
        log.Debug("Test Assembly Path: {Path}", config.TestAssemblyPath);
        log.Debug("Test Adapter Path: {Path}", config.TestAdapterPath);
        log.Debug("Test Logger Path: {Path}", config.LoggerPath);

        var apiClient = new TmsClient(settings);
        var reflector = new Reflector();
        var replacer = new Replacer();
        var parser = new LogParser(replacer, reflector);
        var filterService = new FilterService(replacer);

        if (settings.AdapterMode == 2)
        {
            settings.TestRunId = (await apiClient.CreateTestRun()).Id.ToString();
        }
        
        var processorService = new ProcessorService(apiClient, settings.TestRunId, parser);

        config.TmsAdapterParallelRunCount = settings.AdapterParallelRunCount.ToString();
        var runner = new Runner(config, processorService);
        runner.InitialiseRunner();

        var testCases = runner.DiscoverTests();

        log.Information("Discovered Tests Count: {Count}", testCases.Count);

        if (testCases.Count == 0)
        {
            log.Information("Can not found tests for run");
            return 1;
        }
        
        filterService.CheckDuplicatesOfExternalId(config, testCases);

        switch (settings.AdapterMode)
        {
            case 0:
                {
                    var testCaseForRun = await apiClient.GetAutoTestsForRun(settings.TestRunId);
                    testCases = filterService.FilterTestCases(config.TestAssemblyPath, testCaseForRun, testCases);
                    break;
                }
            case 2:
                {
                    if (!string.IsNullOrEmpty(config.TmsLabelsOfTestsToRun))
                    {
                        testCases = filterService.FilterTestCasesByLabels(config, testCases);
                    }

                    break;
                }
        }

        var failedTestResults = new List<TestResult>();
        var attemptCounter = 0;

        do
        {
            var isLastRun = attemptCounter + 1 > settings.AdapterAutoTestRerunCount;

            if (attemptCounter == 0)
            {
                log.Information("Running tests: {Count}", testCases.Count);
                failedTestResults.AddRange(runner.RunSelectedTests(testCases, isLastRun));
            }
            else
            {
                var failedTestsNames = failedTestResults.Select(r => r.DisplayName).ToList();
                var rerunTestCases = testCases.Where(c => failedTestsNames.Contains(c.DisplayName)).ToList();
                log.Information("Attempt: {attemptCounter}. Rerun tests count: {Count}",
                    attemptCounter,
                    rerunTestCases.Count);
                
                failedTestResults.Clear();
                failedTestResults.AddRange(runner.RunSelectedTests(rerunTestCases, isLastRun));
            }

            attemptCounter++;
        } while (attemptCounter <= settings.AdapterAutoTestRerunCount && failedTestResults.Any());

        if (settings.AdapterMode == 2)
        {
            var projectGlobalId = (await apiClient.GetProject()).GlobalId;
            var testRunUrl = new Uri(new Uri(settings.Url), $"projects/{projectGlobalId}/test-runs/{settings.TestRunId}/test-results");
            log.Information($"Test run {testRunUrl} finished.");
            log.Information($"Count of failed tests: {processorService.TotalCountOfFailedTests}");
        }

        return processorService.UploadError ? 1 : 0;
    }

    private static AdapterConfig GetAdapterConfiguration(IEnumerable<string> args)
    {
        AdapterConfig config = null!;

        Parser.Default.ParseArguments<AdapterConfig>(args)
            .WithParsed(ac =>
            {
                config = new AdapterConfig
                {
                    RunnerPath = ac.RunnerPath.RemoveQuotes(),
                    TestAssemblyPath = ac.TestAssemblyPath.RemoveQuotes(),
                    TestAdapterPath = ac.TestAdapterPath?.RemoveQuotes() ?? string.Empty,
                    LoggerPath = ac.LoggerPath?.RemoveQuotes() ?? string.Empty,
                    IsDebug = ac.IsDebug,
                    TmsUrl = ac.TmsUrl,
                    TmsPrivateToken = ac.TmsPrivateToken,
                    TmsProjectId = ac.TmsProjectId,
                    TmsConfigurationId = ac.TmsConfigurationId,
                    TmsTestRunId = ac.TmsTestRunId,
                    TmsTestRunName = ac.TmsTestRunName,
                    TmsAdapterMode = ac.TmsAdapterMode,
                    TmsAdapterAutoTestRerunCount = ac.TmsAdapterAutoTestRerunCount,
                    TmsAdapterParallelRunCount = ac.TmsAdapterParallelRunCount,
                    TmsConfigFile = ac.TmsConfigFile,
                    TmsLabelsOfTestsToRun = ac.TmsLabelsOfTestsToRun
                };
            });

        return config;
    }
}