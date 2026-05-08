namespace EDAI.Data.Entities;

public sealed class SessionHistoryEntity
{
    public int Id { get; set; }
    public string CommanderName { get; set; } = string.Empty;
    public DateTime SessionStart { get; set; }
    public DateTime? SessionEnd { get; set; }
    public string JournalFileName { get; set; } = string.Empty;
    public ICollection<ResponseLogEntity> ResponseLogs { get; set; } = [];
}
