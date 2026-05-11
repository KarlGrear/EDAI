using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;

namespace EDAI.UI.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _repo;
    private readonly ITtsService _tts;

    // ── OpenAI ──────────────────────────────────────────────────────────────
    [ObservableProperty] private string _openAiApiKey = string.Empty;
    [ObservableProperty] private string _openAiModel = "gpt-4o";

    // ── TTS ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private string? _selectedTtsVoice;
    [ObservableProperty] private bool _ttsEnabled;

    // ── Notifications ───────────────────────────────────────────────────────
    [ObservableProperty] private bool _trayNotificationsEnabled;

    // ── Interface ───────────────────────────────────────────────────────────
    [ObservableProperty] private bool _alwaysOnTop;
    [ObservableProperty] private bool _showSplashScreen = true;

    // ── Font ────────────────────────────────────────────────────────────────
    [ObservableProperty] private string? _selectedFontFamily;
    [ObservableProperty] private double _fontSize = 14.0;

    public ObservableCollection<string> TtsVoices { get; } = [];
    public ObservableCollection<string> FontFamilies { get; } = [];
    public ObservableCollection<string> OpenAiModels { get; } =
    [
        "gpt-5", "gpt-5-mini", "gpt-5-nano",
        "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-4", "gpt-3.5-turbo",
    ];

    public event EventHandler? CloseRequested;

    public SettingsViewModel(ISettingsRepository repo, ITtsService tts)
    {
        _repo = repo;
        _tts = tts;
    }

    public async Task LoadAsync()
    {
        foreach (var v in _tts.GetAvailableVoices())
            TtsVoices.Add(v);

        FontFamilies.Clear();
        foreach (var ff in Fonts.SystemFontFamilies.OrderBy(f => f.Source))
            FontFamilies.Add(ff.Source);

        var s = await _repo.GetAsync();
        OpenAiApiKey             = s.OpenAiApiKey ?? string.Empty;
        OpenAiModel              = s.OpenAiModel;
        SelectedTtsVoice         = s.TtsVoiceName;
        TtsEnabled               = s.TtsEnabled;
        TrayNotificationsEnabled = s.TrayNotificationsEnabled;
        AlwaysOnTop              = s.AlwaysOnTop;
        ShowSplashScreen         = s.ShowSplashScreen;
        SelectedFontFamily       = s.FontFamily;
        FontSize                 = s.FontSize > 0 ? s.FontSize : 14.0;
    }

    [RelayCommand]
    private void TestVoice()
    {
        if (!string.IsNullOrWhiteSpace(SelectedTtsVoice))
            _tts.SetVoice(SelectedTtsVoice);
        _tts.Enqueue("EDAI systems online, Commander.");
    }

    [RelayCommand]
    private async Task Save()
    {
        // Patch only the fields owned by this page; leave theme/colors (managed by ThemeWindow) intact.
        var settings = await _repo.GetAsync();
        settings.OpenAiApiKey             = OpenAiApiKey;
        settings.OpenAiModel              = OpenAiModel;
        settings.TtsVoiceName             = SelectedTtsVoice;
        settings.TtsEnabled               = TtsEnabled;
        settings.TrayNotificationsEnabled = TrayNotificationsEnabled;
        settings.AlwaysOnTop              = AlwaysOnTop;
        settings.ShowSplashScreen         = ShowSplashScreen;
        settings.FontFamily               = string.IsNullOrWhiteSpace(SelectedFontFamily) ? null : SelectedFontFamily;
        settings.FontSize                 = FontSize;

        await _repo.SaveAsync(settings);

        _tts.IsEnabled = TtsEnabled;
        if (!string.IsNullOrWhiteSpace(SelectedTtsVoice))
            _tts.SetVoice(SelectedTtsVoice);

        App.ApplyAppearance(settings);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

}
