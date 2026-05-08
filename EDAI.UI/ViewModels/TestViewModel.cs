using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;

namespace EDAI.UI.ViewModels;

public sealed partial class TestViewModel : ObservableObject
{
    private readonly IJournalParser _parser;
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly IOutputDispatcher _dispatcher;
    private readonly IEventConfigurationRepository _configRepo;

    [ObservableProperty] private string _inputJson = string.Empty;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private EventConfigurationModel? _selectedConfig;

    public ObservableCollection<ResponseDisplayItem> TestResponses { get; } = [];

    /// <summary>Null entry represents "All Configurations" (normal trigger-matched pipeline).</summary>
    public ObservableCollection<EventConfigurationModel?> Configs { get; } = [];

    public TestViewModel(
        IJournalParser parser,
        IPipelineOrchestrator orchestrator,
        IOutputDispatcher dispatcher,
        IEventConfigurationRepository configRepo)
    {
        _parser = parser;
        _orchestrator = orchestrator;
        _dispatcher = dispatcher;
        _configRepo = configRepo;

        _dispatcher.ResponseReceived += OnResponseReceived;
    }

    public async Task LoadAsync(int? preselectedConfigId = null)
    {
        Configs.Clear();
        Configs.Add(null); // "All Configurations"

        var all = await _configRepo.GetAllAsync();
        foreach (var c in all) Configs.Add(c);

        SelectedConfig = preselectedConfigId.HasValue
            ? all.FirstOrDefault(c => c.Id == preselectedConfigId.Value)
            : null;
    }

    [RelayCommand]
    private async Task RunTest()
    {
        if (IsRunning) return;
        IsRunning = true;
        TestResponses.Clear();

        try
        {
            var lines = InputJson
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var line in lines)
            {
                var parsed = _parser.TryParse(line);
                if (parsed == null) continue;

                if (SelectedConfig != null)
                    await _orchestrator.ProcessWithConfigAsync(parsed, SelectedConfig);
                else
                    await _orchestrator.ProcessAsync(parsed);
            }
        }
        finally
        {
            IsRunning = false;
        }
    }

    private void OnResponseReceived(object? sender, AiResponseReceivedEventArgs e)
    {
        var item = new ResponseDisplayItem
        {
            ConfigTitle = e.ConfigTitle,
            TitleDisplayMode = e.TitleDisplayMode,
            Text = e.DisplayedOutput,
            Timestamp = e.Timestamp,
        };
        System.Windows.Application.Current.Dispatcher.InvokeAsync(
            () => TestResponses.Insert(0, item));
    }
}
