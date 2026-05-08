using EDAI.Core.Models;

namespace EDAI.UI.ViewModels;

public sealed class ResponseDisplayItem
{
    public required string ConfigTitle { get; init; }
    public required TitleDisplayMode TitleDisplayMode { get; init; }
    public required string? Text { get; init; }
    public required DateTime Timestamp { get; init; }
}
