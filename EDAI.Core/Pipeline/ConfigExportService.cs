using System.Text.Json;
using System.Text.Json.Serialization;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;

namespace EDAI.Core.Pipeline;

/// <summary>
/// Converts event configurations to/from a portable JSON format.
/// No database dependencies — purely a serialisation concern.
/// </summary>
public sealed class ConfigExportService : IConfigExportService
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <inheritdoc/>
    public string Serialize(EventConfigurationModel config)
    {
        var doc = new ConfigExportDocument
        {
            ExportedAt    = DateTime.UtcNow,
            Configuration = new ConfigExportPayload
            {
                Title                = config.Title,
                Description          = config.Description,
                CategoryName         = config.CategoryName,
                IsEnabled            = config.IsEnabled,
                TriggeringEvents     = config.TriggeringEvents,
                TriggerCondition     = config.TriggerCondition,
                TriggerTimeoutMs     = config.TriggerTimeoutMs,
                SecondaryEvents      = config.SecondaryEvents,
                SecondaryWaitTimeMs  = config.SecondaryWaitTimeMs,
                SendToAi             = config.SendToAi,
                Prompt               = config.Prompt,
                ExpectedResultsSchema = config.ExpectedResultsSchema,
                ModelOverride        = config.ModelOverride,
                DisplayTitle         = config.DisplayTitle,
                DisplayFields        = config.DisplayFields,
                DisplayCondition     = config.DisplayCondition,
                AnnounceTitle        = config.AnnounceTitle,
                AnnounceFields       = config.AnnounceFields,
                AnnounceCondition    = config.AnnounceCondition,
                ShowTrayNotification = config.ShowTrayNotification,
            },
        };

        return JsonSerializer.Serialize(doc, WriteOptions);
    }

    /// <inheritdoc/>
    public (bool Valid, string? Error, ConfigExportPayload? Payload) Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (false, "File is empty.", null);

        ConfigExportDocument? doc;
        try
        {
            doc = JsonSerializer.Deserialize<ConfigExportDocument>(json, ReadOptions);
        }
        catch (JsonException ex)
        {
            return (false, $"Not valid JSON: {ex.Message}", null);
        }

        if (doc == null)
            return (false, "File could not be read.", null);

        if (string.IsNullOrWhiteSpace(doc.EdaiExportVersion))
            return (false, "This does not appear to be an EDAI configuration export (missing version marker).", null);

        if (doc.Configuration == null)
            return (false, "Export file has no configuration payload.", null);

        if (string.IsNullOrWhiteSpace(doc.Configuration.Title))
            return (false, "Configuration is missing a required Title.", null);

        return (true, null, doc.Configuration);
    }
}
