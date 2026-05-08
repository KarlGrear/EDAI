using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using EDAI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDAI.Data.Repositories;

public sealed class SettingsRepository : ISettingsRepository
{
    private readonly IDbContextFactory<EdaiDbContext> _factory;

    public SettingsRepository(IDbContextFactory<EdaiDbContext> factory) => _factory = factory;

    public async Task<SettingsModel> GetAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        var entity = await context.Settings.FirstOrDefaultAsync();
        if (entity is null)
        {
            // First run — persist a default row so subsequent saves are updates.
            entity = new SettingEntity();
            context.Settings.Add(entity);
            await context.SaveChangesAsync();
        }

        return ToModel(entity);
    }

    public async Task SaveAsync(SettingsModel settings)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var entity = await context.Settings.FirstOrDefaultAsync();
        if (entity is null)
        {
            entity = new SettingEntity();
            context.Settings.Add(entity);
        }

        ApplyToEntity(settings, entity);
        await context.SaveChangesAsync();
    }

    // -------------------------------------------------------------------------
    // Mapping
    // -------------------------------------------------------------------------

    private static SettingsModel ToModel(SettingEntity e) => new()
    {
        OpenAiApiKey           = DpapiHelper.Decrypt(e.OpenAiApiKeyEncrypted),
        OpenAiModel            = e.OpenAiModel,
        TtsVoiceName           = e.TtsVoiceName,
        TtsEnabled             = e.TtsEnabled,
        AlwaysOnTop            = e.AlwaysOnTop,
        TrayNotificationsEnabled = e.TrayNotificationsEnabled,
        Theme                  = e.Theme,
        WindowWidth            = e.WindowWidth,
        WindowHeight           = e.WindowHeight,
        WindowLeft             = e.WindowLeft,
        WindowTop              = e.WindowTop,
    };

    private static void ApplyToEntity(SettingsModel m, SettingEntity e)
    {
        e.OpenAiApiKeyEncrypted  = DpapiHelper.Encrypt(m.OpenAiApiKey);
        e.OpenAiModel            = m.OpenAiModel;
        e.TtsVoiceName           = m.TtsVoiceName;
        e.TtsEnabled             = m.TtsEnabled;
        e.AlwaysOnTop            = m.AlwaysOnTop;
        e.TrayNotificationsEnabled = m.TrayNotificationsEnabled;
        e.Theme                  = m.Theme;
        e.WindowWidth            = m.WindowWidth;
        e.WindowHeight           = m.WindowHeight;
        e.WindowLeft             = m.WindowLeft;
        e.WindowTop              = m.WindowTop;
    }
}
