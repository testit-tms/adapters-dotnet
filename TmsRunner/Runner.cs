using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Serilog;
using TmsRunner.Handlers;
using TmsRunner.Logger;
using TmsRunner.Options;
using TmsRunner.Services;

namespace TmsRunner;

public class Runner
{
    private const string DefaultRunSettings =
        @"
<RunSettings>
  <RunConfiguration>
  </RunConfiguration> 
</RunSettings>
";

    private readonly AdapterConfig _config;
    private readonly ILogger _logger;
    private readonly IVsTestConsoleWrapper _consoleWrapper;
    private readonly ProcessorService _processorService;
    private readonly string _runSettings;

    public Runner(AdapterConfig config, ProcessorService processorService)
    {
        _config = config;
        _logger = LoggerFactory.GetLogger().ForContext<Runner>();
        _consoleWrapper = new VsTestConsoleWrapper(config.RunnerPath,
            new ConsoleParameters { LogFilePath = Path.Combine(Directory.GetCurrentDirectory(), @"log.txt") });
        _processorService = processorService;
        _runSettings = string.IsNullOrWhiteSpace(_config.TmsRunSettings) ? DefaultRunSettings : _config.TmsRunSettings;
    }

    public void InitialiseRunner()
    {
        _consoleWrapper.StartSession();

        _logger.Information("Start session");

        var extensions = new List<string>();

        if (File.Exists(_config.TestAdapterPath))
        {
            extensions.Add(_config.TestAdapterPath);

            _logger.Debug("Added test adapter extension");
        }

        if (File.Exists(_config.LoggerPath))
        {
            extensions.Add(_config.LoggerPath);

            _logger.Debug("Added logger extension");
        }

        if (extensions.Count > 0)
        {
            _consoleWrapper.InitializeExtensions(extensions);
        }
    }

    public List<TestCase> DiscoverTests()
    {
        var waitHandle = new AutoResetEvent(false);
        var handler = new DiscoveryEventHandler(waitHandle);

        _consoleWrapper.DiscoverTests(new List<string> { _config.TestAssemblyPath }, _runSettings, handler);

        waitHandle.WaitOne();

        return handler.DiscoveredTestCases;
    }

    public bool RunSelectedTests(IEnumerable<TestCase> testCases)
    {
        var waitHandle = new AutoResetEvent(false);
        var handler = new RunEventHandler(waitHandle, _processorService);
        var retryCounter = 0;

        do
        {
            var testCasesToRun = handler.FailedTestResults.Any() 
                ? testCases.Where(c => handler.FailedTestResults.Select(r => r.DisplayName).Contains(c.DisplayName))
                : testCases;

            handler.FailedTestResults = new();
            _consoleWrapper.RunTests(testCasesToRun, _runSettings, handler);

            retryCounter++;
        } while (handler.FailedTestResults.Any() && retryCounter <= int.Parse(Environment.GetEnvironmentVariable("ADAPTER_AUTOTESTS_RERUN_COUNT") ?? "0"));

        waitHandle.WaitOne();

        return handler.HasUploadErrors;
    }
}
