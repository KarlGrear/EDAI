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
            _errorService.ReportMinor(nameof(OpenAIService), "OpenAI API key is not configured.");
            return string.Empty;
        }

        var model = !string.IsNullOrWhiteSpace(modelOverride) ? modelOverride : settingsModel.OpenAiModel;
        var client = new OpenAIClient(new ApiKeyCredential(settingsModel.OpenAiApiKey));
        var chatClient = client.GetChatClient(model);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(PromptBuilder.SystemPersona),
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
            model, PromptBuilder.SystemPersona, prompt);

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
