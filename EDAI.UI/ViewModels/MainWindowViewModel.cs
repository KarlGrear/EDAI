using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using EDAI.UI.Services;

namespace EDAI.UI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IOutputDispatcher _dispatcher;
    private readonly ITtsService _tts;
    private readonly ISettingsRepository _settingsRepo;
    private readonly INavigationService _navigation;
    private readonly IErrorService _errorService;

    public ObservableCollection<ResponseDisplayItem> Responses { get; } = [];
    public ObservableCollection<EventLogItem> EventLog { get; } = [];

    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasError))] private string? _errorMessage;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty] private bool _isTtsEnabled = true;
    [ObservableProperty] private bool _isAlwaysOnTop;
    public bool MinimizeToTray { get; private set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SplitterColumnWidth))]
    [NotifyPropertyChangedFor(nameof(EventLogColumnWidth))]
    private bool _isEventLogVisible = true;

    public GridLength SplitterColumnWidth =>
        IsEventLogVisible ? new GridLength(4) : new GridLength(0);

    public GridLength EventLogColumnWidth =>
        IsEventLogVisible ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

    private const int MaxResponses     = 200;
    private const int MaxEventLogItems = 500;

    // Guards against partial-method side effects firing during LoadSettingsAsync.
    private bool _loading;

    public MainWindowViewModel(
        IOutputDispatcher dispatcher,
        ITtsService tts,
        ISettingsRepository settingsRepo,
        INavigationService navigation,
        IErrorService errorService)
    {
        _dispatcher   = dispatcher;
        _tts          = tts;
        _settingsRepo = settingsRepo;
        _navigation   = navigation;
        _errorService = errorService;

        _dispatcher.ResponseReceived        += OnResponseReceived;
        _errorService.MinorErrorOccurred    += OnMinorError;
        _errorService.CriticalErrorOccurred += OnCriticalError;
    }

    public async Task LoadSettingsAsync()
    {
        _loading = true;
        try
        {
            var settings = await _settingsRepo.GetAsync();
            IsTtsEnabled   = settings.TtsEnabled;
            IsAlwaysOnTop  = settings.AlwaysOnTop;
            MinimizeToTray = settings.MinimizeToTray;
        }
        finally
        {
            _loading = false;
        }
    }

    // ── TTS toggle ────────────────────────────────────────────────────────────

    partial void OnIsTtsEnabledChanged(bool value)
    {
        _tts.IsEnabled = value;
        if (!_loading) _ = PersistAsync(s => s.TtsEnabled = value);
    }

    // ── Always On Top toggle ──────────────────────────────────────────────────

    partial void OnIsAlwaysOnTopChanged(bool value)
    {
        Application.Current.MainWindow.Topmost = value;
        if (!_loading) _ = PersistAsync(s => s.AlwaysOnTop = value);
    }

    // ── Shared DB persist helper ──────────────────────────────────────────────

    private async Task PersistAsync(Action<SettingsModel> apply)
    {
        var settings = await _settingsRepo.GetAsync();
        apply(settings);
        await _settingsRepo.SaveAsync(settings);
    }

    // ── Journal event ─────────────────────────────────────────────────────────

    public void NotifyJournalEvent(string eventType)
    {
        var item = new EventLogItem { EventType = eventType, Timestamp = DateTime.UtcNow };
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            EventLog.Insert(0, item);
            if (EventLog.Count > MaxEventLogItems)
                EventLog.RemoveAt(EventLog.Count - 1);
            StatusMessage = $"Last event: {eventType} at {item.TimestampDisplay}";
        });
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void NavigateToSettings() =>
        _navigation.ShowSettings(onClosed: () => _ = LoadSettingsAsync());

    [RelayCommand]
    private void NavigateToEventConfigurations() => _navigation.ShowEventConfigurations();

    [RelayCommand]
    private void NavigateToTest() => _navigation.ShowTest();

    [RelayCommand]
    private void NavigateToScriptDesigner() => _navigation.ShowScriptDesigner();

    [RelayCommand]
    private void NavigateToTheme() => _navigation.ShowTheme();

    [RelayCommand]
    private void ShowAbout() => _navigation.ShowAbout();

    [RelayCommand]
    private void ShowHistory() => _navigation.ShowHistory();

    [RelayCommand]
    private void ClearResponses() => Responses.Clear();

    // ── Response / error handlers ─────────────────────────────────────────────

    private void OnResponseReceived(object? sender, AiResponseReceivedEventArgs e)
    {
        var response = new ResponseDisplayItem
        {
            ConfigTitle  = e.ConfigTitle,
            DisplayTitle = e.DisplayTitle,
            Text         = e.DisplayedOutput,
            Timestamp    = e.Timestamp,
        };
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Responses.Insert(0, response);
            if (Responses.Count > MaxResponses)
                Responses.RemoveAt(Responses.Count - 1);
        });
    }

    private void OnMinorError(object? sender, EdaiErrorEventArgs e)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
            ErrorMessage = $"{e.Source}: {e.Message}");
    }

    private void OnCriticalError(object? sender, EdaiErrorEventArgs e)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ErrorMessage = $"CRITICAL — {e.Source}: {e.Message}";
            MessageBox.Show($"{e.Message}", "Elite Dangerous AI — Critical Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }
}
