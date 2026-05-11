using System.Text.Json;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using EDAI.Core.Pipeline;
using Microsoft.Extensions.Logging;

namespace EDAI.Core.OpenAI;

public sealed class ResponseParser : IResponseParser
{
    private readonly IJournalAuxFileReader _auxReader;
    private readonly ILogger<ResponseParser> _logger;

    public ResponseParser(IJournalAuxFileReader auxReader, ILogger<ResponseParser> logger)
    {
        _auxReader = auxReader;
        _logger    = logger;
    }

    public AiResponse Parse(string? rawJson, EventConfigurationModel config, string? triggerJson = null)
    {
        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(rawJson))
        {
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
        }

        var response = new AiResponse { Fields = fields };

        if (config.DisplayFields.Count > 0)
            response.DisplayedOutput = ApplyTemplates(config.DisplayFields, triggerJson, rawJson);

        if (config.AnnounceFields.Count > 0)
            response.AnnouncedOutput = ApplyTemplates(config.AnnounceFields, triggerJson, rawJson);

        return response;
    }

    private string ApplyTemplates(IList<string> templates, string? triggerJson, string? resultJson)
    {
        var lines = templates
            .Select(t => TemplateEngine.Apply(t, triggerJson, resultJson, _auxReader.Read))
            .Where(l => !string.IsNullOrWhiteSpace(l));
        return string.Join(Environment.NewLine, lines);
    }
}
