namespace EDAI.Core.Interfaces;

/// <summary>
/// Reads the companion JSON files that Elite Dangerous writes alongside journal logs:
/// Market.json, ModulesInfo.json, NavRoute.json, Outfitting.json,
/// ShipLocker.json, Shipyard.json, Status.json.
/// Each file is read fresh on every call so templates always see the current game state.
/// </summary>
public interface IJournalAuxFileReader
{
    /// <summary>
    /// Returns the raw JSON content of the aux file identified by <paramref name="identifier"/>
    /// (e.g. "status", "market", "navroute"), or <c>null</c> if the file is unknown,
    /// missing, or unreadable.
    /// </summary>
    string? Read(string identifier);

    /// <summary>All known lowercase identifiers (without extension).</summary>
    IReadOnlyList<string> KnownIdentifiers { get; }
}
