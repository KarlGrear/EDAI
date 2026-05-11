namespace EDAI.UI.ViewModels;

public sealed class ResponseDisplayItem
{
    public required string ConfigTitle { get; init; }
    public required bool DisplayTitle { get; init; }
    public required string? Text { get; init; }
    public required DateTime Timestamp { get; init; }

    public bool ShowTitle => DisplayTitle;
}
