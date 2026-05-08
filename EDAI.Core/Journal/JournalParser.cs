using System.Text.Json;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.Journal;

/// <summary>
/// Parses a raw Elite Dangerous journal line (one JSON object per line) into a
/// <see cref="ParsedJournalEvent"/>. Lines that are not valid JSON objects or that
/// lack an <c>event</c> property are silently discarded (logged at Debug level).
/// </summary>
public sealed class JournalParser : IJournalParser
{
    private readonly ILogger<JournalParser> _logger;

    public JournalParser(ILogger<JournalParser> logger) => _logger = logger;

    public ParsedJournalEvent? TryParse(string rawLine)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawLine);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            if (!root.TryGetProperty("event", out var eventProp))
                return null;

            var eventType = eventProp.GetString();
            if (string.IsNullOrWhiteSpace(eventType))
                return null;

            return new ParsedJournalEvent(eventType, rawLine);
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse journal line: {Preview}",
                rawLine[..Math.Min(rawLine.Length, 120)]);
            return null;
        }
    }
}
