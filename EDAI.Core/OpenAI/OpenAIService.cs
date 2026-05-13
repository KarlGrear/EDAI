using EDAI.Core.Interfaces;
using EDAI.Core.Pipeline;
using Microsoft.Extensions.Logging;
using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

namespace EDAI.Core.OpenAI;

public sealed class OpenAIService : IOpenAIService
{
    private readonly ISettingsRepository _settings;
    private readonly IErrorService _errorService;
    private readonly ILogger<OpenAIService> _logger;

    // Tracks whether we have already shown the critical "no API key" dialog this session.
    // Reset to false whenever a call succeeds so that removing and re-adding a key triggers
    // the prominent dialog again rather than silently showing a status-bar message.
    private bool _missingKeyDialogShown;

    public OpenAIService(
        ISettingsRepository settings,
        IErrorService errorService,
        ILogger<OpenAIService> logger)
    {
        _settings = settings;
        _errorService = errorService;
        _logger = logger;
    }

    public async Task<string> SendAsync(string prompt, string? modelOverride = null, CancellationToken cancellationToken = default)
    {
        var settingsModel = await _settings.GetAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(settingsModel.OpenAiApiKey))
        {
            const string msg = "OpenAI API key is not configured. Open Settings to enter your API key.";
            if (!_missingKeyDialogShown)
            {
                _missingKeyDialogShown = true;
                _errorService.ReportCritical(nameof(OpenAIService), msg);
            }
            else
            {
                _errorService.ReportMinor(nameof(OpenAIService), msg);
            }
            return string.Empty;
        }

        var model = !string.IsNullOrWhiteSpace(modelOverride) ? modelOverride : settingsModel.OpenAiModel;
        var client = new OpenAIClient(new ApiKeyCredential(settingsModel.OpenAiApiKey));
        var chatClient = client.GetChatClient(model);

        var persona = settingsModel.SystemPersona;

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(persona),
            new UserChatMessage(prompt)
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation(
            "OpenAI Request | Model: {Model} | URL: https://api.openai.com/v1/chat/completions\n" +
            "--- System ---\n{System}\n--- User ---\n{Prompt}",
            model, persona, prompt);

        try
        {
            var completion = await chatClient
                .CompleteChatAsync(messages, options, cancellationToken)
                .ConfigureAwait(false);

            sw.Stop();
            var text = completion.Value.Content[0].Text ?? string.Empty;

            _logger.LogInformation(
                "OpenAI Response | Model: {Model} | Elapsed: {ElapsedMs}ms\n--- Response ---\n{Response}",
                model, (long)sw.Elapsed.TotalMilliseconds, text);

            // A successful call means the key is now valid — reset so that if the user
            // later clears the key they get the prominent dialog again.
            _missingKeyDialogShown = false;
            return text;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "OpenAI Error | Model: {Model} | Elapsed: {ElapsedMs}ms | {Message}",
                model, (long)sw.Elapsed.TotalMilliseconds, ex.Message);
            _errorService.ReportMinor(nameof(OpenAIService), $"OpenAI call failed: {ex.Message}", ex);
            return string.Empty;
        }
    }
}
