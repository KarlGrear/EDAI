namespace EDAI.Core.Interfaces;

/// <summary>
/// Global FIFO queue for pipeline action-phase work (display, TTS, logging).
/// Actions are executed one at a time in the order they were enqueued, so that
/// concurrent pipeline completions never overlap during announcement.
/// </summary>
public interface IActionQueue
{
    /// <summary>
    /// Adds an action to the tail of the queue. Thread-safe; returns immediately.
    /// The action receives a <see cref="CancellationToken"/> that is cancelled when
    /// <see cref="StopAsync"/> is called.
    /// </summary>
    void Enqueue(Func<CancellationToken, Task> action);

    /// <summary>Starts the consumer loop. Must be called once at application startup.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Signals the queue to stop accepting new items, drains remaining items,
    /// and waits for the active action to complete. Safe to call multiple times.
    /// </summary>
    Task StopAsync();
}
