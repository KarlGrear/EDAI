using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using EDAI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDAI.Data.Repositories;

public sealed class SessionHistoryRepository : ISessionHistoryRepository
{
    private readonly IDbContextFactory<EdaiDbContext> _factory;

    public SessionHistoryRepository(IDbContextFactory<EdaiDbContext> factory) => _factory = factory;

    public async Task<SessionHistoryModel> StartSessionAsync(string commanderName, string journalFileName)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = new SessionHistoryEntity
        {
            CommanderName = commanderName,
            JournalFileName = journalFileName,
            SessionStart = DateTime.UtcNow,
        };
        context.SessionHistories.Add(entity);
        await context.SaveChangesAsync();
        return ToModel(entity);
    }

    public async Task EndSessionAsync(int sessionId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.SessionHistories.FindAsync(sessionId);
        if (entity is not null)
        {
            entity.SessionEnd = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<SessionHistoryModel?> GetCurrentSessionAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.SessionHistories
            .Where(s => s.SessionEnd == null)
            .OrderByDescending(s => s.SessionStart)
            .FirstOrDefaultAsync();
        return entity is not null ? ToModel(entity) : null;
    }

    private static SessionHistoryModel ToModel(SessionHistoryEntity e) => new()
    {
        Id = e.Id,
        CommanderName = e.CommanderName,
        SessionStart = e.SessionStart,
        SessionEnd = e.SessionEnd,
        JournalFileName = e.JournalFileName,
    };
}
