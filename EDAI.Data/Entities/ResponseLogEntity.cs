namespace EDAI.Data.Entities;

public sealed class ResponseLogEntity
{
    public int Id { get; set; }
    public int? SessionHistoryId { get; set; }
    public SessionHistoryEntity? SessionHistory { get; set; }
    public int EventConfigurationId { get; set; }
    public EventConfigurationEntity EventConfiguration { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string TriggeringEventJson { get; set; } = string.Empty;
    public string? SecondaryEventsJson { get; set; }
    public string PromptSent { get; set; } = string.Empty;
    public string RawAiResponse { get; set; } = string.Empty;
    public string? DisplayedOutput { get; set; }
    public string? AnnouncedOutput { get; set; }
}
