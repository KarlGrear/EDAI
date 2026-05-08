using Microsoft.Extensions.Logging;

namespace EDAI.Core.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly StreamWriter _writer;
    private readonly LogLevel _minLevel;
    private readonly object _syncLock = new();
    private bool _disposed;

    public FileLoggerProvider(string filePath, LogLevel minLevel = LogLevel.Information)
    {
        _minLevel = minLevel;
        var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(stream, leaveOpen: false) { AutoFlush = true };
    }

    public ILogger CreateLogger(string categoryName) =>
        new FileLogger(categoryName, this, _minLevel);

    internal void WriteEntry(LogLevel logLevel, string categoryName, string message, Exception? exception)
    {
        if (_disposed) return;
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var level = LevelAbbreviation(logLevel);
        var line = $"[{timestamp}] [{level}] [{categoryName}] {message}";
        try
        {
            lock (_syncLock)
            {
                _writer.WriteLine(line);
                if (exception is not null)
                    _writer.WriteLine($"  Exception: {exception}");
            }
        }
        catch { /* never crash the app due to a logging failure */ }
    }

    private static string LevelAbbreviation(LogLevel level) => level switch
    {
        LogLevel.Trace       => "TRCE",
        LogLevel.Debug       => "DBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning     => "WARN",
        LogLevel.Error       => "ERRO",
        LogLevel.Critical    => "CRIT",
        _                    => "UNKN",
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        lock (_syncLock)
        {
            _writer.Dispose();
        }
    }
}
