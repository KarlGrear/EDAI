using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

/// <summary>
/// Serialises event configurations to a shareable JSON format and deserialises
/// them back. No database access — purely a formatting concern.
/// </summary>
public interface IConfigExportService
{
    /// <summary>
    /// Converts <paramref name="config"/> to a formatted JSON string ready to be
    /// written to a <c>.edai.json</c> file.
    /// </summary>
    string Serialize(EventConfigurationModel config);

    /// <summary>
    /// Parses and validates <paramref name="json"/>.
    /// Returns <c>(true, null, model)</c> on success.
    /// Returns <c>(false, errorMessage, null)</c> if the JSON is invalid, missing required
    /// fields, or does not look like an EDAI configuration export.
    /// </summary>
    (bool Valid, string? Error, ConfigExportPayload? Payload) Deserialize(string json);
}
