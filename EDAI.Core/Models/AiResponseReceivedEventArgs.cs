namespace EDAI.Core.Models;

public sealed class AiResponseReceivedEventArgs : EventArgs
{
    public required string ConfigTitle { get; init; }
    public required bool DisplayTitle { get; init; }
    public required string? DisplayedOutput { get; init; }
    public required string? AnnouncedOutput { get; init; }
    public required DateTime Timestamp { get; init; }
}
