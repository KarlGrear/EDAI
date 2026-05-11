using EDAI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.Journal;

public sealed class JournalAuxFileReader : IJournalAuxFileReader
{
    private static readonly string JournalDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Saved Games", "Frontier Developments", "Elite Dangerous");

    private static readonly Dictionary<string, string> FileMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["market"]      = "Market.json",
            ["modulesinfo"] = "ModulesInfo.json",
            ["navroute"]    = "NavRoute.json",
            ["outfitting"]  = "Outfitting.json",
            ["shiplocker"]  = "ShipLocker.json",
            ["shipyard"]    = "Shipyard.json",
            ["status"]      = "Status.json",
        };

    private readonly ILogger<JournalAuxFileReader> _logger;

    public IReadOnlyList<string> KnownIdentifiers { get; } =
        FileMap.Keys.OrderBy(k => k).ToList();

    public JournalAuxFileReader(ILogger<JournalAuxFileReader> logger)
    {
        _logger = logger;
    }

    public string? Read(string identifier)
    {
        if (!FileMap.TryGetValue(identifier, out var fileName))
            return null;

        var path = Path.Combine(JournalDirectory, fileName);
        if (!File.Exists(path))
            return null;

        try
        {
            // FileShare.ReadWrite — Elite Dangerous holds the file open while running.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read aux file {File}", fileName);
            return null;
        }
    }
}
