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
        OpenAiApiKey             = DpapiHelper.Decrypt(e.OpenAiApiKeyEncrypted),
        OpenAiModel              = e.OpenAiModel,
        TtsVoiceName             = e.TtsVoiceName,
        TtsEnabled               = e.TtsEnabled,
        AlwaysOnTop              = e.AlwaysOnTop,
        ShowSplashScreen         = e.ShowSplashScreen,
        TrayNotificationsEnabled = e.TrayNotificationsEnabled,
        Theme                    = e.Theme,
        PrimaryColor             = e.PrimaryColor,
        CustomBackgroundColor    = e.CustomBackgroundColor,
        CustomForegroundColor    = e.CustomForegroundColor,
        ToolbarBackground        = e.ToolbarBackground,
        ToolbarForeground        = e.ToolbarForeground,
        ButtonForeground         = e.ButtonForeground,
        ControlBackground        = e.ControlBackground,
        ControlHoverBackground   = e.ControlHoverBackground,
        ControlBorderColor       = e.ControlBorderColor,
        TtsProvider              = e.TtsProvider,
        EdgeTtsLanguage          = e.EdgeTtsLanguage,
        EdgeTtsVoice             = e.EdgeTtsVoice,
        EdgeTtsRate              = e.EdgeTtsRate,
        EdgeTtsPitch             = e.EdgeTtsPitch,
        FontFamily               = e.FontFamily,
        FontSize                 = e.FontSize,
        WindowWidth              = e.WindowWidth,
        WindowHeight             = e.WindowHeight,
        WindowLeft               = e.WindowLeft,
        WindowTop                = e.WindowTop,
        IsMaximized              = e.IsMaximized,
        JournalPath              = e.JournalPath ?? SettingsModel.DefaultJournalPath,
    };

    private static void ApplyToEntity(SettingsModel m, SettingEntity e)
    {
        e.OpenAiApiKeyEncrypted  = DpapiHelper.Encrypt(m.OpenAiApiKey);
        e.OpenAiModel            = m.OpenAiModel;
        e.TtsVoiceName           = m.TtsVoiceName;
        e.TtsEnabled             = m.TtsEnabled;
        e.AlwaysOnTop            = m.AlwaysOnTop;
        e.ShowSplashScreen       = m.ShowSplashScreen;
        e.TrayNotificationsEnabled = m.TrayNotificationsEnabled;
        e.Theme                  = m.Theme;
        e.PrimaryColor           = m.PrimaryColor;
        e.CustomBackgroundColor  = m.CustomBackgroundColor;
        e.CustomForegroundColor  = m.CustomForegroundColor;
        e.ToolbarBackground      = m.ToolbarBackground;
        e.ToolbarForeground      = m.ToolbarForeground;
        e.ButtonForeground       = m.ButtonForeground;
        e.ControlBackground      = m.ControlBackground;
        e.ControlHoverBackground = m.ControlHoverBackground;
        e.ControlBorderColor     = m.ControlBorderColor;
        e.TtsProvider            = m.TtsProvider;
        e.EdgeTtsLanguage        = m.EdgeTtsLanguage;
        e.EdgeTtsVoice           = m.EdgeTtsVoice;
        e.EdgeTtsRate            = m.EdgeTtsRate;
        e.EdgeTtsPitch           = m.EdgeTtsPitch;
        e.FontFamily             = m.FontFamily;
        e.FontSize               = m.FontSize;
        e.WindowWidth            = m.WindowWidth;
        e.WindowHeight           = m.WindowHeight;
        e.WindowLeft             = m.WindowLeft;
        e.WindowTop              = m.WindowTop;
        e.IsMaximized            = m.IsMaximized;
        e.JournalPath            = m.JournalPath;
    }
}
