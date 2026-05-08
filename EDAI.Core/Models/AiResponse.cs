namespace EDAI.Core.Models;

public sealed class AiResponse
{
    public required IReadOnlyDictionary<string, string> Fields { get; init; }
    public string? DisplayedOutput { get; set; }
    public string? AnnouncedOutput { get; set; }
}
