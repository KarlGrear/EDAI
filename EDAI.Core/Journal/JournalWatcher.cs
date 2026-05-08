using System.Text;
using System.Threading.Channels;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.Journal;

/// <summary>
/// Monitors the Elite Dangerous journal directory for the active log file and
/// streams new lines to subscribers in real time using <see cref="System.IO.FileSystemWatcher"/>.
/// A bounded <see cref="System.Threading.Channels.Channel{T}"/> (capacity 1, DropWrite)
/// collapses burst FSW events so each physical read pass happens at most once concurrently.
/// Only complete lines (terminated by <c>\n</c>) are emitted — partial writes are buffered
/// until the next read and the file position is only advanced to the last complete line.
/// </summary>
public sealed class JournalWatcher : IJournalWatcher
{
    private static readonly string JournalDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Saved Games", "Frontier Developments", "Elite Dangerous");

    // Bounded capacity 1 + DropWrite collapses rapid FSW events into at most one
    // pending read. The reader always reads to EOF, so no lines are missed.
    private readonly Channel<bool> _readChannel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    private readonly IErrorService _errorService;
    private readonly ILogger<JournalWatcher> _logger;

    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _cts;
    private Task? _readerTask;

    // Both fields are only read/written under _stateLock.
    private readonly object _stateLock = new();
    private string? _currentFilePath;
    private long _filePosition;

    public event EventHandler<JournalLineReceivedEventArgs>? JournalLineReceived;

    public JournalWatcher(IErrorService errorService, ILogger<JournalWatcher> logger)
    {
        _errorService = errorService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(JournalDirectory))
        {
            _errorService.ReportCritical(nameof(JournalWatcher),
                $"Journal directory not found: {JournalDirectory}");
            return Task.CompletedTask;
        }

        lock (_stateLock)
        {
            _currentFilePath = FindMostRecentJournalFile();
            // Start from end of file — do not replay existing session events.
            _filePosition = _currentFilePath is not null
                ? new FileInfo(_currentFilePath).Length
                : 0;
        }

        if (_currentFilePath is not null)
            _logger.LogInformation("Tailing journal file: {File}", Path.GetFileName(_currentFilePath));
        else
            _logger.LogInformation("No journal files found; waiting for the game to start.");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _watcher = new FileSystemWatcher(JournalDirectory, "Journal.*.log")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += OnFileEvent;
        _watcher.Created += OnFileCreated;
        _watcher.Error   += OnWatcherError;

        _readerTask = Task.Run(() => ReaderLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileEvent;
            _watcher.Created -= OnFileCreated;
            _watcher.Error   -= OnWatcherError;
            _watcher.Dispose();
            _watcher = null;
        }

        _readChannel.Writer.TryComplete();

        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            _cts = null;
        }

        if (_readerTask is not null)
        {
            try { await _readerTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            _readerTask = null;
        }
    }

    // -------------------------------------------------------------------------
    // FileSystemWatcher callbacks
    // -------------------------------------------------------------------------

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        // Only react to changes on the file we are currently tailing.
        bool isCurrent;
        lock (_stateLock)
            isCurrent = string.Equals(e.FullPath, _currentFilePath, StringComparison.OrdinalIgnoreCase);

        if (isCurrent)
            _readChannel.Writer.TryWrite(true);
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        // A new journal file signals a new game session — switch to it.
        _logger.LogInformation("New journal file detected: {File}", e.Name);
        lock (_stateLock)
        {
            _currentFilePath = e.FullPath;
            _filePosition = 0;
        }
        _readChannel.Writer.TryWrite(true);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _errorService.ReportMinor(nameof(JournalWatcher),
            "FileSystemWatcher error — journal monitoring may have stopped.",
            e.GetException());
    }

    // -------------------------------------------------------------------------
    // Reader loop
    // -------------------------------------------------------------------------

    private async Task ReaderLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var _ in _readChannel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                await ReadNewLinesAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
    }

    private async Task ReadNewLinesAsync(CancellationToken ct)
    {
        string? path;
        long startPosition;
        lock (_stateLock)
        {
            path = _currentFilePath;
            startPosition = _filePosition;
        }

        if (path is null) return;

        try
        {
            await using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (stream.Length <= startPosition) return;

            stream.Seek(startPosition, SeekOrigin.Begin);

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct).ConfigureAwait(false);

            var bytes = ms.ToArray();
            if (bytes.Length == 0) return;

            // Split on newlines. All parts except the last are complete lines.
            // The last part is either empty (trailing newline) or a partial line
            // that has not yet been terminated — we leave it for the next read.
            var text = Encoding.UTF8.GetString(bytes);
            var parts = text.Split('\n');

            for (int i = 0; i < parts.Length - 1; i++)
            {
                var line = parts[i].TrimEnd('\r');
                if (!string.IsNullOrWhiteSpace(line))
                {
                    JournalLineReceived?.Invoke(this, new JournalLineReceivedEventArgs
                    {
                        Line = new JournalLine(line, DateTime.UtcNow),
                    });
                }
            }

            // Advance position only to the byte after the last complete newline.
            // Partial content beyond that is re-read next cycle.
            int lastNewlineByte = Array.LastIndexOf(bytes, (byte)'\n');
            if (lastNewlineByte >= 0)
            {
                var newPosition = startPosition + lastNewlineByte + 1;
                lock (_stateLock)
                {
                    // Don't overwrite position if the file was switched mid-read.
                    if (_currentFilePath == path)
                        _filePosition = newPosition;
                }
            }
        }
        catch (FileNotFoundException) { }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _errorService.ReportMinor(nameof(JournalWatcher),
                $"Failed to read journal file: {ex.Message}", ex);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string? FindMostRecentJournalFile()
    {
        var dir = new DirectoryInfo(JournalDirectory);
        return dir.GetFiles("Journal.*.log")
                  .OrderByDescending(f => f.LastWriteTimeUtc)
                  .FirstOrDefault()?.FullName;
    }
}
