using Microsoft.Extensions.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TmsRunner.Handlers;
using TmsRunner.Models.Configuration;

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
}