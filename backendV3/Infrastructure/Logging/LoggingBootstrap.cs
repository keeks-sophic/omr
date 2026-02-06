using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace BackendV3.Infrastructure.Logging;

public static class LoggingBootstrap
{
    public static void Configure(IHostBuilder host)
    {
        var logDir = Environment.GetEnvironmentVariable("BACKENDV3_LOG_DIR") ?? Path.Combine(Directory.GetCurrentDirectory(), "logs");
        Directory.CreateDirectory(logDir);
        var logFormat = (Environment.GetEnvironmentVariable("BACKENDV3_LOG_FORMAT") ?? "jsonl").Trim().ToLowerInvariant();
        var logLevel = ParseLevel(Environment.GetEnvironmentVariable("BACKENDV3_LOG_LEVEL"));
        var fileExt = logFormat == "text" ? "log" : "jsonl";
        var filePath = Path.Combine(logDir, $"backendV3-.{fileExt}");

        host.UseSerilog((ctx, services, cfg) =>
        {
            cfg.MinimumLevel.Is(logLevel);
            cfg.MinimumLevel.Override("Microsoft", logLevel > LogEventLevel.Information ? logLevel : LogEventLevel.Information);
            cfg.Enrich.FromLogContext();
            cfg.WriteTo.Console();
            if (logFormat == "text")
            {
                cfg.WriteTo.File(filePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, shared: true);
            }
            else
            {
                cfg.WriteTo.File(new JsonFormatter(renderMessage: true), filePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14, shared: true);
            }
        });
    }

    private static LogEventLevel ParseLevel(string? value)
    {
        if (Enum.TryParse<LogEventLevel>(value ?? string.Empty, ignoreCase: true, out var level))
        {
            return level;
        }

        return LogEventLevel.Information;
    }
}

