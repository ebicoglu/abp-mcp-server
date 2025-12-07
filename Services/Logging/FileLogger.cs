using Microsoft.Extensions.Logging;

namespace AbpMcpServer.Services.Logging;

public class FileLogger : ILogger
{
    private readonly string _name;
    private readonly Func<FileLoggerConfiguration> _getCurrentConfig;
    private static readonly object _lock = new object();

    public FileLogger(string name, Func<FileLoggerConfiguration> getCurrentConfig)
    {
        _name = name;
        _getCurrentConfig = getCurrentConfig;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var config = _getCurrentConfig();

        // Ensure log directory exists
        var logDir = Path.GetDirectoryName(config.Params.LogPath);
        if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        var message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {_name}: {formatter(state, exception)}";

        if (exception != null)
        {
            message += Environment.NewLine + exception.ToString();
        }

        lock (_lock)
        {
            File.AppendAllText(config.Params.LogPath, message + Environment.NewLine);
        }
    }
}

public class FileLoggerConfiguration
{
    public FileLoggerParams Params { get; set; } = new FileLoggerParams();
}

public class FileLoggerParams
{
    public string LogPath { get; set; } = "logs/mcp_server.log";
}
