namespace EDAI.Data.Entities;

public sealed class SettingEntity
{
    public int Id { get; set; }
    public string? OpenAiApiKeyEncrypted { get; set; }
    public string OpenAiModel { get; set; } = "gpt-4o";
    public string? TtsVoiceName { get; set; }
    public bool TtsEnabled { get; set; } = true;
    public bool AlwaysOnTop { get; set; }
    public bool TrayNotificationsEnabled { get; set; } = true;
    public string Theme { get; set; } = "Dark";
    public double WindowWidth { get; set; } = 900;
    public double WindowHeight { get; set; } = 600;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
}
