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
        TestRunV2ApiResult? testRun = null;
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
                testRun = await tmsManager.GetTestRunAsync().ConfigureAwait(false);
                var testCaseForRun = await tmsManager.GetExternalIdsForRunAsync().ConfigureAwait(false);

                logger.LogDebug(
                    "Autotests for run from test run {TestRunId} with configuration {ConfigurationId}: {@ExternalIds}",
                    tmsSettings.TestRunId,
                    tmsSettings.ConfigurationId,
                    testCaseForRun);

                testCases = filterService.FilterTestCases(adapterConfig.TestAssemblyPath, testCaseForRun, testCases);
                testRunContext.SetCurrentTestRun(testRun!);
                break;
            }
            case 1:
                testRun = await tmsManager.GetTestRunAsync().ConfigureAwait(false);

                testRunContext.SetCurrentTestRun(testRun!);
                break;
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
        
        bool runSuccess;
        if (tmsSettings.RerunTestsCount > 0)
        {
            runSuccess = await runService.RunTestsWithRerunsAsync(testCases, tmsSettings.RerunTestsCount).ConfigureAwait(false);
        }
        else
        {
            runSuccess = await runService.RunSelectedTestsAsync(testCases).ConfigureAwait(false);
        }

        if (tmsSettings.AdapterMode != 2 && !string.IsNullOrEmpty(tmsSettings.TestRunName) && testRun != null && !testRun.Name.Equals(tmsSettings.TestRunName)) {
            testRun.Name = tmsSettings.TestRunName;

            await tmsManager.UpdateTestRunAsync(testRun).ConfigureAwait(false);
        }

        if (tmsSettings.AdapterMode == 2)
        {
            var project = await tmsManager.GetProjectByIdAsync().ConfigureAwait(false);
            var testRunUrl = new Uri(new Uri(tmsSettings.Url!), $"projects/{project.GlobalId}/test-runs/{tmsSettings.TestRunId}/test-results");
            var failedTests = runService.GetFailedTestCasesCount();
            
            logger.LogInformation("Test run {TestRunUrl} finished.", testRunUrl);
            logger.LogInformation("Count of failed tests: {Count}", failedTests);
        }
        
        return runSuccess ? 0 : 1;
    }
}
