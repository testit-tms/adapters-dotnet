using Microsoft.Extensions.Logging;
using TmsRunner.Managers;
using TmsRunner.Models;
using TmsRunner.Models.Configuration;
using TmsRunner.Services;

namespace TmsRunner;

public class App(ILogger<App> logger,
                 AdapterConfig adapterConfig,
                 TmsManager tmsManager,
                 TmsSettings tmsSettings,
                 FilterService filterService,
                 RunService runService)
{
    public async Task<int> RunAsync()
    {
        logger.LogInformation("Adapter works in {Mode} mode", tmsSettings.AdapterMode);
        logger.LogDebug("Parameters:");
        logger.LogDebug("Runner Path: {Path}", adapterConfig.RunnerPath);
        logger.LogDebug("Test Assembly Path: {Path}", adapterConfig.TestAssemblyPath);
        logger.LogDebug("Test Adapter Path: {Path}", adapterConfig.TestAdapterPath);
        logger.LogDebug("Test Logger Path: {Path}", adapterConfig.LoggerPath);

        runService.InitialiseRunner();
        var testCases = runService.DiscoverTests();
        logger.LogInformation("Discovered Tests Count: {Count}", testCases.Count);

        if (testCases.Count == 0)
        {
            logger.LogInformation("Can not found tests for run");

            return 1;
        }

        switch (tmsSettings.AdapterMode)
        {
            case 0:
                {
                    var testCaseForRun = await tmsManager.GetAutoTestsForRunAsync(tmsSettings.TestRunId).ConfigureAwait(false);
                    testCases = filterService.FilterTestCases(adapterConfig.TestAssemblyPath, testCaseForRun, testCases);

                    break;
                }
            case 2:
                {
                    tmsSettings.TestRunId = await tmsManager.CreateTestRunAsync().ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(adapterConfig.TmsLabelsOfTestsToRun))
                    {
                        testCases = FilterService.FilterTestCasesByLabels(adapterConfig, testCases);
                    }

                    break;
                }
        }

        logger.LogInformation("Running tests: {Count}", testCases.Count);
        await runService.RunSelectedTestsAsync(testCases).ConfigureAwait(false);

        if (tmsSettings.AdapterMode == 2)
        {
            logger.LogInformation("Test run {TestRunId} finished.", tmsSettings.TestRunId);
        }

        return 0;
    }
}
