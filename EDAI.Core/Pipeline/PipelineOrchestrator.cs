using System.Collections.Concurrent;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.Pipeline;

/// <summary>
/// Routes incoming journal events through the full AI pipeline.
/// Maintains one <see cref="ConfigPipeline"/> queue per configuration so that
/// rapid re-triggers for the same config are processed in order without overlap.
/// Secondary event collection happens concurrently for all active pipelines via a
/// shared <see cref="SecondaryEventCollector"/> list.
/// </summary>
public sealed class PipelineOrchestrator : IPipelineOrchestrator
{
    private readonly ITriggerMatcher _triggerMatcher;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IOpenAIService _openAiService;
    private readonly IResponseParser _responseParser;
    private readonly IOutputDispatcher _outputDispatcher;
    private readonly IErrorService _errorService;
    private readonly ILogger<PipelineOrchestrator> _logger;

    private readonly ConcurrentDictionary<int, ConfigPipeline> _pipelines = new();
    private readonly List<SecondaryEventCollector> _activeCollectors = [];
    private readonly object _collectorsLock = new();

    public PipelineOrchestrator(
        ITriggerMatcher triggerMatcher,
        IPromptBuilder promptBuilder,
        IOpenAIService openAiService,
        IResponseParser responseParser,
        IOutputDispatcher outputDispatcher,
        IErrorService errorService,
        ILogger<PipelineOrchestrator> logger)
    {
        _triggerMatcher = triggerMatcher;
        _promptBuilder = promptBuilder;
        _openAiService = openAiService;
        _responseParser = responseParser;
        _outputDispatcher = outputDispatcher;
        _errorService = errorService;
        _logger = logger;
    }

    public async Task ProcessAsync(ParsedJournalEvent journalEvent, CancellationToken cancellationToken = default)
    {
        // Feed incoming event to any active secondary collectors first.
        lock (_collectorsLock)
            foreach (var c in _activeCollectors)
                c.TryAccept(journalEvent);

        IReadOnlyList<EventConfigurationModel> matches;
        try
        {
            matches = await _triggerMatcher.FindMatchesAsync(journalEvent, cancellationToken)
                                           .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _errorService.ReportMinor(nameof(PipelineOrchestrator),
                $"Trigger matching failed: {ex.Message}", ex);
            return;
        }

        foreach (var config in matches)
        {
            var pipeline = _pipelines.GetOrAdd(config.Id, _ => new ConfigPipeline());
            pipeline.Enqueue(config, journalEvent);
            pipeline.EnsureRunning(RunAsync, cancellationToken);
        }
    }

    public Task ProcessWithConfigAsync(
        ParsedJournalEvent journalEvent,
        EventConfigurationModel config,
        CancellationToken cancellationToken = default)
    {
        lock (_collectorsLock)
            foreach (var c in _activeCollectors)
                c.TryAccept(journalEvent);

        var pipeline = _pipelines.GetOrAdd(config.Id, _ => new ConfigPipeline());
        pipeline.Enqueue(config, journalEvent);
        pipeline.EnsureRunning(RunAsync, cancellationToken);
        return Task.CompletedTask;
    }

    private async Task RunAsync(EventConfigurationModel config, ParsedJournalEvent triggeringEvent)
    {
        _logger.LogInformation("Pipeline: {Config} triggered by {Event}",
            config.Title, triggeringEvent.EventType);
        try
        {
            // Collect secondary events during the configured wait window.
            IReadOnlyList<ParsedJournalEvent> secondary = [];
            if (config.SecondaryEvents.Count > 0 && config.SecondaryWaitTimeMs > 0)
            {
                var collector = new SecondaryEventCollector(config.SecondaryEvents);
                lock (_collectorsLock) _activeCollectors.Add(collector);
                try
                {
                    secondary = await collector
                        .WaitAndCollectAsync(config.SecondaryWaitTimeMs, CancellationToken.None)
                        .ConfigureAwait(false);
                }
                finally
                {
                    lock (_collectorsLock) _activeCollectors.Remove(collector);
                }
            }

            var context = new PipelineContext
            {
                Config = config,
                TriggeringEvent = triggeringEvent,
                SecondaryEvents = secondary,
            };

            _promptBuilder.Build(context);

            context.RawAiResponse = await _openAiService
                .SendAsync(context.BuiltPrompt!, CancellationToken.None)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(context.RawAiResponse)) return;

            context.ParsedResponse = _responseParser.Parse(context.RawAiResponse, config);

            await _outputDispatcher
                .DispatchAsync(context, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _errorService.ReportMinor(nameof(PipelineOrchestrator),
                $"Pipeline run failed for '{config.Title}': {ex.Message}", ex);
        }
    }
}
