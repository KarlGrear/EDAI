using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NAudio.Wave;
using EDAI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.TTS;

public sealed class EdgeVoiceInfo
{
    public string ShortName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Locale { get; init; } = string.Empty;
    public string Gender { get; init; } = string.Empty;
}

public sealed class EdgeTtsService : ITtsService, IDisposable
{
    private const string TrustedClientToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";

    private const string WssBase =
        "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1" +
        "?TrustedClientToken=" + TrustedClientToken;

    private const string VoicesUrl =
        "https://speech.platform.bing.com/consumer/speech/synthesize/readaloud/voices/list" +
        "?trustedclienttoken=" + TrustedClientToken;

    // Kept in sync with the edge-tts Python library (rany2/edge-tts).
    // Sec-MS-GEC and Sec-MS-GEC-Version go in the URL query string, not HTTP headers.
    private const string SecMsGecVersion = "1-143.0.3650.75";
    private const string UserAgentString  =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36 Edg/143.0.0.0";

    private static readonly string[] AllowedLocales = ["en-US", "en-GB"];

    private readonly ILogger<EdgeTtsService> _logger;
    private readonly SemaphoreSlim _speechLock = new(1, 1);
    private List<EdgeVoiceInfo> _voiceCache = [];
    private bool _disposed;

    public bool IsEnabled { get; set; } = true;
    public string CurrentVoice { get; set; } = "en-US-AriaNeural";
    public double Rate { get; set; } = 1.0;
    public double Pitch { get; set; } = 1.0;

    public EdgeTtsService(ILogger<EdgeTtsService> logger)
    {
        _logger = logger;
    }

    // ── ITtsService ────────────────────────────────────────────────────────────

    public IReadOnlyList<string> GetAvailableVoices()
        => _voiceCache.Select(v => v.ShortName).ToList();

    public void SetVoice(string voiceName) => CurrentVoice = voiceName;

    public void Enqueue(string text)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(text)) return;
        _ = Task.Run(() => SpeakCoreAsync(text, CancellationToken.None));
    }

    public Task SpeakAndWaitAsync(string text, CancellationToken ct = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(text)) return Task.CompletedTask;
        return SpeakCoreAsync(text, ct);
    }

    // ── Edge-specific ──────────────────────────────────────────────────────────

    public IReadOnlyList<EdgeVoiceInfo> GetEdgeVoices() => _voiceCache;

    public async Task LoadVoicesAsync()
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", UserAgentString);

            var json = await http.GetStringAsync(VoicesUrl);
            using var doc = JsonDocument.Parse(json);

            var voices = new List<EdgeVoiceInfo>();
            foreach (var v in doc.RootElement.EnumerateArray())
            {
                var locale = v.GetProperty("Locale").GetString() ?? string.Empty;
                if (!AllowedLocales.Contains(locale)) continue;

                var shortName = v.GetProperty("ShortName").GetString() ?? string.Empty;
                var gender    = v.GetProperty("Gender").GetString()    ?? string.Empty;

                // "en-US-AriaNeural" → strip locale prefix and "Neural" suffix → "Aria"
                var baseName = shortName
                    .Replace($"{locale}-", string.Empty)
                    .Replace("Neural", string.Empty)
                    .Trim();

                voices.Add(new EdgeVoiceInfo
                {
                    ShortName   = shortName,
                    DisplayName = $"{baseName} ({gender})",
                    Locale      = locale,
                    Gender      = gender,
                });
            }

            _voiceCache = voices;
            _logger.LogInformation("Loaded {Count} Edge TTS voices (en-US / en-GB)", voices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Edge TTS voice list — voices will be unavailable until next restart");
        }
    }

    // ── Core speech logic ──────────────────────────────────────────────────────

    private async Task SpeakCoreAsync(string text, CancellationToken ct)
    {
        await _speechLock.WaitAsync(ct);
        try
        {
            var audioBytes = await FetchAudioAsync(text, CurrentVoice, Rate, Pitch, ct);
            if (audioBytes.Length > 0)
                await PlayMp3Async(audioBytes, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Edge TTS speech failed (voice='{Voice}')", CurrentVoice);
        }
        finally
        {
            _speechLock.Release();
        }
    }

    internal async Task<byte[]> FetchAudioAsync(string text, string voice, double rate, double pitch, CancellationToken ct)
    {
        var (secMsGec, connectionId) = GenerateSecMsGec();

        // Sec-MS-GEC and Sec-MS-GEC-Version go in the URL query string, not HTTP headers.
        var wssUrl = $"{WssBase}&ConnectionId={connectionId}" +
                     $"&Sec-MS-GEC={secMsGec}&Sec-MS-GEC-Version={SecMsGecVersion}";

        // muid is a per-session random cookie required since ~mid-2024.
        var muid = Guid.NewGuid().ToString("N").ToUpperInvariant();

        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("User-Agent", UserAgentString);
        ws.Options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");
        ws.Options.SetRequestHeader("Cookie", $"muid={muid};");
        ws.Options.SetRequestHeader("Pragma", "no-cache");
        ws.Options.SetRequestHeader("Cache-Control", "no-cache");
        ws.Options.SetRequestHeader("Accept-Language", "en-US,en;q=0.9");
        ws.Options.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");

        // Use SocketsHttpHandler (managed stack) instead of the default WinHTTP backend.
        // WinHTTP silently strips custom Cookie and other headers before the upgrade handshake.
        using var invoker = new System.Net.Http.HttpMessageInvoker(new System.Net.Http.SocketsHttpHandler());
        await ws.ConnectAsync(new Uri(wssUrl), invoker, ct);

        var timestamp = DateTime.UtcNow.ToString(
            "ddd MMM dd yyyy HH:mm:ss 'GMT+0000 (Coordinated Universal Time)'");

        // 1. Send audio format config
        var config =
            $"X-Timestamp:{timestamp}\r\n" +
            "Content-Type:application/json; charset=utf-8\r\n" +
            "Path:speech.config\r\n\r\n" +
            """{"context":{"synthesis":{"audio":{"metadataoptions":{"sentenceBoundaryEnabled":"false","wordBoundaryEnabled":"false"},"outputFormat":"audio-24khz-48kbitrate-mono-mp3"}}}}""";

        await ws.SendAsync(
            Encoding.UTF8.GetBytes(config), WebSocketMessageType.Text, true, ct);

        // 2. Send SSML
        var safeText = System.Security.SecurityElement.Escape(text) ?? text;
        // Edge TTS SSML prosody: rate uses "+N%" (relative %), pitch uses "+NHz" (Hz offset).
        // When both are default (1.0), omit the prosody element to keep the request identical
        // to the pre-prosody baseline that was confirmed working.
        string speechContent;
        if (Math.Abs(rate - 1.0) < 0.001 && Math.Abs(pitch - 1.0) < 0.001)
        {
            speechContent = safeText;
        }
        else
        {
            var ratePct   = FormatProsodyRate(rate);
            var pitchHz   = FormatProsodyPitchHz(pitch);
            speechContent = $"<prosody rate='{ratePct}' pitch='{pitchHz}'>{safeText}</prosody>";
        }

        var ssml =
            $"X-Timestamp:{timestamp}\r\n" +
            $"X-RequestId:{Guid.NewGuid():N}\r\n" +
            "Content-Type:application/ssml+xml\r\n" +
            "Path:ssml\r\n\r\n" +
            $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
            $"<voice name='{voice}'>{speechContent}</voice></speak>";

        await ws.SendAsync(
            Encoding.UTF8.GetBytes(ssml), WebSocketMessageType.Text, true, ct);

        // 3. Collect audio frames until turn.end
        var audioBuffer  = new List<byte>(64 * 1024);
        var receiveBuffer = new byte[16 * 1024];

        while (ws.State == WebSocketState.Open)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(receiveBuffer, ct);
                ms.Write(receiveBuffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close) break;

            var frame = ms.ToArray();

            if (result.MessageType == WebSocketMessageType.Binary)
            {
                // Binary frame layout: [2-byte big-endian header length][header][audio bytes]
                if (frame.Length > 2)
                {
                    var headerLen  = (frame[0] << 8) | frame[1];
                    var audioStart = 2 + headerLen;
                    if (audioStart < frame.Length)
                        audioBuffer.AddRange(frame.AsSpan(audioStart).ToArray());
                }
            }
            else
            {
                // Text frame — "Path:turn.end" signals completion
                if (Encoding.UTF8.GetString(frame).Contains("Path:turn.end"))
                    break;
            }
        }

        if (ws.State == WebSocketState.Open)
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);

        return audioBuffer.ToArray();
    }

    // Generates the Sec-MS-GEC security token required by the Edge TTS API since ~2024.
    // Algorithm: SHA-256("{filetimeTicks_roundedTo5min}{TrustedClientToken}") → uppercase hex.
    // ToFileTimeUtc() returns 100-ns intervals since 1601-01-01, matching what the Edge browser sends.
    private static (string secMsGec, string connectionId) GenerateSecMsGec()
    {
        var ticks = DateTime.UtcNow.ToFileTimeUtc();
        ticks -= ticks % 3_000_000_000L; // round down to the nearest 5-minute window

        var input    = $"{ticks}{TrustedClientToken}";
        var hash     = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var secMsGec = Convert.ToHexString(hash); // uppercase by default in .NET

        // Lowercase hex without dashes — matches edge-tts connect_id() = uuid.uuid4().hex
        var connectionId = Guid.NewGuid().ToString("N");
        return (secMsGec, connectionId);
    }

    // Rate: Edge TTS uses relative % — "+0%" = normal, "+50%" = 1.5×, "-25%" = 0.75×
    private static string FormatProsodyRate(double value)
    {
        var pct = (int)Math.Round((value - 1.0) * 100);
        return pct >= 0 ? $"+{pct}%" : $"{pct}%";
    }

    // Pitch: Edge TTS uses Hz offset — "+0Hz" = normal, "+50Hz" = higher, "-50Hz" = lower
    // Map slider range 0.5–2.0 → -50Hz to +100Hz
    private static string FormatProsodyPitchHz(double value)
    {
        var hz = (int)Math.Round((value - 1.0) * 100);
        return hz >= 0 ? $"+{hz}Hz" : $"{hz}Hz";
    }

    internal static Task PlayMp3PublicAsync(byte[] mp3Bytes, CancellationToken ct)
        => PlayMp3Async(mp3Bytes, ct);

    private static async Task PlayMp3Async(byte[] mp3Bytes, CancellationToken ct)
    {
        using var ms     = new MemoryStream(mp3Bytes);
        using var reader = new Mp3FileReader(ms);
        using var player = new WaveOutEvent();

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        player.PlaybackStopped += (_, _) => tcs.TrySetResult(true);
        ct.Register(() => { player.Stop(); tcs.TrySetCanceled(ct); },
                    useSynchronizationContext: false);

        player.Init(reader);
        player.Play();
        await tcs.Task;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _speechLock.Dispose();
    }
}
