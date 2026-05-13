using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;

namespace EDAI.UI.ViewModels;

public sealed partial class HistoryViewModel : ObservableObject, IDisposable
{
    private readonly IResponseLogRepository _logRepo;
    private readonly IOutputDispatcher _dispatcher;
    private const int MaxHistory = 100;

    public ObservableCollection<HistoryItem> History { get; } = [];

    public HistoryViewModel(IResponseLogRepository logRepo, IOutputDispatcher dispatcher)
    {
        _logRepo    = logRepo;
        _dispatcher = dispatcher;
        _dispatcher.ResponseReceived += OnResponseReceived;
    }

    public async Task LoadAsync()
    {
        var records = await _logRepo.GetRecentAsync(MaxHistory);
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            History.Clear();
            foreach (var r in records)
                History.Add(ToHistoryItem(r));
        });
    }

    private void OnResponseReceived(object? sender, AiResponseReceivedEventArgs e)
    {
        var item = new HistoryItem
        {
            ConfigTitle     = e.ConfigTitle,
            DisplayedOutput = e.DisplayedOutput,
            PromptSent      = e.PromptSent,
            RawAiResponse   = e.RawAiResponse,
            Timestamp       = e.Timestamp,
        };
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            History.Insert(0, item);
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

    public void Dispose() => _dispatcher.ResponseReceived -= OnResponseReceived;
}
