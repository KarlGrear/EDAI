using System.Threading.Channels;
using EDAI.Core.Models;

namespace EDAI.Core.Pipeline;

// Per-config serialised queue: at most one pipeline run active at a time per config.
// Additional triggers queue up and are processed in order.
internal sealed class ConfigPipeline
{
    private sealed record Trigger(EventConfigurationModel Config, ParsedJournalEvent Event);

    private readonly Channel<Trigger> _channel = Channel.CreateUnbounded<Trigger>(
        new UnboundedChannelOptions { SingleReader = true });

    private readonly object _lock = new();
    private Task _consumer = Task.CompletedTask;

    public void Enqueue(EventConfigurationModel config, ParsedJournalEvent e)
        => _channel.Writer.TryWrite(new Trigger(config, e));

    public void EnsureRunning(
        Func<EventConfigurationModel, ParsedJournalEvent, Task> processor,
        CancellationToken ct)
    {
        lock (_lock)
        {
            if (!_consumer.IsCompleted) return;
            _consumer = Task.Run(() => ConsumeAsync(processor, ct), ct);
        }
    }

    private async Task ConsumeAsync(
        Func<EventConfigurationModel, ParsedJournalEvent, Task> processor,
        CancellationToken ct)
    {
        try
        {
            await foreach (var t in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                await processor(t.Config, t.Event).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
    }
}
