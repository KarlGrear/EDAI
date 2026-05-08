using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.Logging;

public sealed class ErrorService : IErrorService
{
    private readonly ILoggerFactory _loggerFactory;

    public event EventHandler<EdaiErrorEventArgs>? MinorErrorOccurred;
    public event EventHandler<EdaiErrorEventArgs>? CriticalErrorOccurred;

    public ErrorService(ILoggerFactory loggerFactory) => _loggerFactory = loggerFactory;

    public void ReportMinor(string source, string message, Exception? exception = null)
    {
        _loggerFactory.CreateLogger(source).LogWarning(exception, "{Message}", message);
        MinorErrorOccurred?.Invoke(this, new EdaiErrorEventArgs
        {
            Source = source,
            Message = message,
            Exception = exception,
        });
    }

    public void ReportCritical(string source, string message, Exception? exception = null)
    {
        _loggerFactory.CreateLogger(source).LogError(exception, "{Message}", message);
        CriticalErrorOccurred?.Invoke(this, new EdaiErrorEventArgs
        {
            Source = source,
            Message = message,
            Exception = exception,
        });
    }
}
