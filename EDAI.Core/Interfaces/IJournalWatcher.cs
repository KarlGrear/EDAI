using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

/// <summary>
/// Watches the active Elite Dangerous journal file for new lines in real time.
/// Uses <see cref="System.IO.FileSystemWatcher"/> to detect file changes, then
/// tail-reads only the newly appended lines.
/// </summary>
public interface IJournalWatcher
{
    /// <summary>Raised on the thread pool whenever a complete new JSON line is received.</summary>
    event EventHandler<JournalLineReceivedEventArgs> JournalLineReceived;

    /// <summary>File name (without path) of the journal file currently being tailed, or null if none.</summary>
    string? CurrentJournalFileName { get; }

    /// <summary>
    /// Starts watching the journal directory and tailing the most recent journal file.
    /// Begins reading from the current end-of-file so past events are not replayed.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops all file watching and releases file handles.</summary>
    Task StopAsync();
}
