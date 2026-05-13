using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using EDAI.Core.Pipeline;
using EDAI.UI.Validators;
using Microsoft.Extensions.Logging;

namespace EDAI.UI.ViewModels;

public sealed partial class TestViewModel : ObservableValidator
{
    private readonly IJournalParser _parser;
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly IOutputDispatcher _dispatcher;
    private readonly IEventConfigurationRepository _configRepo;
    private readonly IJournalAuxFileReader _auxReader;
    private readonly ILogger<TestViewModel> _logger;

    // ── Pipeline Test tab ────────────────────────────────────────────────────
    [ObservableProperty]
    [CustomValidation(typeof(JsonValidator), nameof(JsonValidator.ValidateJsonLines))]
    private string _inputJson = string.Empty;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private EventConfigurationModel? _selectedConfig;

    public ObservableCollection<ResponseDisplayItem> TestResponses { get; } = [];

    /// <summary>Null entry represents "All Configurations" (normal trigger-matched pipeline).</summary>
    public ObservableCollection<EventConfigurationModel?> Configs { get; } = [];

    // ── Template Tester tab ──────────────────────────────────────────────────
    [ObservableProperty]
    [CustomValidation(typeof(JsonValidator), nameof(JsonValidator.ValidateSingleObject))]
    private string _templateTriggerJson = string.Empty;

    [ObservableProperty]
    [CustomValidation(typeof(JsonValidator), nameof(JsonValidator.ValidateJsonLines))]
    private string _templateSecondaryJson = string.Empty;

    [ObservableProperty]
    [CustomValidation(typeof(JsonValidator), nameof(JsonValidator.ValidateSingleObject))]
    private string _templateResultJson = string.Empty;
    [ObservableProperty] private string _templateInput          = string.Empty;
    [ObservableProperty] private string _templateCondition      = string.Empty;
    [ObservableProperty] private string _templateOutput         = string.Empty;
    [ObservableProperty] private string _templateStatus         = string.Empty;
    [ObservableProperty] private string _templateConditionResult = string.Empty;

    [RelayCommand]
    private void EvaluateTemplate() => RunEvaluation();

    internal void RunEvaluation()
    {
        TemplateOutput = string.Empty;
        TemplateConditionResult = string.Empty;

        _logger.LogInformation("Template evaluation started. Input length={Len}", TemplateInput?.Length ?? 0);
        try
        {
            var trigger   = string.IsNullOrWhiteSpace(TemplateTriggerJson)   ? null : TemplateTriggerJson;
            var secondary = BuildSecondaryJson(TemplateSecondaryJson);
            var result    = string.IsNullOrWhiteSpace(TemplateResultJson)    ? null : TemplateResultJson;

            if (!string.IsNullOrWhiteSpace(TemplateCondition))
            {
                bool conditionPassed;
                try
                {
                    conditionPassed = ConditionEvaluator.Evaluate(TemplateCondition, trigger, result, _auxReader.Read, secondary);
                    TemplateConditionResult = conditionPassed ? "True" : "False";
                }
                catch (Exception ex)
                {
                    TemplateConditionResult = "Error";
                    TemplateStatus = $"Condition error: {ex.Message}";
                    _logger.LogWarning(ex, "Condition evaluation failed");
                    return;
                }

                if (!conditionPassed)
                {
                    TemplateStatus = $"Condition false — no output ({DateTime.Now:HH:mm:ss})";
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(TemplateInput))
            {
                TemplateStatus = "Template is empty.";
                return;
            }

            TemplateOutput = TemplateEngine.Apply(TemplateInput, trigger, result, _auxReader.Read, secondary);
            TemplateStatus = $"Evaluated at {DateTime.Now:HH:mm:ss}";
            _logger.LogInformation("Template evaluation complete. Output={Output}", TemplateOutput);
        }
        catch (Exception ex)
        {
            TemplateOutput = string.Empty;
            TemplateStatus = $"Error: {ex.Message}";
            _logger.LogWarning(ex, "Template evaluation failed");
        }
    }

    public string AuxIdentifiersHint =>
        "Aux files: " + string.Join("   ", _auxReader.KnownIdentifiers.Select(id => $"|{id}…|"));

    public TestViewModel(
        IJournalParser parser,
        IPipelineOrchestrator orchestrator,
        IOutputDispatcher dispatcher,
        IEventConfigurationRepository configRepo,
        IJournalAuxFileReader auxReader,
        ILogger<TestViewModel> logger)
    {
        _parser = parser;
        _orchestrator = orchestrator;
        _dispatcher = dispatcher;
        _configRepo = configRepo;
        _auxReader = auxReader;
        _logger = logger;

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

            bool triggerDispatched = false;
            foreach (var line in lines)
            {
                var parsed = _parser.TryParse(line);
                if (parsed == null) continue;

                if (SelectedConfig != null)
                {
                    if (!triggerDispatched)
                    {
                        // First event is the trigger — start the pipeline.
                        await _orchestrator.ProcessWithConfigAsync(parsed, SelectedConfig);
                        triggerDispatched = true;

                        // The pipeline consumer runs on a thread-pool thread via Task.Run.
                        // Yield briefly so it can reach the SecondaryEventCollector
                        // registration point before we feed subsequent events to it.
                        if (SelectedConfig.SecondaryEvents.Count > 0 && SelectedConfig.SecondaryWaitTimeMs > 0)
                            await Task.Delay(50);
                    }
                    else
                    {
                        // Subsequent events: feed through normal pipeline so they reach
                        // active secondary collectors without starting additional pipeline runs.
                        await _orchestrator.ProcessAsync(parsed);
                    }
                }
                else
                {
                    await _orchestrator.ProcessAsync(parsed);
                }
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
            DisplayTitle = e.DisplayTitle,
            Text = e.DisplayedOutput,
            Timestamp = e.Timestamp,
        };
        System.Windows.Application.Current.Dispatcher.InvokeAsync(
            () => TestResponses.Insert(0, item));
    }

    // Converts one-JSON-object-per-line input into a JSON array string, matching
    // the format PipelineContext.SecondaryJson produces from live journal events.
    private static string? BuildSecondaryJson(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var lines = input.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return lines.Length == 0 ? null : "[" + string.Join(",", lines) + "]";
    }
}
