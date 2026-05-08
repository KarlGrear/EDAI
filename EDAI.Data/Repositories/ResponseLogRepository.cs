using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using EDAI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDAI.Data.Repositories;

public sealed class ResponseLogRepository : IResponseLogRepository
{
    private readonly IDbContextFactory<EdaiDbContext> _factory;

    public ResponseLogRepository(IDbContextFactory<EdaiDbContext> factory) => _factory = factory;

    public async Task<ResponseLogModel> AddAsync(ResponseLogModel model)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = new ResponseLogEntity
        {
            SessionHistoryId = model.SessionHistoryId,
            EventConfigurationId = model.EventConfigurationId,
            Timestamp = model.Timestamp,
            TriggeringEventJson = model.TriggeringEventJson,
            SecondaryEventsJson = model.SecondaryEventsJson,
            PromptSent = model.PromptSent,
            RawAiResponse = model.RawAiResponse,
            DisplayedOutput = model.DisplayedOutput,
            AnnouncedOutput = model.AnnouncedOutput,
        };
        context.ResponseLogs.Add(entity);
        await context.SaveChangesAsync();
        return new ResponseLogModel
        {
            Id = entity.Id,
            SessionHistoryId = entity.SessionHistoryId,
            EventConfigurationId = entity.EventConfigurationId,
            Timestamp = entity.Timestamp,
            TriggeringEventJson = entity.TriggeringEventJson,
            SecondaryEventsJson = entity.SecondaryEventsJson,
            PromptSent = entity.PromptSent,
            RawAiResponse = entity.RawAiResponse,
            DisplayedOutput = entity.DisplayedOutput,
            AnnouncedOutput = entity.AnnouncedOutput,
        };
    }
}
