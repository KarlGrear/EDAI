using System.Text.Json;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.Pipeline;

/// <summary>
/// Final pipeline stage. Enqueues an action to the global <see cref="IActionQueue"/>
/// so that display, TTS, and logging for every completed pipeline run execute
/// sequentially in FIFO order.
///
/// Trigger and Processing phases complete concurrently across configurations;
/// only the Action phase is serialised, preventing simultaneous announcements.
/// </summary>
public sealed class OutputDispatcher : IOutputDispatcher
{
    private readonly ITtsService _tts;
    private readonly IActionQueue _actionQueue;
    private readonly IResponseLogRepository _logRepo;
    private readonly ISessionHistoryRepository _sessionRepo;
    private readonly IJournalAuxFileReader _auxReader;
    private readonly ILogger<OutputDispatcher> _logger;

    public event EventHandler<AiResponseReceivedEventArgs>? ResponseReceived;

    public OutputDispatcher(
        ITtsService tts,
        IActionQueue actionQueue,
        IResponseLogRepository logRepo,
        ISessionHistoryRepository sessionRepo,
        IJournalAuxFileReader auxReader,
        ILogger<OutputDispatcher> logger)
    {
        _tts        = tts;
        _actionQueue = actionQueue;
        _logRepo    = logRepo;
        _sessionRepo = sessionRepo;
        _auxReader  = auxReader;
        _logger     = logger;
    }

    /// <summary>
    /// Enqueues the completed pipeline context as an action. Returns immediately;
    /// the actual display, TTS, and log write happen on the action queue consumer.
    /// </summary>
    public Task DispatchAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        _actionQueue.Enqueue(ct => ExecuteActionAsync(context, ct));
        return Task.CompletedTask;
    }

    // ── Action phase ─────────────────────────────────────────────────────────
    // Runs on the action queue's single consumer. Steps execute in order:
    //   1. Evaluate DisplayCondition → raise ResponseReceived for UI / tray
    //   2. Evaluate AnnounceCondition → await TTS (blocks next action until done)
    //   3. Write ResponseLog to SQLite
    private async Task ExecuteActionAsync(PipelineContext context, CancellationToken ct)
    {
        var parsed = context.ParsedResponse;
        if (parsed == null) return;

        var now         = DateTime.UtcNow;
        var triggerJson = context.TriggeringEvent.RawJson;
        var resultJson  = context.RawAiResponse;

        // ── 1. Display ────────────────────────────────────────────────────────
        var showDisplay = ConditionEvaluator.Evaluate(
            context.Config.DisplayCondition, triggerJson, resultJson, _auxReader.Read);

        if (showDisplay && (!string.IsNullOrWhiteSpace(parsed.DisplayedOutput) || context.Config.DisplayTitle))
        {
            ResponseReceived?.Invoke(this, new AiResponseReceivedEventArgs
            {
                ConfigTitle     = context.Config.Title,
                DisplayTitle    = context.Config.DisplayTitle,
                DisplayedOutput = parsed.DisplayedOutput,
                AnnouncedOutput = parsed.AnnouncedOutput,
                Timestamp       = now,
            });
        }

        // ── 2. Announce (awaited — next action waits for speech to finish) ────
        var doAnnounce = ConditionEvaluator.Evaluate(
            context.Config.AnnounceCondition, triggerJson, resultJson, _auxReader.Read);

        if (doAnnounce && !string.IsNullOrWhiteSpace(parsed.AnnouncedOutput))
        {
            var ttsText = context.Config.AnnounceTitle
                ? $"{context.Config.Title}. {parsed.AnnouncedOutput}"
                : parsed.AnnouncedOutput;

            try
            {
                await _tts.SpeakAndWaitAsync(ttsText, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Propagate so the consumer stops cleanly on shutdown.
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TTS announce failed for '{Config}'", context.Config.Title);
            }
        }

        // ── 3. Persist response log ───────────────────────────────────────────
        try
        {
            var session = await _sessionRepo.GetCurrentSessionAsync().ConfigureAwait(false);

            var secondaryJson = context.SecondaryEvents.Count > 0
                ? JsonSerializer.Serialize(context.SecondaryEvents.Select(e => e.RawJson))
                : null;

            await _logRepo.AddAsync(new ResponseLogModel
            {
                SessionHistoryId     = session?.Id,
                EventConfigurationId = context.Config.Id,
                Timestamp            = now,
                TriggeringEventJson  = triggerJson,
                SecondaryEventsJson  = secondaryJson,
                PromptSent           = context.BuiltPrompt ?? string.Empty,
                RawAiResponse        = context.RawAiResponse ?? string.Empty,
                DisplayedOutput      = parsed.DisplayedOutput,
                AnnouncedOutput      = parsed.AnnouncedOutput,
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write response log");
        }
    }
}
