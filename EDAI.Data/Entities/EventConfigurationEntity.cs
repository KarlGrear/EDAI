namespace EDAI.Data.Entities;

public sealed class EventConfigurationEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public CategoryEntity? Category { get; set; }
    public bool IsEnabled { get; set; }
    public List<string> TriggeringEvents { get; set; } = [];
    public List<string> SecondaryEvents { get; set; } = [];
    public int SecondaryWaitTimeMs { get; set; } = 1000;
    public string Prompt { get; set; } = string.Empty;
    public string? ExpectedResultsSchema { get; set; }
    public bool DisplayTitle { get; set; }
    public bool AnnounceTitle { get; set; }
    public List<string> DisplayFields { get; set; } = [];
    public List<string> AnnounceFields { get; set; } = [];
    public bool ShowTrayNotification { get; set; }
    public bool SendToAi { get; set; } = true;
    public string? ModelOverride { get; set; }
    public string? TriggerCondition { get; set; }
    public string? DisplayCondition { get; set; }
    public string? AnnounceCondition { get; set; }
    public long TriggerTimeoutMs { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
