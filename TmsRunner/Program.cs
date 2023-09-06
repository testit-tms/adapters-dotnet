using CommandLine;
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
        var isError = false;
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

        var runner = new Runner(config);
        runner.InitialiseRunner();

        var testCases = runner.DiscoverTests();

        log.Information("Discovered Tests Count: {Count}", testCases.Count);

        if (testCases.Count == 0)
        {
            log.Information("Can not found tests for run");
            return 1;
        }

        ITmsClient apiClient = new TmsClient(settings);

        var replacer = new Replacer();
        var filterService = new FilterService(replacer);

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
                settings.TestRunId = await apiClient.CreateTestRun();

                if (!string.IsNullOrEmpty(config.TmsLabelsOfTestsToRun))
                {
                    testCases = filterService.FilterTestCasesByLabels(config, testCases);
                }
                
                break;
            }
        }

        log.Information("Running tests: {Count}", testCases.Count);

        var testResults = runner.RunSelectedTests(testCases);

        log.Debug("Run Selected Test Result: {@Results}",
            testResults.Select(t => t.DisplayName));

        var reflector = new Reflector();
        var parser = new LogParser(replacer, reflector);
        var processorService =
            new ProcessorService(apiClient, settings, parser);

        foreach (var testResult in testResults)
        {
            log.Information("Uploading test {Name}", testResult.DisplayName);

            try
            {
                await processorService.ProcessAutoTest(testResult);

                log.Information("Uploaded test {Name}", testResult.DisplayName);
            }
            catch (Exception e)
            {
                isError = true;
                log.Error(e, "Uploaded test {Name} is failed", testResult.DisplayName);
            }
        }

        if (settings.AdapterMode == 2)
        {
            var projectGlobalId = (await apiClient.GetProjectModel()).GlobalId;
            var testRunUrl = new Uri(new Uri(config.TmsUrl), $"projects/{projectGlobalId}/test-runs/{settings.TestRunId}/test-results");
            log.Information($"Test run '{testRunUrl}' finished.");
        }

        return isError ? 1 : 0;
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
}