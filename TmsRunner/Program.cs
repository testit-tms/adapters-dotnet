using CommandLine;
using TestIt.Client.Model;
using Tms.Adapter.Utils;
using TmsRunner.Client;
using TmsRunner.Configuration;
using TmsRunner.Extensions;
using TmsRunner.Logger;
using TmsRunner.Models;
using TmsRunner.Options;
using TmsRunner.Services;
using TmsRunner.Utils;

namespace TmsRunner;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        var config = GetAdapterConfiguration(args);
        var settings = ConfigurationManager.Configure(config.ToInternalConfig(),
            Path.GetDirectoryName(config.TestAssemblyPath)!);

        var log = LoggerFactory.GetLogger(config.IsDebug).ForContext<Program>();

        log.Information("Adapter works in {Mode} mode", settings.AdapterMode);
        log.Debug("Parameters:");
        log.Debug("Runner Path: {Path}", config.RunnerPath);
        log.Debug("Test Assembly Path: {Path}", config.TestAssemblyPath);
        log.Debug("Test Adapter Path: {Path}", config.TestAdapterPath);
        log.Debug("Test Logger Path: {Path}", config.LoggerPath);

        var replacer = new Replacer();
        var reflector = new Reflector();
        var parser = new LogParser(replacer, reflector);
        var apiClient = new TmsClient(settings);
        var testRun = await GetTestRun(apiClient, settings);

        var processorService = new ProcessorService(apiClient, testRun, parser);
        var runner = new Runner(config, processorService);
        runner.InitialiseRunner();

        var testCases = runner.DiscoverTests();
        log.Information("Discovered Tests Count: {Count}", testCases.Count);

        if (testCases.Count == 0)
        {
            log.Information("Can not found tests for run");
            return 1;
        }

        var filterService = new FilterService(replacer);
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
                    settings.TestRunId = testRun.Id.ToString();

                    if (!string.IsNullOrEmpty(config.TmsLabelsOfTestsToRun))
                    {
                        testCases = filterService.FilterTestCasesByLabels(config, testCases);
                    }

                    break;
                }
        }

        log.Information("Running tests: {Count}", testCases.Count);
        var hasUploadErrors = runner.RunSelectedTests(testCases);

        if (settings.AdapterMode == 2)
        {
            var projectGlobalId = (await apiClient.GetProject()).GlobalId;
            var testRunUrl = new Uri(new Uri(settings.Url), $"projects/{projectGlobalId}/test-runs/{settings.TestRunId}/test-results");
            log.Information($"Test run {testRunUrl} finished.");
        }

        return hasUploadErrors ? 1 : 0;
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
                    TmsConfigFile = ac.TmsConfigFile,
                    TmsLabelsOfTestsToRun = ac.TmsLabelsOfTestsToRun
                };
            });

        return config;
    }

    private static async Task<TestRunV2GetModel> GetTestRun(TmsClient apiClient, TmsSettings settings)
    {
        TestRunV2GetModel? testRun = null;

        switch (settings.AdapterMode)
        {
            case 0:
                {
                    testRun = await apiClient.GetTestRun(settings.TestRunId);
                    break;
                }
            case 2:
                {
                    testRun = await apiClient.CreateTestRun();
                    break;
                }
        }

        return testRun;
    }
}