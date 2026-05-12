using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.TTS;

/// <summary>
/// Caches Edge TTS audio files on disk to avoid redundant API calls.
/// Cache key: SHA-256 of "normalizedPhrase|voiceName|language|rate|pitch".
/// Files are stored under cacheDirectory sharded by the first 2 chars of the hash.
/// </summary>
public sealed class VoiceCacheService
{
    private readonly IVoiceCacheRepository _repo;
    private readonly string _cacheDirectory;
    private readonly ILogger<VoiceCacheService> _logger;

    // Azure Cognitive Services SDK swap-in point:
    // Replace the EdgeTtsService.FetchAudioAsync call in SpeakWithCacheAsync
    // with a call to SpeechSynthesizer from Azure.AI.Speech to swap providers.
    public VoiceCacheService(
        IVoiceCacheRepository repo,
        string cacheDirectory,
        ILogger<VoiceCacheService> logger)
    {
        _repo           = repo;
        _cacheDirectory = cacheDirectory;
        _logger         = logger;
    }

    public async Task<byte[]?> GetCachedAudioAsync(
        string phrase, string voiceName, string language, double rate, double pitch)
    {
        var hash = ComputeHash(phrase, voiceName, language, rate, pitch);
        var entry = await _repo.GetByHashAsync(hash);
        if (entry is null) return null;

        if (!File.Exists(entry.FilePath))
        {
            // File deleted externally — evict the stale index row
            await _repo.DeleteByHashAsync(hash);
            return null;
        }

        await _repo.UpdateUsageAsync(hash);
        return await File.ReadAllBytesAsync(entry.FilePath);
    }

    public async Task StoreCachedAudioAsync(
        string phrase, string voiceName, string language, double rate, double pitch,
        byte[] audioBytes)
    {
        var hash     = ComputeHash(phrase, voiceName, language, rate, pitch);
        var filePath = BuildFilePath(hash);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, audioBytes);

        var now = DateTime.UtcNow;
        await _repo.InsertAsync(new VoiceCacheModel
        {
            Hash      = hash,
            Phrase    = NormalizePhrase(phrase),
            VoiceName = voiceName,
            Language  = language,
            Rate      = rate,
            Pitch     = pitch,
            FilePath  = filePath,
            CreatedAt = now,
            LastUsed  = now,
            UseCount  = 1,
        });

        _logger.LogDebug("Voice cache stored: hash={Hash}, voice={Voice}", hash[..8], voiceName);
    }

    public async Task<(int filesDeleted, int dbRowsDeleted)> ClearCacheAsync()
    {
        // Step 1: Delete all .mp3 files from the cache directory (independent of DB state).
        int filesDeleted = 0;
        if (Directory.Exists(_cacheDirectory))
        {
            foreach (var file in Directory.GetFiles(_cacheDirectory, "*.mp3", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                    filesDeleted++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Voice cache: could not delete file '{Path}'", file);
                }
            }
        }

        // Step 2: Remove all database rows (independent of file state).
        int dbRowsDeleted = await _repo.ClearAllAsync();

        _logger.LogInformation(
            "Voice cache cleared. Removed {Files} file{FS} and {Rows} database {RS}.",
            filesDeleted, filesDeleted == 1 ? "" : "s",
            dbRowsDeleted, dbRowsDeleted == 1 ? "entry" : "entries");

        return (filesDeleted, dbRowsDeleted);
    }

    public static string ComputeHash(
        string phrase, string voiceName, string language, double rate, double pitch)
    {
        var normalized = NormalizePhrase(phrase);
        var key = $"{normalized}|{voiceName}|{language}|{rate:F2}|{pitch:F2}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string NormalizePhrase(string phrase)
    {
        // Collapse whitespace, strip leading/trailing, lowercase
        var collapsed = Regex.Replace(phrase.Trim(), @"\s+", " ");
        return collapsed.ToLowerInvariant();
    }

    private string BuildFilePath(string hash)
    {
        var shard = hash[..2];
        return Path.Combine(_cacheDirectory, shard, hash + ".mp3");
    }
}
