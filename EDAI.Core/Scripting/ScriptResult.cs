using System.Text.Json.Nodes;

namespace EDAI.Core.Scripting;

/// <summary>
/// Output written by a process script. Serialises to flat JSON consumed by the
/// existing ResponseParser / template engine via |result.X| tokens.
/// </summary>
public sealed class ScriptResult
{
    public string? Announcement { get; set; }
    public string? Display { get; set; }

    private readonly Dictionary<string, string?> _fields = new(StringComparer.OrdinalIgnoreCase);

    public string? this[string key]
    {
        get => _fields.GetValueOrDefault(key);
        set => _fields[key] = value;
    }

    public string ToJson()
    {
        var obj = new JsonObject();
        if (Announcement != null) obj["Announcement"] = Announcement;
        if (Display != null) obj["Display"] = Display;
        foreach (var (key, val) in _fields)
            if (val != null) obj[key] = val;
        return obj.ToJsonString();
    }
}
