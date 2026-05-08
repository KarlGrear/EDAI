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

    public async Task<string> SendAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var settingsModel = await _settings.GetAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(settingsModel.OpenAiApiKey))
        {
            _errorService.ReportMinor(nameof(OpenAIService), "OpenAI API key is not configured.");
            return string.Empty;
        }

        var client = new OpenAIClient(new ApiKeyCredential(settingsModel.OpenAiApiKey));
        var chatClient = client.GetChatClient(settingsModel.OpenAiModel);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(PromptBuilder.SystemPersona),
            new UserChatMessage(prompt)
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        _logger.LogDebug("Sending prompt to OpenAI model {Model}", settingsModel.OpenAiModel);

        var completion = await chatClient
            .CompleteChatAsync(messages, options, cancellationToken)
            .ConfigureAwait(false);

        var text = completion.Value.Content[0].Text;
        _logger.LogDebug("Received {Length} chars from OpenAI", text?.Length ?? 0);
        return text ?? string.Empty;
    }
}
