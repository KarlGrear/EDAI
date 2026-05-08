namespace EDAI.Core.Models;

public sealed class SessionHistoryModel
{
    public int Id { get; init; }
    public string CommanderName { get; set; } = string.Empty;
    public DateTime SessionStart { get; init; }
    public DateTime? SessionEnd { get; set; }
    public string JournalFileName { get; init; } = string.Empty;
}
