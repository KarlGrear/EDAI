using System.Threading.Channels;
using EDAI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.Pipeline;

/// <summary>
/// Global FIFO action queue. A single background consumer processes one action at a
/// time in arrival order, ensuring that display and TTS announcements from concurrent
/// pipeline completions never overlap.
///
/// Threading contract:
///   • <see cref="Enqueue"/> is safe to call from any thread at any time.
///   • The consumer loop runs on a single dedicated async continuation chain —
///     no two actions ever execute concurrently.
///   • Errors thrown by individual actions are caught and logged; the queue
///     continues processing subsequent actions.
/// </summary>
public sealed class ActionQueue : IActionQueue, IAsyncDisposable
{
    private readonly Channel<Func<CancellationToken, Task>> _channel =
        Channel.CreateUnbounded<Func<CancellationToken, Task>>(
            new UnboundedChannelOptions
            {
                SingleReader = true,  // only the consumer loop reads
                AllowSynchronousContinuations = false,
            });

    private readonly ILogger<ActionQueue> _logger;
    private CancellationTokenSource? _cts;
    private Task _consumerTask = Task.CompletedTask;
    private bool _stopped;

    public ActionQueue(ILogger<ActionQueue> logger) => _logger = logger;

    /// <inheritdoc/>
    public void Enqueue(Func<CancellationToken, Task> action)
        => _channel.Writer.TryWrite(action); // TryWrite never blocks on unbounded channel

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _consumerTask = ConsumeAsync(_cts.Token);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync()
    {
        if (_stopped) return;
        _stopped = true;

        // Stop accepting new items and let the consumer drain what remains.
        _channel.Writer.TryComplete();
        try
        {
            await _consumerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { /* expected on forced cancel */ }

        _cts?.Dispose();
    }

    private async Task ConsumeAsync(CancellationToken ct)
    {
        await foreach (var action in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            try
            {
                await action(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Application is shutting down — stop processing immediately.
                break;
            }
            catch (Exception ex)
            {
                // Isolate individual action failures so the queue keeps running.
                _logger.LogWarning(ex, "Action queue: action failed, continuing");
            }
        }
    }

    public ValueTask DisposeAsync() => new(StopAsync());
}
