using System.Text.Json;
using System.Text.Json.Nodes;
using EDAI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.Pipeline;

public sealed class SessionService : ISessionService
{
    private static readonly JsonSerializerOptions PrettyOptions = new() { WriteIndented = true };

    private readonly object _writeLock = new();
    private readonly ILogger<SessionService> _logger;

    public string FilePath { get; }

    public SessionService(ILogger<SessionService> logger)
    {
        _logger  = logger;
        FilePath = Path.Combine(AppContext.BaseDirectory, "session.json");
    }

    public string? ReadJson()
    {
        if (!File.Exists(FilePath)) return null;
        try
        {
            using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read session.json");
            return null;
        }
    }

    public void Set(string key, JsonNode? value)
    {
        lock (_writeLock)
        {
            var obj = ReadAndParseObject() ?? new JsonObject();
            if (value is null)
                obj.Remove(key);
            else
                obj[key] = value.DeepClone();
            WriteObject(obj);
        }
    }

    public void Delete(string key)
    {
        lock (_writeLock)
        {
            var obj = ReadAndParseObject();
            if (obj == null) return;
            obj.Remove(key);
            WriteObject(obj);
        }
    }

    public void Clear()
    {
        lock (_writeLock)
            WriteObject(new JsonObject());
    }

    private JsonObject? ReadAndParseObject()
    {
        var json = ReadJson();
        if (json == null) return null;
        try { return JsonNode.Parse(json) as JsonObject; }
        catch { return null; }
    }

    private void WriteObject(JsonObject obj)
    {
        try { File.WriteAllText(FilePath, obj.ToJsonString(PrettyOptions)); }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not write session.json"); }
    }
}
