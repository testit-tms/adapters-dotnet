using Microsoft.Extensions.Logging;
using TestIT.ApiClient.Model;
using TmsRunner.Entities;
using TmsRunner.Entities.Configuration;
using TmsRunner.Managers;
using TmsRunner.Services;

namespace TmsRunner;

public class App(ILogger<App> logger,
                 AdapterConfig adapterConfig,
                 TmsManager tmsManager,
                 TmsSettings tmsSettings,
                 FilterService filterService,
                 RunService runService,
                 ITestRunContextService testRunContext)
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
        TestRunV2GetModel? testRun;
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
                testRun = await tmsManager.GetTestRunAsync(tmsSettings.TestRunId!).ConfigureAwait(false);
                var testCaseForRun = tmsManager.GetAutoTestsForRunAsync(testRun!);
                testCases = filterService.FilterTestCases(adapterConfig.TestAssemblyPath, testCaseForRun, testCases);
                testRunContext.SetCurrentTestRun(testRun!);
                break;
            }
            case 2:
            {
                testRun = await tmsManager.CreateTestRunAsync().ConfigureAwait(false);
                tmsSettings.TestRunId = testRun!.Id.ToString();
                testRunContext.SetCurrentTestRun(testRun);

                if (!string.IsNullOrEmpty(adapterConfig.TmsLabelsOfTestsToRun))
                {
                    testCases = filterService.FilterTestCasesByLabels(adapterConfig, testCases);
                }

                break;
            }
        }

        logger.LogInformation("Running tests: {Count}", testCases.Count);
        
        if (tmsSettings.RerunTestsCount > 0)
        {
            await runService.RunTestsWithRerunsAsync(testCases).ConfigureAwait(false);
        }
        else
        {
            await runService.RunSelectedTestsAsync(testCases).ConfigureAwait(false);
        }

        if (tmsSettings.AdapterMode == 2)
        {
            logger.LogInformation("Test run {TestRunId} finished.", tmsSettings.TestRunId);
        }

        return 0;
    }
}
