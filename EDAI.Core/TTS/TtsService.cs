using System.Collections.Concurrent;
using System.Speech.Synthesis;
using EDAI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.TTS;

public sealed class TtsService : ITtsService, IDisposable
{
    private readonly ILogger<TtsService> _logger;
    private readonly SpeechSynthesizer _synth = new();
    private readonly BlockingCollection<string> _queue = new(new ConcurrentQueue<string>());
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

    public void Enqueue(string text)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(text)) return;
        _queue.TryAdd(text);
    }

    private void Consume()
    {
        foreach (var text in _queue.GetConsumingEnumerable())
        {
            try { _synth.Speak(text); }
            catch (Exception ex) { _logger.LogWarning(ex, "TTS speak failed"); }
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
