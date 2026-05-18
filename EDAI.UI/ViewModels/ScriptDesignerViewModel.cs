using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDAI.Core.Interfaces;
using EDAI.Core.Scripting;

namespace EDAI.UI.ViewModels;

public sealed partial class ScriptDesignerViewModel : ObservableObject
{
    private readonly IScriptingService _scriptingService;
    private readonly ISessionService _sessionService;

    public bool IsProcessScript  { get; private set; }
    public bool IsStandalone     { get; private set; }
    public bool IsNotStandalone  => !IsStandalone;
    public string WindowTitle    { get; private set; } = "Script Designer";
    public string OkButtonLabel  => IsStandalone ? "Close" : "OK";

    [ObservableProperty] private string _scriptText = string.Empty;
    [ObservableProperty] private string _testTriggerJson = "{\n  \"event\": \"TestEvent\",\n  \"StarSystem\": \"Sol\"\n}";
    [ObservableProperty] private string _validationOutput = string.Empty;
    [ObservableProperty] private bool   _hasValidationErrors;
    [ObservableProperty] private string _testOutput = string.Empty;
    [ObservableProperty] private bool   _hasTestOutput;
    [ObservableProperty] private bool   _isTestRunning;

    public string GlobalsReference => BuildGlobalsReference();

    public ScriptDesignerViewModel(IScriptingService scriptingService, ISessionService sessionService)
    {
        _scriptingService = scriptingService;
        _sessionService   = sessionService;
    }

    public void Setup(bool isProcessScript, string? existingScript)
    {
        IsProcessScript = isProcessScript;
        WindowTitle     = isProcessScript ? "Script Designer — Process Script" : "Script Designer — Condition Script";
        ScriptText      = existingScript ?? DefaultTemplate(isProcessScript);
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(GlobalsReference));
    }

    public void SetupStandalone(bool isProcessScript = true)
    {
        IsStandalone = true;
        Setup(isProcessScript, null);
        OnPropertyChanged(nameof(OkButtonLabel));
    }

    [RelayCommand]
    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(ScriptText))
        {
            ValidationOutput    = "Script is empty.";
            HasValidationErrors = false;
            return;
        }

        var errors = _scriptingService.ValidateScript(ScriptText);
        if (errors.Count == 0)
        {
            ValidationOutput    = "No errors found.";
            HasValidationErrors = false;
        }
        else
        {
            ValidationOutput    = string.Join("\n", errors);
            HasValidationErrors = true;
        }
    }

    [RelayCommand]
    private async Task TestRunAsync()
    {
        if (string.IsNullOrWhiteSpace(ScriptText)) return;

        IsTestRunning = true;
        TestOutput    = "Compiling...";
        HasTestOutput = true;

        try
        {
            var globals = BuildTestGlobals();
            var output  = await _scriptingService
                .RunForTestAsync(ScriptText, IsProcessScript, globals)
                .ConfigureAwait(true);   // resume on UI thread so WPF binding picks up the update
            TestOutput    = output;
            HasTestOutput = true;
        }
        catch (Exception ex)
        {
            TestOutput    = $"Error: {ex.Message}";
            HasTestOutput = true;
        }
        finally
        {
            IsTestRunning = false;
        }
    }

    private ScriptGlobals BuildTestGlobals()
    {
        JsonNode? ParseOrNull(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonNode.Parse(json); } catch { return null; }
        }

        return new ScriptGlobals(_sessionService)
        {
            Trigger = ParseOrNull(TestTriggerJson),
        };
    }

    private static string DefaultTemplate(bool isProcessScript) => isProcessScript
        ? """
          // Process script — populate Result fields to drive display and announce.
          // Trigger, Secondary, NavRoute, Status, Market, Outfitting, Shipyard,
          // ShipLocker, ModulesInfo are available as JsonNode? globals.

          var starSystem = Trigger?["StarSystem"]?.GetValue<string>();
          Result.Announcement = $"Arrived in {starSystem}.";
          """
        : """
          // Condition script — return true to proceed, false to skip.
          // Trigger, Secondary, NavRoute, Status, Market, etc. are available.

          var remainingJumps = Trigger?["RemainingJumpsInRoute"]?.GetValue<int>() ?? 0;
          return remainingJumps >= 3;
          """;

    private static string BuildGlobalsReference() => """
        ── Globals available in every script ──────────────────

        Trigger       JsonNode?   The triggering journal event
        Secondary     JsonArray?  Secondary events collected after trigger
        NavRoute      JsonNode?   Current NavRoute.json content
        Status        JsonNode?   Current Status.json content
        Market        JsonNode?   Current Market.json content
        Outfitting    JsonNode?   Current Outfitting.json content
        Shipyard      JsonNode?   Current Shipyard.json content
        ShipLocker    JsonNode?   Current ShipLocker.json content
        ModulesInfo   JsonNode?   Current ModulesInfo.json content
        Session       JsonNode?   session.json — always read fresh at each access

        ── Process scripts only ───────────────────────────────

        Result.Announcement    string?   TTS announcement text
        Result.Display         string?   Display panel text
        Result["key"]          string?   Custom field (|result.key| token)

        ── Session read ───────────────────────────────────────

        var count = Session?["jumpCount"]?.GetValue<int>() ?? 0;
        var name  = Session?["lastSystem"]?.GetValue<string>();
        var flag  = Session?["warned"]?.GetValue<bool>() ?? false;

        ── Session write ──────────────────────────────────────

        SetSession("jumpCount", JsonValue.Create(count + 1));
        SetSession("lastSystem", JsonValue.Create("Sol"));
        SetSession("warned", JsonValue.Create(true));
        DeleteSession("tempKey");   // remove one key
        ClearSession();             // clear all keys

        ── JsonNode access patterns ───────────────────────────

        Trigger?["StarSystem"]?.GetValue<string>()
        Trigger?["RemainingJumpsInRoute"]?.GetValue<int>() ?? 0
        Trigger?["StarPos"]?[0]?.GetValue<double>()
        Trigger?["Factions"]?.AsArray()
          ?.FirstOrDefault(f => f?["Allegiance"]?.GetValue<string>() == "Empire")
        """;
}
