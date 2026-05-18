using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using EDAI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDAI.Data.Repositories;

public sealed class EventConfigurationRepository : IEventConfigurationRepository
{
    private readonly IDbContextFactory<EdaiDbContext> _factory;

    public EventConfigurationRepository(IDbContextFactory<EdaiDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<EventConfigurationModel>> GetAllAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.EventConfigurations
            .Include(e => e.Category)
            .OrderBy(e => e.Title)
            .ToListAsync();
        return entities.Select(ToModel).ToList();
    }

    public async Task<IReadOnlyList<EventConfigurationModel>> GetEnabledAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entities = await context.EventConfigurations
            .Where(e => e.IsEnabled)
            .Include(e => e.Category)
            .ToListAsync();
        return entities.Select(ToModel).ToList();
    }

    public async Task<EventConfigurationModel?> GetByIdAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.EventConfigurations
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id);
        return entity is not null ? ToModel(entity) : null;
    }

    public async Task<EventConfigurationModel> AddAsync(EventConfigurationModel model)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = ToEntity(model);
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        context.EventConfigurations.Add(entity);
        await context.SaveChangesAsync();
        return await GetByIdAsync(entity.Id) ?? ToModel(entity);
    }

    public async Task UpdateAsync(EventConfigurationModel model)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.EventConfigurations.FindAsync(model.Id)
            ?? throw new InvalidOperationException($"EventConfiguration {model.Id} not found.");
        ApplyToEntity(model, entity);
        entity.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.EventConfigurations.FindAsync(id);
        if (entity is not null)
        {
            var logs = await context.ResponseLogs
                .Where(r => r.EventConfigurationId == id)
                .ToListAsync();
            context.ResponseLogs.RemoveRange(logs);
            context.EventConfigurations.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task SetEnabledAsync(int id, bool isEnabled)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var entity = await context.EventConfigurations.FindAsync(id)
            ?? throw new InvalidOperationException($"EventConfiguration {id} not found.");
        entity.IsEnabled = isEnabled;
        entity.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private static EventConfigurationModel ToModel(EventConfigurationEntity e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Description = e.Description,
        CategoryId = e.CategoryId,
        CategoryName = e.Category?.Name,
        IsEnabled = e.IsEnabled,
        TriggeringEvents = e.TriggeringEvents,
        SecondaryEvents = e.SecondaryEvents,
        SecondaryWaitTimeMs = e.SecondaryWaitTimeMs,
        Prompt = e.Prompt,
        ExpectedResultsSchema = e.ExpectedResultsSchema,
        DisplayTitle = e.DisplayTitle,
        AnnounceTitle = e.AnnounceTitle,
        DisplayFields = e.DisplayFields,
        AnnounceFields = e.AnnounceFields,
        ShowTrayNotification = e.ShowTrayNotification,
        SendToAi = e.SendToAi,
        ProcessingType = Enum.TryParse<ScriptProcessingType>(e.ProcessingType, out var pt) ? pt : ScriptProcessingType.None,
        ProcessScript = e.ProcessScript,
        ModelOverride = e.ModelOverride,
        TriggerCondition = e.TriggerCondition,
        TriggerConditionScript = e.TriggerConditionScript,
        DisplayCondition = e.DisplayCondition,
        DisplayConditionScript = e.DisplayConditionScript,
        AnnounceCondition = e.AnnounceCondition,
        AnnounceConditionScript = e.AnnounceConditionScript,
        TriggerTimeoutMs = e.TriggerTimeoutMs,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };

    private static EventConfigurationEntity ToEntity(EventConfigurationModel m)
    {
        var entity = new EventConfigurationEntity();
        ApplyToEntity(m, entity);
        return entity;
    }

    private static void ApplyToEntity(EventConfigurationModel m, EventConfigurationEntity e)
    {
        e.Title = m.Title;
        e.Description = m.Description;
        e.CategoryId = m.CategoryId;
        e.IsEnabled = m.IsEnabled;
        e.TriggeringEvents = [.. m.TriggeringEvents];
        e.SecondaryEvents = [.. m.SecondaryEvents];
        e.SecondaryWaitTimeMs = m.SecondaryWaitTimeMs;
        e.Prompt = m.Prompt;
        e.ExpectedResultsSchema = m.ExpectedResultsSchema;
        e.DisplayTitle = m.DisplayTitle;
        e.AnnounceTitle = m.AnnounceTitle;
        e.DisplayFields = [.. m.DisplayFields];
        e.AnnounceFields = [.. m.AnnounceFields];
        e.ShowTrayNotification = m.ShowTrayNotification;
        e.SendToAi = m.SendToAi;
        e.ProcessingType = m.ProcessingType.ToString();
        e.ProcessScript = m.ProcessScript;
        e.ModelOverride = m.ModelOverride;
        e.TriggerCondition = m.TriggerCondition;
        e.TriggerConditionScript = m.TriggerConditionScript;
        e.DisplayCondition = m.DisplayCondition;
        e.DisplayConditionScript = m.DisplayConditionScript;
        e.AnnounceCondition = m.AnnounceCondition;
        e.AnnounceConditionScript = m.AnnounceConditionScript;
        e.TriggerTimeoutMs = m.TriggerTimeoutMs;
    }
}
