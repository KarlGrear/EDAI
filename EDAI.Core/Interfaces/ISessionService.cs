using System.Text.Json.Nodes;

namespace EDAI.Core.Interfaces;

/// <summary>
/// Manages the session.json file — a mutable, persistent JSON object shared across all pipelines.
/// Reads are always fresh from disk; writes are thread-safe atomic read-modify-write operations.
/// Accessible in templates as |session.key| and in scripts as Session["key"].
/// </summary>
public interface ISessionService
{
    /// <summary>Raw JSON of the current session object, or null if session.json does not exist.</summary>
    string? ReadJson();

    /// <summary>Sets <paramref name="key"/> to <paramref name="value"/>. Pass null to remove the key.</summary>
    void Set(string key, JsonNode? value);

    /// <summary>Removes <paramref name="key"/> from the session if present.</summary>
    void Delete(string key);

    /// <summary>Clears all session values, leaving an empty object.</summary>
    void Clear();

    string FilePath { get; }
}
