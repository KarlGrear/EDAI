using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

/// <summary>
/// Checks an incoming journal event against all enabled <see cref="EventConfigurationModel"/>
/// records and returns every configuration whose trigger list contains a matching event type.
/// </summary>
public interface ITriggerMatcher
{
    /// <summary>
    /// Returns all enabled configurations whose <c>TriggeringEvents</c> list contains
    /// <see cref="ParsedJournalEvent.EventType"/> (case-insensitive OR match).
    /// Returns an empty list when no configurations match.
    /// </summary>
    Task<IReadOnlyList<EventConfigurationModel>> FindMatchesAsync(
        ParsedJournalEvent journalEvent,
        CancellationToken cancellationToken = default);
}
