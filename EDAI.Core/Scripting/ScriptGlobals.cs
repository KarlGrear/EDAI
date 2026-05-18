using System.Text.Json.Nodes;
using EDAI.Core.Interfaces;

namespace EDAI.Core.Scripting;

/// <summary>
/// Host object injected into every script as top-level variables.
/// Condition scripts receive all globals except Result.
/// Process scripts receive all globals including Result, which they populate.
/// Aux-file globals are null when the game has not yet written the file.
/// Session is always read fresh from disk at the moment of access.
/// </summary>
public sealed class ScriptGlobals
{
    private readonly ISessionService _sessionService;

    public ScriptGlobals(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public JsonNode? Trigger     { get; init; }
    public JsonArray? Secondary  { get; init; }
    public JsonNode? NavRoute    { get; init; }
    public JsonNode? Status      { get; init; }
    public JsonNode? Market      { get; init; }
    public JsonNode? Outfitting  { get; init; }
    public JsonNode? Shipyard    { get; init; }
    public JsonNode? ShipLocker  { get; init; }
    public JsonNode? ModulesInfo { get; init; }

    /// <summary>
    /// Always reads the current session.json from disk, so changes made by a
    /// concurrently running pipeline are visible immediately on each access.
    /// Returns null when session.json does not exist or is empty.
    /// </summary>
    public JsonNode? Session => ParseOrNull(_sessionService.ReadJson());

    /// <summary>Sets a session key. Pass null to remove the key.</summary>
    public void SetSession(string key, JsonNode? value) => _sessionService.Set(key, value);

    /// <summary>Removes a session key if present.</summary>
    public void DeleteSession(string key) => _sessionService.Delete(key);

    /// <summary>Clears all session values.</summary>
    public void ClearSession() => _sessionService.Clear();

    /// <summary>Populated by process scripts. Ignored by condition scripts.</summary>
    public ScriptResult Result { get; } = new();

    private static JsonNode? ParseOrNull(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonNode.Parse(json); } catch { return null; }
    }
}
