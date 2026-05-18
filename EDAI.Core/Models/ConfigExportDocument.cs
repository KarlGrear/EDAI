namespace EDAI.Core.Models;

/// <summary>
/// Root wrapper for a shareable event-configuration export file.
/// The <see cref="EdaiExportVersion"/> field acts as a type marker so that imports can
/// reject unrelated JSON files with a clear error message.
/// </summary>
public sealed class ConfigExportDocument
{
    public string EdaiExportVersion { get; init; } = "1.0";
    public DateTime ExportedAt { get; init; } = DateTime.UtcNow;
    public ConfigExportPayload Configuration { get; init; } = new();
}

/// <summary>
/// All shareable fields from <see cref="EventConfigurationModel"/>.
/// Database-specific fields (Id, CategoryId, CreatedAt, UpdatedAt) are excluded.
/// CategoryName is included so the importing system can find or create a matching category.
/// </summary>
public sealed class ConfigExportPayload
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? CategoryName { get; init; }

    // Trigger
    public bool IsEnabled { get; init; }
    public IList<string> TriggeringEvents { get; init; } = [];
    public string? TriggerCondition { get; init; }
    public long TriggerTimeoutMs { get; init; }

    // Secondary collection
    public IList<string> SecondaryEvents { get; init; } = [];
    public int SecondaryWaitTimeMs { get; init; } = 1000;

    // AI / Script processing
    public bool SendToAi { get; init; } = true;
    public ScriptProcessingType ProcessingType { get; init; } = ScriptProcessingType.None;
    public string? ProcessScript { get; init; }
    public string Prompt { get; init; } = string.Empty;
    public string? ExpectedResultsSchema { get; init; }
    public string? ModelOverride { get; init; }

    // Display
    public bool DisplayTitle { get; init; }
    public IList<string> DisplayFields { get; init; } = [];
    public string? DisplayCondition { get; init; }
    public string? DisplayConditionScript { get; init; }

    // Announce
    public bool AnnounceTitle { get; init; }
    public IList<string> AnnounceFields { get; init; } = [];
    public string? AnnounceCondition { get; init; }
    public string? AnnounceConditionScript { get; init; }

    // Trigger condition scripts
    public string? TriggerConditionScript { get; init; }

    public bool ShowTrayNotification { get; init; }
}
