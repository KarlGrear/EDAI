using System.Text.Json;
using System.Text.Json.Nodes;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using EDAI.Core.Scripting;
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
    private readonly IScriptingService _scriptingService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<OutputDispatcher> _logger;

    public event EventHandler<AiResponseReceivedEventArgs>? ResponseReceived;

    public OutputDispatcher(
        ITtsService tts,
        IActionQueue actionQueue,
        IResponseLogRepository logRepo,
        ISessionHistoryRepository sessionRepo,
        IJournalAuxFileReader auxReader,
        IScriptingService scriptingService,
        ISessionService sessionService,
        ILogger<OutputDispatcher> logger)
    {
        _tts              = tts;
        _actionQueue      = actionQueue;
        _logRepo          = logRepo;
        _sessionRepo      = sessionRepo;
        _auxReader        = auxReader;
        _scriptingService = scriptingService;
        _sessionService   = sessionService;
        _logger           = logger;
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

        var now           = DateTime.UtcNow;
        var triggerJson   = context.TriggeringEvent.RawJson;
        var resultJson    = context.RawAiResponse;
        var secondaryJson = context.SecondaryJson;

        // ── 1. Display ────────────────────────────────────────────────────────
        bool showDisplay;
        if (!string.IsNullOrWhiteSpace(context.Config.DisplayConditionScript))
        {
            var globals = BuildScriptGlobals(context);
            showDisplay = await _scriptingService
                .EvaluateConditionAsync(context.Config.DisplayConditionScript, globals, ct)
                .ConfigureAwait(false);
        }
        else
        {
            showDisplay = ConditionEvaluator.Evaluate(
                context.Config.DisplayCondition, triggerJson, resultJson, _auxReader.Read, secondaryJson);
        }

        if (showDisplay && (!string.IsNullOrWhiteSpace(parsed.DisplayedOutput) || context.Config.DisplayTitle))
        {
            ResponseReceived?.Invoke(this, new AiResponseReceivedEventArgs
            {
                ConfigTitle          = context.Config.Title,
                DisplayTitle         = context.Config.DisplayTitle,
                DisplayedOutput      = parsed.DisplayedOutput,
                AnnouncedOutput      = parsed.AnnouncedOutput,
                Timestamp            = now,
                ShowTrayNotification = context.Config.ShowTrayNotification,
                PromptSent           = context.BuiltPrompt      ?? string.Empty,
                RawAiResponse        = context.RawAiResponse    ?? string.Empty,
            });
        }

        // ── 2. Announce (awaited — next action waits for speech to finish) ────
        bool doAnnounce;
        if (!string.IsNullOrWhiteSpace(context.Config.AnnounceConditionScript))
        {
            var globals = BuildScriptGlobals(context);
            doAnnounce = await _scriptingService
                .EvaluateConditionAsync(context.Config.AnnounceConditionScript, globals, ct)
                .ConfigureAwait(false);
        }
        else
        {
            doAnnounce = ConditionEvaluator.Evaluate(
                context.Config.AnnounceCondition, triggerJson, resultJson, _auxReader.Read, secondaryJson);
        }

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

            var secondaryLogJson = context.SecondaryEvents.Count > 0
                ? JsonSerializer.Serialize(context.SecondaryEvents.Select(e => e.RawJson))
                : null;

            await _logRepo.AddAsync(new ResponseLogModel
            {
                SessionHistoryId     = session?.Id,
                EventConfigurationId = context.Config.Id,
                Timestamp            = now,
                TriggeringEventJson  = triggerJson,
                SecondaryEventsJson  = secondaryLogJson,
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

    private ScriptGlobals BuildScriptGlobals(PipelineContext context)
    {
        static JsonNode? ParseOrNull(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonNode.Parse(json); } catch { return null; }
        }

        JsonArray? secondaryArray = null;
        if (context.SecondaryEvents.Count > 0)
        {
            secondaryArray = [];
            foreach (var ev in context.SecondaryEvents)
            {
                var node = ParseOrNull(ev.RawJson);
                if (node != null) secondaryArray.Add(node);
            }
        }

        return new ScriptGlobals(_sessionService)
        {
            Trigger     = ParseOrNull(context.TriggeringEvent.RawJson),
            Secondary   = secondaryArray,
            NavRoute    = ParseOrNull(_auxReader.Read("navroute")),
            Status      = ParseOrNull(_auxReader.Read("status")),
            Market      = ParseOrNull(_auxReader.Read("market")),
            Outfitting  = ParseOrNull(_auxReader.Read("outfitting")),
            Shipyard    = ParseOrNull(_auxReader.Read("shipyard")),
            ShipLocker  = ParseOrNull(_auxReader.Read("shipLocker")),
            ModulesInfo = ParseOrNull(_auxReader.Read("modulesinfo")),
        };
    }
}
