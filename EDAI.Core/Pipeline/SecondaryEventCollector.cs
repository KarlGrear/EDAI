using EDAI.Core.Models;

namespace EDAI.Core.Pipeline;

internal sealed class SecondaryEventCollector
{
    private readonly HashSet<string> _eventTypes;
    private readonly List<ParsedJournalEvent> _collected = [];
    private readonly object _lock = new();
    private bool _closed;

    public SecondaryEventCollector(IEnumerable<string> eventTypes)
    {
        _eventTypes = new HashSet<string>(eventTypes, StringComparer.OrdinalIgnoreCase);
    }

    public void TryAccept(ParsedJournalEvent e)
    {
        if (_closed || !_eventTypes.Contains(e.EventType)) return;
        lock (_lock)
        {
            if (!_closed) _collected.Add(e);
        }
    }

    public async Task<IReadOnlyList<ParsedJournalEvent>> WaitAndCollectAsync(
        int waitMs, CancellationToken ct)
    {
        try { await Task.Delay(waitMs, ct); }
        catch (OperationCanceledException) { }
        lock (_lock) { _closed = true; }
        return _collected.AsReadOnly();
    }
}
