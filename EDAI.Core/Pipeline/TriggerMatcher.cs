using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.Pipeline;

public sealed class TriggerMatcher : ITriggerMatcher
{
    private readonly IEventConfigurationRepository _repo;
    private readonly ILogger<TriggerMatcher> _logger;

    public TriggerMatcher(IEventConfigurationRepository repo, ILogger<TriggerMatcher> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EventConfigurationModel>> FindMatchesAsync(
        ParsedJournalEvent journalEvent,
        CancellationToken cancellationToken = default)
    {
        var configs = await _repo.GetEnabledAsync();
        var matches = configs
            .Where(c => c.TriggeringEvents.Any(t =>
                string.Equals(t, journalEvent.EventType, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (matches.Count > 0)
            _logger.LogDebug("Event {Type} matched {Count} config(s)", journalEvent.EventType, matches.Count);

        return matches;
    }
}
