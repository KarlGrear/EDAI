using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDAI.Core.Journal;
using EDAI.Core.Models;
using EDAI.Core.Scripting;
using EDAI.Core.TTS;
using EDAI.Core.Interfaces;
using EDAI.UI.Services;
using Microsoft.Extensions.Logging;

namespace EDAI.UI.ViewModels;

public sealed record TtsLanguageOption(string Display, string Locale);

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _repo;
    private readonly CompositeTtsService _tts;
    private readonly VoiceCacheService _voiceCache;
    private readonly IFileDialogService _fileDialog;
    private readonly JournalPathOptions _journalOptions;
    private readonly IScriptingService _scriptingService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<SettingsViewModel> _logger;

    // Set by the view so the ViewModel can show a confirmation dialog without referencing WPF types.
    public Func<string, string, bool>? ShowConfirmation { get; set; }

    // Set by the view to open the theme customization window.
    public Action? OpenThemeRequested { get; set; }

    // Captured on LoadAsync so Cancel can restore the composite to its pre-edit state.
    private string _originalProvider = CompositeTtsService.ProviderSapi;

    // ── OpenAI ──────────────────────────────────────────────────────────────
    [ObservableProperty] private string _openAiApiKey    = string.Empty;
    [ObservableProperty] private string _openAiModel     = "gpt-4o";
    [ObservableProperty] private string _systemPersona   = SettingsModel.DefaultSystemPersona;

    // ── TTS engine ──────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSapiEngine))]
    [NotifyPropertyChangedFor(nameof(IsEdgeEngine))]
    private string _selectedTtsEngine = "Windows Speech (SAPI)";

    public bool IsSapiEngine => SelectedTtsEngine == "Windows Speech (SAPI)";
    public bool IsEdgeEngine => SelectedTtsEngine == "Edge Neural Voices (Online)";

    // ── SAPI voice ───────────────────────────────────────────────────────────
    [ObservableProperty] private string? _selectedTtsVoice;

    // ── Edge language ────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredEdgeVoices))]
    private TtsLanguageOption? _selectedEdgeLanguage;

    // ── Edge voice ───────────────────────────────────────────────────────────
    [ObservableProperty] private EdgeVoiceInfo? _selectedEdgeVoice;

    // ── Edge rate / pitch ────────────────────────────────────────────────────
    [ObservableProperty] private double _edgeTtsRate  = 1.0;
    [ObservableProperty] private double _edgeTtsPitch = 1.0;

    // ── Global TTS settings ─────────────────────────────────────────────────
    [ObservableProperty] private bool _ttsEnabled;

    // ── Notifications ───────────────────────────────────────────────────────
    [ObservableProperty] private bool _trayNotificationsEnabled;

    // ── Interface ───────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _alwaysOnTop;
    [ObservableProperty] private bool   _showSplashScreen = true;
    [ObservableProperty] private bool   _minimizeToTray   = true;

    // ── Font ────────────────────────────────────────────────────────────────
    [ObservableProperty] private string? _selectedFontFamily;
    [ObservableProperty] private double  _fontSize = 14.0;

    // ── Journal path ─────────────────────────────────────────────────────────
    [ObservableProperty] private string _journalPath = SettingsModel.DefaultJournalPath;

    // ── Scripting permissions ────────────────────────────────────────────────
    [ObservableProperty] private bool _scriptingAllowFileSystem;
    [ObservableProperty] private bool _scriptingAllowNetwork;
    [ObservableProperty] private bool _scriptingAllowProcessExecution;
    [ObservableProperty] private bool _scriptingAllowReflection;

    // ── Collections ─────────────────────────────────────────────────────────
    public ObservableCollection<string> TtsEngines { get; } =
        ["Windows Speech (SAPI)", "Edge Neural Voices (Online)"];

    public ObservableCollection<string> TtsVoices { get; } = [];

    public ObservableCollection<TtsLanguageOption> EdgeLanguages { get; } =
    [
        new("English (United States)", "en-US"),
        new("English (United Kingdom)", "en-GB"),
    ];

    public ObservableCollection<EdgeVoiceInfo> FilteredEdgeVoices { get; } = [];

    public ObservableCollection<string> FontFamilies   { get; } = [];
    public ObservableCollection<string> OpenAiModels   { get; } =
    [
        "gpt-5", "gpt-5-mini", "gpt-5-nano",
        "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-4", "gpt-3.5-turbo",
    ];

    public event EventHandler? CloseRequested;

    public SettingsViewModel(
        ISettingsRepository repo,
        CompositeTtsService tts,
        VoiceCacheService voiceCache,
        IFileDialogService fileDialog,
        JournalPathOptions journalOptions,
        IScriptingService scriptingService,
        ISessionService sessionService,
        ILogger<SettingsViewModel> logger)
    {
        _repo             = repo;
        _tts              = tts;
        _voiceCache       = voiceCache;
        _fileDialog       = fileDialog;
        _journalOptions   = journalOptions;
        _scriptingService = scriptingService;
        _sessionService   = sessionService;
        _logger           = logger;
    }

    public async Task LoadAsync()
    {
        // Populate SAPI voices
        TtsVoices.Clear();
        foreach (var v in _tts.GetAvailableVoices())
            TtsVoices.Add(v);

        // Populate font list
        FontFamilies.Clear();
        foreach (var ff in Fonts.SystemFontFamilies.OrderBy(f => f.Source))
            FontFamilies.Add(ff.Source);

        var s = await _repo.GetAsync();

        OpenAiApiKey             = s.OpenAiApiKey ?? string.Empty;
        OpenAiModel              = s.OpenAiModel;
        SystemPersona            = s.SystemPersona;
        TtsEnabled               = s.TtsEnabled;
        TrayNotificationsEnabled = s.TrayNotificationsEnabled;
        AlwaysOnTop              = s.AlwaysOnTop;
        ShowSplashScreen         = s.ShowSplashScreen;
        MinimizeToTray           = s.MinimizeToTray;
        SelectedFontFamily       = s.FontFamily;
        FontSize                 = s.FontSize > 0 ? s.FontSize : 14.0;
        JournalPath              = s.JournalPath;

        ScriptingAllowFileSystem       = s.ScriptingAllowFileSystem;
        ScriptingAllowNetwork          = s.ScriptingAllowNetwork;
        ScriptingAllowProcessExecution = s.ScriptingAllowProcessExecution;
        ScriptingAllowReflection       = s.ScriptingAllowReflection;

        // Engine
        _originalProvider   = s.TtsProvider;
        SelectedTtsEngine   = s.TtsProvider == CompositeTtsService.ProviderEdge
            ? "Edge Neural Voices (Online)"
            : "Windows Speech (SAPI)";

        // SAPI voice
        SelectedTtsVoice = s.TtsVoiceName;

        EdgeTtsRate  = s.EdgeTtsRate  > 0 ? s.EdgeTtsRate  : 1.0;
        EdgeTtsPitch = s.EdgeTtsPitch > 0 ? s.EdgeTtsPitch : 1.0;

        // Edge language — default to en-US if not saved
        SelectedEdgeLanguage = EdgeLanguages
            .FirstOrDefault(l => l.Locale == s.EdgeTtsLanguage)
            ?? EdgeLanguages.First();

        // Edge voices list (may already be populated if loaded in background)
        RefreshFilteredEdgeVoices();
        SelectedEdgeVoice = FilteredEdgeVoices
            .FirstOrDefault(v => v.ShortName == s.EdgeTtsVoice)
            ?? FilteredEdgeVoices.FirstOrDefault();
    }

    // When language selection changes, re-filter the Edge voice list.
    partial void OnSelectedEdgeLanguageChanged(TtsLanguageOption? value)
        => RefreshFilteredEdgeVoices();

    // When engine dropdown changes, update the composite immediately so Test Voice works.
    partial void OnSelectedTtsEngineChanged(string value)
    {
        _tts.ActiveProvider = value == "Edge Neural Voices (Online)"
            ? CompositeTtsService.ProviderEdge
            : CompositeTtsService.ProviderSapi;

        // Switching to SAPI: repopulate TtsVoices from the SAPI engine now that
        // ActiveProvider is correct. Without this, TtsVoices still contains Edge
        // voice names from the initial LoadAsync call.
        if (IsSapiEngine)
        {
            TtsVoices.Clear();
            foreach (var v in _tts.GetAvailableVoices())
                TtsVoices.Add(v);
            SelectedTtsVoice = TtsVoices.FirstOrDefault();
        }
    }

    // When the user picks an Edge voice, pre-apply it so Test Voice speaks correctly.
    partial void OnSelectedEdgeVoiceChanged(EdgeVoiceInfo? value)
    {
        if (value != null && IsEdgeEngine)
            _tts.SetVoice(value.ShortName);
    }

    private void RefreshFilteredEdgeVoices()
    {
        FilteredEdgeVoices.Clear();
        var locale = SelectedEdgeLanguage?.Locale;
        if (locale == null) return;

        foreach (var v in _tts.GetEdgeVoices().Where(v => v.Locale == locale))
            FilteredEdgeVoices.Add(v);

        // Restore selection or default to first
        SelectedEdgeVoice = FilteredEdgeVoices.FirstOrDefault();
    }

    [RelayCommand]
    private void OpenTheme() => OpenThemeRequested?.Invoke();

    [RelayCommand]
    private void ResetSystemPersona() => SystemPersona = SettingsModel.DefaultSystemPersona;

    [RelayCommand]
    private void TestVoice()
    {
        if (IsSapiEngine && !string.IsNullOrWhiteSpace(SelectedTtsVoice))
            _tts.SetVoice(SelectedTtsVoice);

        if (IsEdgeEngine && SelectedEdgeVoice != null)
            _tts.ConfigureEdge(
                SelectedEdgeVoice.ShortName,
                SelectedEdgeLanguage?.Locale ?? "en-US",
                EdgeTtsRate,
                EdgeTtsPitch);

        _tts.Enqueue("EDAI systems online, Commander.");
    }

    [RelayCommand]
    private async Task Save()
    {
        var settings = await _repo.GetAsync();

        settings.OpenAiApiKey             = OpenAiApiKey;
        settings.OpenAiModel              = OpenAiModel;
        settings.SystemPersona            = SystemPersona;
        settings.TtsEnabled               = TtsEnabled;
        settings.TrayNotificationsEnabled = TrayNotificationsEnabled;
        settings.AlwaysOnTop              = AlwaysOnTop;
        settings.ShowSplashScreen         = ShowSplashScreen;
        settings.MinimizeToTray           = MinimizeToTray;
        settings.FontFamily               = string.IsNullOrWhiteSpace(SelectedFontFamily) ? null : SelectedFontFamily;
        settings.FontSize                 = FontSize;
        settings.JournalPath              = JournalPath;

        // TTS engine
        settings.TtsProvider = IsEdgeEngine
            ? CompositeTtsService.ProviderEdge
            : CompositeTtsService.ProviderSapi;

        // SAPI voice
        settings.TtsVoiceName = SelectedTtsVoice;

        // Edge voice / rate / pitch
        settings.EdgeTtsLanguage = SelectedEdgeLanguage?.Locale;
        settings.EdgeTtsVoice    = SelectedEdgeVoice?.ShortName;
        settings.EdgeTtsRate     = EdgeTtsRate;
        settings.EdgeTtsPitch    = EdgeTtsPitch;

        settings.ScriptingAllowFileSystem       = ScriptingAllowFileSystem;
        settings.ScriptingAllowNetwork          = ScriptingAllowNetwork;
        settings.ScriptingAllowProcessExecution = ScriptingAllowProcessExecution;
        settings.ScriptingAllowReflection       = ScriptingAllowReflection;

        await _repo.SaveAsync(settings);

        _scriptingService.UpdatePermissions(new ScriptingPermissions
        {
            FileSystem       = ScriptingAllowFileSystem,
            Network          = ScriptingAllowNetwork,
            ProcessExecution = ScriptingAllowProcessExecution,
            Reflection       = ScriptingAllowReflection,
        });

        // Apply to composite
        _originalProvider        = settings.TtsProvider;
        _tts.ActiveProvider      = settings.TtsProvider;
        _tts.IsEnabled           = TtsEnabled;

        if (IsEdgeEngine && SelectedEdgeVoice != null)
        {
            _tts.ConfigureEdge(
                SelectedEdgeVoice.ShortName,
                SelectedEdgeLanguage?.Locale ?? "en-US",
                EdgeTtsRate,
                EdgeTtsPitch);
        }
        else if (IsSapiEngine && !string.IsNullOrWhiteSpace(SelectedTtsVoice))
            _tts.SetVoice(SelectedTtsVoice);

        _journalOptions.Path = JournalPath;
        App.ApplyAppearance(settings);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void BrowseJournalPath()
    {
        var chosen = _fileDialog.BrowseFolder("Select Elite Dangerous Journal Folder", JournalPath);
        if (chosen != null)
            JournalPath = chosen;
    }

    [RelayCommand]
    private async Task ClearVoiceCache()
    {
        var confirmed = ShowConfirmation?.Invoke(
            "This will permanently delete all cached voice audio files and database entries. Continue?",
            "Clear Voice Cache") ?? false;
        if (!confirmed) return;

        var (files, rows) = await _voiceCache.ClearCacheAsync();
        _logger.LogInformation("Voice Cache Cleared. Removed {Files} file{FS} and {Rows} database {RS}.",
            files, files == 1 ? "" : "s",
            rows,  rows  == 1 ? "entry" : "entries");
    }

    [RelayCommand]
    private void ClearSession()
    {
        var confirmed = ShowConfirmation?.Invoke(
            "This will clear all values from session.json. Scripts that read session variables will see an empty object until they write new values. Continue?",
            "Clear Session") ?? false;
        if (!confirmed) return;

        _sessionService.Clear();
        _logger.LogInformation("Session cleared.");
    }

    [RelayCommand]
    private void Cancel()
    {
        // Revert composite to whatever it was before this settings session opened.
        _tts.ActiveProvider = _originalProvider;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
