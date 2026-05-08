using System.Text;
using System.Text.Json;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.OpenAI;

public sealed class ResponseParser : IResponseParser
{
    private readonly ILogger<ResponseParser> _logger;

    public ResponseParser(ILogger<ResponseParser> logger)
    {
        _logger = logger;
    }

    public AiResponse Parse(string rawJson, EventConfigurationModel config)
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var value = prop.Value.ValueKind == JsonValueKind.String
                    ? prop.Value.GetString() ?? string.Empty
                    : prop.Value.ToString();
                fields[prop.Name] = value;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response JSON");
        }

        var response = new AiResponse { Fields = fields };

        if (config.DisplayFields.Count > 0)
            response.DisplayedOutput = FormatFields(fields, config.DisplayFields, config.DisplayKeys);

        if (config.AnnounceFields.Count > 0)
            response.AnnouncedOutput = FormatFields(fields, config.AnnounceFields, config.AnnounceKeys);

        return response;
    }

    private static string FormatFields(
        IReadOnlyDictionary<string, string> fields,
        IList<string> keys,
        bool includeKeys)
    {
        var sb = new StringBuilder();
        foreach (var key in keys)
        {
            if (!fields.TryGetValue(key, out var value)) continue;
            if (sb.Length > 0) sb.AppendLine();
            if (includeKeys)
                sb.Append(key).Append(": ").Append(value);
            else
                sb.Append(value);
        }
        return sb.ToString();
    }
}
