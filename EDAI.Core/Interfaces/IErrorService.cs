using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

/// <summary>
/// Central error reporting hub. All exceptions caught at service boundaries are routed
/// here so they are logged to EDAI.log and surfaced to the UI in a consistent way.
/// </summary>
public interface IErrorService
{
    /// <summary>
    /// Raised when a non-fatal error occurs (e.g. a single parse failure or TTS hiccup).
    /// The UI shows this in the status bar only — no dialog is displayed.
    /// </summary>
    event EventHandler<EdaiErrorEventArgs> MinorErrorOccurred;

    /// <summary>
    /// Raised when a serious error occurs (e.g. DB unavailable, bad API key).
    /// The UI shows a modal error dialog in addition to updating the status bar.
    /// </summary>
    event EventHandler<EdaiErrorEventArgs> CriticalErrorOccurred;

    /// <summary>Logs and surfaces a non-fatal error. Never throws.</summary>
    void ReportMinor(string source, string message, Exception? exception = null);

    /// <summary>Logs and surfaces a fatal error. Never throws.</summary>
    void ReportCritical(string source, string message, Exception? exception = null);
}
