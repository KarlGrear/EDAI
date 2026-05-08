using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

public interface ISessionHistoryRepository
{
    Task<SessionHistoryModel> StartSessionAsync(string commanderName, string journalFileName);
    Task EndSessionAsync(int sessionId);
    Task<SessionHistoryModel?> GetCurrentSessionAsync();
}
