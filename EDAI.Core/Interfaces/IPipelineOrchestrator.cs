using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

/// <summary>
/// Coordinates the full AI event pipeline: trigger matching, secondary event collection,
/// prompt building, OpenAI call, response parsing, and output dispatch.
/// One pipeline instance per <see cref="EventConfigurationModel"/> is maintained so that
/// concurrent triggers for the same config are queued and processed in order.
/// </summary>
public interface IPipelineOrchestrator
{
    /// <summary>
    /// Feeds a parsed journal event into the pipeline.
    /// Matched configurations are enqueued for processing; unmatched events are discarded.
    /// </summary>
    Task ProcessAsync(ParsedJournalEvent journalEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a journal event through a specific configuration, bypassing trigger matching.
    /// Used by the test screen to exercise a chosen config directly.
    /// </summary>
    Task ProcessWithConfigAsync(ParsedJournalEvent journalEvent, EventConfigurationModel config, CancellationToken cancellationToken = default);
}
