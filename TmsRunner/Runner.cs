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
    private readonly string _runSettings;
    private readonly ProcessorService _processorService;

    public Runner(AdapterConfig config, ProcessorService processorService)
    {
        _config = config;
        _logger = LoggerFactory.GetLogger().ForContext<Runner>();
        _consoleWrapper = new VsTestConsoleWrapper(config.RunnerPath,
            new ConsoleParameters { LogFilePath = Path.Combine(Directory.GetCurrentDirectory(), @"log.txt") });
        _runSettings = string.IsNullOrWhiteSpace(_config.TmsRunSettings) ? DefaultRunSettings : _config.TmsRunSettings;
        _processorService = processorService;
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

    public List<TestResult> RunSelectedTests(List<TestCase> testCases, bool isLastRun)
    {
        using var waitHandle = new AutoResetEvent(false);
        var handler = new RunEventHandler(waitHandle, isLastRun, _processorService);

        _consoleWrapper.RunTests(testCases, _runSettings, handler);

        waitHandle.WaitOne();

        return handler.FailedTestResults;
    }
}
