using Microsoft.Extensions.Logging;

namespace EDAI.Core.Logging;

internal sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerProvider _provider;
    private readonly LogLevel _minLevel;

    internal FileLogger(string categoryName, FileLoggerProvider provider, LogLevel minLevel)
    {
        _categoryName = categoryName;
        _provider = provider;
        _minLevel = minLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None && logLevel >= _minLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        _provider.WriteEntry(logLevel, _categoryName, formatter(state, exception), exception);
    }
}
