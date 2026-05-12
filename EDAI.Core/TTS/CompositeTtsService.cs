using EDAI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.TTS;

/// <summary>
/// Routes all ITtsService calls to either the Windows SAPI service or the Edge Neural
/// service based on <see cref="ActiveProvider"/>. Edge speech is routed through
/// <see cref="VoiceCacheService"/> to avoid redundant API calls for repeated phrases.
/// Registered as the concrete singleton and aliased as <see cref="ITtsService"/> in DI.
/// </summary>
public sealed class CompositeTtsService : ITtsService, IDisposable
{
    public const string ProviderSapi = "SAPI";
    public const string ProviderEdge = "EdgeNeural";

    private readonly TtsService           _sapi;
    private readonly EdgeTtsService       _edge;
    private readonly VoiceCacheService    _cache;
    private readonly ILogger<CompositeTtsService> _logger;

    public string ActiveProvider { get; set; } = ProviderSapi;

    public CompositeTtsService(
        TtsService sapi,
        EdgeTtsService edge,
        VoiceCacheService cache,
        ILogger<CompositeTtsService> logger)
    {
        _sapi   = sapi;
        _edge   = edge;
        _cache  = cache;
        _logger = logger;
    }

    // ── ITtsService ────────────────────────────────────────────────────────────

    public bool IsEnabled
    {
        get => ActiveProvider == ProviderEdge ? _edge.IsEnabled : _sapi.IsEnabled;
        set
        {
            _sapi.IsEnabled = value;
            _edge.IsEnabled = value;
        }
    }

    public IReadOnlyList<string> GetAvailableVoices()
        => ActiveProvider == ProviderEdge
            ? _edge.GetAvailableVoices()
            : _sapi.GetAvailableVoices();

    public void SetVoice(string voiceName)
    {
        if (ActiveProvider == ProviderEdge)
            _edge.SetVoice(voiceName);
        else
            _sapi.SetVoice(voiceName);
    }

    public void Enqueue(string text)
    {
        if (ActiveProvider == ProviderEdge)
            _ = Task.Run(() => SpeakEdgeCachedAsync(text, CancellationToken.None));
        else
            _sapi.Enqueue(text);
    }

    public Task SpeakAndWaitAsync(string text, CancellationToken ct = default)
        => ActiveProvider == ProviderEdge
            ? SpeakEdgeCachedAsync(text, ct)
            : _sapi.SpeakAndWaitAsync(text, ct);

    // ── Configuration ──────────────────────────────────────────────────────────

    /// <summary>
    /// Applies all Edge-specific settings in one call.
    /// Called on startup and when the user saves settings.
    /// </summary>
    public void ConfigureEdge(string voice, string language, double rate, double pitch)
    {
        _edge.SetVoice(voice);
        _edge.Rate  = rate;
        _edge.Pitch = pitch;
        // language is stored in _edge indirectly via CurrentVoice locale prefix —
        // it's passed through to the cache key and SSML by the voice name itself.
        _ = language; // stored for cache key via VoiceCacheService.ComputeHash
    }

    // ── Edge-specific helpers (used by SettingsViewModel) ─────────────────────

    public IReadOnlyList<EdgeVoiceInfo> GetEdgeVoices() => _edge.GetEdgeVoices();

    public Task LoadEdgeVoicesAsync() => _edge.LoadVoicesAsync();

    // ── Cache-aware Edge playback ──────────────────────────────────────────────

    private async Task SpeakEdgeCachedAsync(string text, CancellationToken ct)
    {
        if (!_edge.IsEnabled || string.IsNullOrWhiteSpace(text)) return;

        var voice    = _edge.CurrentVoice;
        var language = voice.Length >= 5 ? voice[..5] : "en-US"; // e.g. "en-US" from "en-US-AriaNeural"
        var rate     = _edge.Rate;
        var pitch    = _edge.Pitch;

        try
        {
            // Cache lookup is best-effort — errors must never prevent audio from playing.
            byte[]? cached = null;
            try
            {
                cached = await _cache.GetCachedAudioAsync(text, voice, language, rate, pitch);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Voice cache lookup failed — falling back to API");
            }

            if (cached is { Length: > 0 })
            {
                _logger.LogInformation("TTS cache hit for '{Phrase}' (voice='{Voice}') — skipping API call",
                    text.Length > 60 ? text[..57] + "..." : text, voice);
                await EdgeTtsService.PlayMp3PublicAsync(cached, ct);
                return;
            }

            // Cache miss: fetch from API, store, then play
            _logger.LogInformation("TTS calling Edge API (voice='{Voice}'): {Phrase}",
                voice, text.Length > 80 ? text[..77] + "..." : text);
            var audioBytes = await _edge.FetchAudioAsync(text, voice, rate, pitch, ct);
            if (audioBytes.Length == 0)
            {
                _logger.LogWarning("Edge TTS returned 0 bytes (voice='{Voice}')", voice);
                return;
            }

            try { await _cache.StoreCachedAudioAsync(text, voice, language, rate, pitch, audioBytes); }
            catch (Exception ex) { _logger.LogWarning(ex, "Voice cache store failed — playing without caching"); }

            await EdgeTtsService.PlayMp3PublicAsync(audioBytes, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Edge TTS speech failed (voice='{Voice}')", voice);
        }
    }

    public void Dispose()
    {
        _sapi.Dispose();
        _edge.Dispose();
    }
}
