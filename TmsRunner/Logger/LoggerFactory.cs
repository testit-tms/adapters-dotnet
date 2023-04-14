using Serilog;

namespace TmsRunner.Logger;

public static class LoggerFactory
{
    private static ILogger _logger;
    private static readonly object Lock = new object();

    public static ILogger GetLogger(bool isDebug = false)
    {
        if (_logger != null) return _logger;

        lock (Lock)
        {
            if (_logger != null) return _logger;

            var logConfig = isDebug
                ? new LoggerConfiguration()
                    .MinimumLevel.Debug()
                : new LoggerConfiguration();

            _logger = logConfig
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    outputTemplate:
                    "{Timestamp:HH:mm} [{Level}] ({ThreadId}) {SourceContext}: {Message}{NewLine}{Exception}")
                .CreateLogger();

            return _logger;
        }
    }
}