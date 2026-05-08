using System.Text.Json;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.Pipeline;

public sealed class OutputDispatcher : IOutputDispatcher
{
    private readonly ITtsService _tts;
    private readonly IResponseLogRepository _logRepo;
    private readonly ISessionHistoryRepository _sessionRepo;
    private readonly ILogger<OutputDispatcher> _logger;

    public event EventHandler<AiResponseReceivedEventArgs>? ResponseReceived;

    public OutputDispatcher(
        ITtsService tts,
        IResponseLogRepository logRepo,
        ISessionHistoryRepository sessionRepo,
        ILogger<OutputDispatcher> logger)
    {
        _tts = tts;
        _logRepo = logRepo;
        _sessionRepo = sessionRepo;
        _logger = logger;
    }

    public async Task DispatchAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        var parsed = context.ParsedResponse;
        if (parsed == null) return;

        var now = DateTime.UtcNow;

        // Raise event for UI display and tray notification
        if (!string.IsNullOrWhiteSpace(parsed.DisplayedOutput) ||
            context.Config.TitleDisplayMode != TitleDisplayMode.None)
        {
            ResponseReceived?.Invoke(this, new AiResponseReceivedEventArgs
            {
                ConfigTitle = context.Config.Title,
                TitleDisplayMode = context.Config.TitleDisplayMode,
                DisplayedOutput = parsed.DisplayedOutput,
                AnnouncedOutput = parsed.AnnouncedOutput,
                Timestamp = now
            });
        }

        // Announce via TTS
        if (!string.IsNullOrWhiteSpace(parsed.AnnouncedOutput))
            _tts.Enqueue(parsed.AnnouncedOutput);

        // Write response log
        try
        {
            var session = await _sessionRepo.GetCurrentSessionAsync().ConfigureAwait(false);

            var secondaryJson = context.SecondaryEvents.Count > 0
                ? JsonSerializer.Serialize(context.SecondaryEvents.Select(e => e.RawJson))
                : null;

            await _logRepo.AddAsync(new ResponseLogModel
            {
                SessionHistoryId = session?.Id ?? 0,
                EventConfigurationId = context.Config.Id,
                Timestamp = now,
                TriggeringEventJson = context.TriggeringEvent.RawJson,
                SecondaryEventsJson = secondaryJson,
                PromptSent = context.BuiltPrompt ?? string.Empty,
                RawAiResponse = context.RawAiResponse ?? string.Empty,
                DisplayedOutput = parsed.DisplayedOutput,
                AnnouncedOutput = parsed.AnnouncedOutput
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write response log");
        }
    }
}
