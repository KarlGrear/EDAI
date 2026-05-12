namespace EDAI.Data.Entities;

public sealed class SettingEntity
{
    public int Id { get; set; }
    public string? OpenAiApiKeyEncrypted { get; set; }
    public string OpenAiModel { get; set; } = "gpt-4o";
    public string? TtsVoiceName { get; set; }
    public bool TtsEnabled { get; set; } = true;
    public bool AlwaysOnTop { get; set; }
    public bool ShowSplashScreen { get; set; } = true;
    public bool TrayNotificationsEnabled { get; set; } = true;
    public string Theme { get; set; } = "Dark";
    public string PrimaryColor { get; set; } = "#FF6D00";
    public string? CustomBackgroundColor { get; set; }
    public string? CustomForegroundColor { get; set; }
    public string? ToolbarBackground { get; set; }
    public string? ToolbarForeground { get; set; }
    public string? ButtonForeground { get; set; }
    public string? ControlBackground { get; set; }
    public string? ControlHoverBackground { get; set; }
    public string? ControlBorderColor { get; set; }
    public string TtsProvider { get; set; } = "SAPI";
    public string? EdgeTtsLanguage { get; set; }
    public string? EdgeTtsVoice { get; set; }
    public double EdgeTtsRate { get; set; } = 1.0;
    public double EdgeTtsPitch { get; set; } = 1.0;
    public string? FontFamily { get; set; }
    public double FontSize { get; set; } = 14.0;
    public double WindowWidth { get; set; } = 900;
    public double WindowHeight { get; set; } = 600;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public bool IsMaximized { get; set; }
    public string? JournalPath { get; set; }
}
