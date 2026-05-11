using System.Collections.Concurrent;
using System.Speech.Synthesis;
using EDAI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.TTS;

public sealed class TtsService : ITtsService, IDisposable
{
    private readonly ILogger<TtsService> _logger;
    private readonly SpeechSynthesizer _synth = new();

    // Each item carries the text and an optional TCS. When TCS is non-null the worker
    // signals it after Speak() returns, allowing SpeakAndWaitAsync callers to await
    // completion of that specific utterance.
    private readonly BlockingCollection<(string Text, TaskCompletionSource<bool>? Tcs)> _queue =
        new(new ConcurrentQueue<(string, TaskCompletionSource<bool>?)>());

    private readonly Thread _worker;
    private bool _disposed;

    public bool IsEnabled { get; set; } = true;

    public TtsService(ILogger<TtsService> logger)
    {
        _logger = logger;
        _worker = new Thread(Consume) { IsBackground = true, Name = "TtsWorker" };
        _worker.Start();
    }

    public IReadOnlyList<string> GetAvailableVoices()
        => _synth.GetInstalledVoices()
                 .Where(v => v.Enabled)
                 .Select(v => v.VoiceInfo.Name)
                 .ToList();

    public void SetVoice(string voiceName)
    {
        try { _synth.SelectVoice(voiceName); }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not select TTS voice '{Voice}'", voiceName); }
    }

    /// <inheritdoc/>
    public void Enqueue(string text)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(text)) return;
        _queue.TryAdd((text, null));
    }

    /// <inheritdoc/>
    public Task SpeakAndWaitAsync(string text, CancellationToken ct = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(text)) return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // If the caller cancels, unblock them immediately. The worker will still
        // speak the item (speech cannot be aborted mid-queue cleanly) but the
        // awaiting task transitions to cancelled so the action queue can move on.
        ct.Register(() => tcs.TrySetCanceled(ct), useSynchronizationContext: false);

        if (!_queue.TryAdd((text, tcs)))
            tcs.TrySetCanceled(); // queue completed (app shutting down)

        return tcs.Task;
    }

    private void Consume()
    {
        foreach (var (text, tcs) in _queue.GetConsumingEnumerable())
        {
            try
            {
                _synth.Speak(text);      // blocking — returns when utterance is done
                tcs?.TrySetResult(true); // signal awaiting caller
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TTS speak failed");
                tcs?.TrySetException(ex);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _queue.CompleteAdding();
        _synth.Dispose();
        _queue.Dispose();
    }
}
