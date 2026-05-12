namespace EDAI.Core.Models;

/// <summary>
/// Represents one user-defined AI pipeline configuration. Each record ties a set of
/// triggering journal event types to a prompt, an expected JSON response schema, and
/// display/announce rules for the parsed output.
/// </summary>
public sealed class EventConfigurationModel
{
    public int Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsEnabled { get; set; }
    public IList<string> TriggeringEvents { get; set; } = [];
    public IList<string> SecondaryEvents { get; set; } = [];
    public int SecondaryWaitTimeMs { get; set; } = 1000;
    public string Prompt { get; set; } = string.Empty;
    public string? ExpectedResultsSchema { get; set; }
    public bool DisplayTitle { get; set; }
    public bool AnnounceTitle { get; set; }
    public IList<string> DisplayFields { get; set; } = [];
    public bool DisplayKeys { get; set; }
    public IList<string> AnnounceFields { get; set; } = [];
    public bool AnnounceKeys { get; set; }
    public bool ShowTrayNotification { get; set; }
    public bool SendToAi { get; set; } = true;
    public bool SendFullTriggerEvent { get; set; } = true;
    public string? ModelOverride { get; set; }
    public string? TriggerCondition { get; set; }
    public string? DisplayCondition { get; set; }
    public string? AnnounceCondition { get; set; }
    public long TriggerTimeoutMs { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
}
