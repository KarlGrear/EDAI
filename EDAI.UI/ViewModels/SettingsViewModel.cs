using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;

namespace EDAI.UI.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _repo;
    private readonly ITtsService _tts;

    [ObservableProperty] private string _openAiApiKey = string.Empty;
    [ObservableProperty] private string _openAiModel = "gpt-4o";
    [ObservableProperty] private string? _selectedTtsVoice;
    [ObservableProperty] private bool _ttsEnabled;
    [ObservableProperty] private bool _trayNotificationsEnabled;
    [ObservableProperty] private bool _alwaysOnTop;
    [ObservableProperty] private string _theme = "Dark";

    public ObservableCollection<string> TtsVoices { get; } = [];
    public ObservableCollection<string> OpenAiModels { get; } =
    [
        "gpt-4o",
        "gpt-4o-mini",
        "gpt-4-turbo",
        "gpt-4",
        "gpt-3.5-turbo",
    ];

    public event EventHandler? CloseRequested;

    public SettingsViewModel(ISettingsRepository repo, ITtsService tts)
    {
        _repo = repo;
        _tts = tts;
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    public async Task LoadAsync()
    {
        foreach (var v in _tts.GetAvailableVoices())
            TtsVoices.Add(v);

        var settings = await _repo.GetAsync();
        OpenAiApiKey = settings.OpenAiApiKey ?? string.Empty;
        OpenAiModel = settings.OpenAiModel;
        SelectedTtsVoice = settings.TtsVoiceName;
        TtsEnabled = settings.TtsEnabled;
        TrayNotificationsEnabled = settings.TrayNotificationsEnabled;
        AlwaysOnTop = settings.AlwaysOnTop;
        Theme = settings.Theme;
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
        var settings = new SettingsModel
        {
            OpenAiApiKey = OpenAiApiKey,
            OpenAiModel = OpenAiModel,
            TtsVoiceName = SelectedTtsVoice,
            TtsEnabled = TtsEnabled,
            TrayNotificationsEnabled = TrayNotificationsEnabled,
            AlwaysOnTop = AlwaysOnTop,
            Theme = Theme,
        };
        await _repo.SaveAsync(settings);

        _tts.IsEnabled = TtsEnabled;
        if (!string.IsNullOrWhiteSpace(SelectedTtsVoice))
            _tts.SetVoice(SelectedTtsVoice);

        App.ApplyTheme(Theme);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
