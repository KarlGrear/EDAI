namespace EDAI.Core.Models;

public sealed class ResponseLogModel
{
    public int Id { get; init; }
    public int? SessionHistoryId { get; init; }
    public int EventConfigurationId { get; init; }
    public string? ConfigTitle { get; init; }
    public DateTime Timestamp { get; init; }
    public string TriggeringEventJson { get; init; } = string.Empty;
    public string? SecondaryEventsJson { get; init; }
    public string PromptSent { get; init; } = string.Empty;
    public string RawAiResponse { get; init; } = string.Empty;
    public string? DisplayedOutput { get; init; }
    public string? AnnouncedOutput { get; init; }
}
