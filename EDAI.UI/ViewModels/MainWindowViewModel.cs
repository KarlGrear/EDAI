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
    private readonly IResponseLogRepository _logRepo;
    private readonly INavigationService _navigation;
    private readonly IErrorService _errorService;

    public ObservableCollection<ResponseDisplayItem> Responses { get; } = [];
    public ObservableCollection<EventLogItem> EventLog { get; } = [];
    public ObservableCollection<HistoryItem> History { get; } = [];

    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasError))] private string? _errorMessage;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    [ObservableProperty] private bool _isTtsEnabled = true;
    [ObservableProperty] private bool _isAlwaysOnTop;

    private const int MaxResponses = 200;
    private const int MaxEventLogItems = 500;
    private const int MaxHistory = 100;

    public MainWindowViewModel(
        IOutputDispatcher dispatcher,
        ITtsService tts,
        ISettingsRepository settingsRepo,
        IResponseLogRepository logRepo,
        INavigationService navigation,
        IErrorService errorService)
    {
        _dispatcher = dispatcher;
        _tts = tts;
        _settingsRepo = settingsRepo;
        _logRepo = logRepo;
        _navigation = navigation;
        _errorService = errorService;

        _dispatcher.ResponseReceived += OnResponseReceived;
        _errorService.MinorErrorOccurred += OnMinorError;
        _errorService.CriticalErrorOccurred += OnCriticalError;
    }

    public async Task LoadSettingsAsync()
    {
        var settings = await _settingsRepo.GetAsync();
        IsTtsEnabled = settings.TtsEnabled;
        IsAlwaysOnTop = settings.AlwaysOnTop;
    }

    public async Task LoadHistoryAsync()
    {
        var records = await _logRepo.GetRecentAsync(MaxHistory);
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            History.Clear();
            foreach (var r in records)
                History.Add(ToHistoryItem(r));
        });
    }

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

    [RelayCommand]
    private async Task ToggleAlwaysOnTop()
    {
        IsAlwaysOnTop = !IsAlwaysOnTop;
        Application.Current.MainWindow.Topmost = IsAlwaysOnTop;
        var settings = await _settingsRepo.GetAsync();
        settings.AlwaysOnTop = IsAlwaysOnTop;
        await _settingsRepo.SaveAsync(settings);
    }

    [RelayCommand]
    private async Task ToggleTts()
    {
        IsTtsEnabled = !IsTtsEnabled;
        _tts.IsEnabled = IsTtsEnabled;
        var settings = await _settingsRepo.GetAsync();
        settings.TtsEnabled = IsTtsEnabled;
        await _settingsRepo.SaveAsync(settings);
    }

    [RelayCommand]
    private void NavigateToSettings() => _navigation.ShowSettings();

    [RelayCommand]
    private void NavigateToEventConfigurations() => _navigation.ShowEventConfigurations();

    [RelayCommand]
    private void NavigateToTest() => _navigation.ShowTest();

    [RelayCommand]
    private void NavigateToTheme() => _navigation.ShowTheme();

    [RelayCommand]
    private void ShowAbout() => _navigation.ShowAbout();

    [RelayCommand]
    private void ClearResponses() => Responses.Clear();

    private void OnResponseReceived(object? sender, AiResponseReceivedEventArgs e)
    {
        var response = new ResponseDisplayItem
        {
            ConfigTitle  = e.ConfigTitle,
            DisplayTitle = e.DisplayTitle,
            Text         = e.DisplayedOutput,
            Timestamp    = e.Timestamp,
        };
        var history = new HistoryItem
        {
            ConfigTitle     = e.ConfigTitle,
            DisplayedOutput = e.DisplayedOutput,
            PromptSent      = e.PromptSent,
            RawAiResponse   = e.RawAiResponse,
            Timestamp       = e.Timestamp,
        };
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Responses.Insert(0, response);
            if (Responses.Count > MaxResponses)
                Responses.RemoveAt(Responses.Count - 1);

            History.Insert(0, history);
            if (History.Count > MaxHistory)
                History.RemoveAt(History.Count - 1);
        });
    }

    private static HistoryItem ToHistoryItem(ResponseLogModel r) => new()
    {
        ConfigTitle     = r.ConfigTitle ?? $"Config #{r.EventConfigurationId}",
        DisplayedOutput = r.DisplayedOutput,
        PromptSent      = r.PromptSent,
        RawAiResponse   = r.RawAiResponse,
        Timestamp       = r.Timestamp,
    };

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
