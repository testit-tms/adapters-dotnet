using Microsoft.Extensions.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TmsRunner.Entities.Configuration;
using TmsRunner.Handlers;

namespace TmsRunner.Services;

public sealed class RunService(ILogger<RunService> logger,
                           IVsTestConsoleWrapper consoleWrapper,
                           AdapterConfig config,
                           DiscoveryEventHandler discoveryEventHandler,
                           RunEventHandler runEventHandler)
{
    public void InitialiseRunner()
    {
        consoleWrapper.StartSession();

        logger.LogInformation("Start session");

        var extensions = new List<string>();

        if (File.Exists(config.TestAdapterPath))
        {
            extensions.Add(config.TestAdapterPath);

            logger.LogDebug("Added test adapter extension");
        }

        if (File.Exists(config.LoggerPath))
        {
            extensions.Add(config.LoggerPath);

            logger.LogDebug("Added logger extension");
        }

        if (extensions.Count > 0)
        {
            consoleWrapper.InitializeExtensions(extensions);
        }
    }

    public List<TestCase> DiscoverTests()
    {
        consoleWrapper.DiscoverTests([config.TestAssemblyPath ?? string.Empty], config.TmsRunSettings, discoveryEventHandler);
        discoveryEventHandler.WaitForEnd();

        return discoveryEventHandler.GetTestCases();
    }

    public async Task RunSelectedTestsAsync(IEnumerable<TestCase> testCases)
    {
        consoleWrapper.RunTests(testCases, config.TmsRunSettings, runEventHandler);
        runEventHandler.WaitForEnd();
        await Task.WhenAll(runEventHandler.GetProcessTestResultsTasks()).ConfigureAwait(false);
    }
    
    public async Task RunTestsWithRerunsAsync(IEnumerable<TestCase> initialTestCases)
    {
        var currentRun = 1;
        var maxRuns = (int.TryParse(config.TmsRerunTestsCount, out int rerunCount) ? rerunCount : 0) + 1; // +1 for initial run
        var testCasesToRun = initialTestCases.ToList();

        while (testCasesToRun.Count != 0)
        {
            logger.LogInformation(
                "Running tests (Attempt {CurrentRun} of {MaxRuns}), Number of tests: {TestCount}", 
                currentRun, 
                maxRuns, 
                testCasesToRun.Count);

            await RunSelectedTestsAsync(testCasesToRun).ConfigureAwait(false);

            if (currentRun >= maxRuns)
            {
                break;
            }

            testCasesToRun = runEventHandler.GetFailedTestCases().ToList();
            runEventHandler.ClearFailedTestCases();

            if (testCasesToRun.Count == 0)
            {
                logger.LogInformation("No failed tests to rerun");
                break;
            }

            logger.LogInformation("Found {FailedCount} failed tests to rerun", testCasesToRun.Count);
            currentRun++;
        }
    }
}