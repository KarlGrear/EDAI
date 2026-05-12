using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace EDAI.UI.Validators;

internal static class JsonValidator
{
    /// <summary>
    /// Accepts empty/null or a well-formed JSON object.  Used for schema fields.
    /// </summary>
    public static ValidationResult? ValidateSingleObject(string? value, ValidationContext _)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Success;

        try
        {
            using var doc = JsonDocument.Parse(value);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return new ValidationResult("Must be a JSON object { … }.");
            return ValidationResult.Success;
        }
        catch (JsonException)
        {
            return new ValidationResult("Invalid JSON.");
        }
    }

    /// <summary>
    /// Accepts empty/null or a block of text where every non-empty line is a
    /// well-formed JSON object.  Used for the pipeline test input.
    /// </summary>
    public static ValidationResult? ValidateJsonLines(string? value, ValidationContext _)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Success;

        var lines = value.Split('\n',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i++)
        {
            try
            {
                using var doc = JsonDocument.Parse(lines[i]);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return new ValidationResult($"Line {i + 1}: must be a JSON object {{ … }}.");
            }
            catch (JsonException)
            {
                return new ValidationResult($"Line {i + 1}: invalid JSON.");
            }
        }

        return ValidationResult.Success;
    }
}
