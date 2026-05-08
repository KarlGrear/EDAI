namespace EDAI.UI.ViewModels;

public sealed class EventLogItem
{
    public required string EventType { get; init; }
    public required DateTime Timestamp { get; init; }
    public string TimestampDisplay => Timestamp.ToLocalTime().ToString("HH:mm:ss");
}
