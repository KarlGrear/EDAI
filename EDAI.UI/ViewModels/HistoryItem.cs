using CommunityToolkit.Mvvm.ComponentModel;

namespace EDAI.UI.ViewModels;

public sealed partial class HistoryItem : ObservableObject
{
    public required string ConfigTitle { get; init; }
    public required string? DisplayedOutput { get; init; }
    public required string PromptSent { get; init; }
    public required string RawAiResponse { get; init; }
    public required DateTime Timestamp { get; init; }

    [ObservableProperty] private bool _isExpanded;

    public string TimestampDisplay => Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

    public string Summary
    {
        get
        {
            var text = DisplayedOutput ?? string.Empty;
            return text.Length > 100 ? text[..97] + "…" : text;
        }
    }
}
