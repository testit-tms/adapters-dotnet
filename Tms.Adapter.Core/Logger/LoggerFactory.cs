using Serilog;
using Serilog.Extensions.Logging;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace Tms.Adapter.Core.Logger;

public static class LoggerFactory
{
    private static ILoggerFactory _logger;
    private static readonly object Lock = new object();

    public static ILoggerFactory GetLogger(bool isDebug = false)
    {
        if (_logger != null) return _logger;

        lock (Lock)
        {
            if (_logger != null) return _logger;

            var logConfig = isDebug
                ? new LoggerConfiguration()
                    .MinimumLevel.Debug()
                : new LoggerConfiguration();

            var serilogLogger = logConfig
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    outputTemplate:
                    "{Timestamp:HH:mm} [{Level}] ({ThreadId}) {SourceContext}: {Message}{NewLine}{Exception}")
                .CreateLogger();

            return new SerilogLoggerFactory(serilogLogger);
        }
    }
}