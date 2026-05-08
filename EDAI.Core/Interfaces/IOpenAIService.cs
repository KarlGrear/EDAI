namespace EDAI.Core.Interfaces;

/// <summary>
/// Sends a prompt to OpenAI and returns the raw JSON response string.
/// Each call is stateless — no conversation history is maintained.
/// The model and API key are read from <see cref="ISettingsRepository"/> on every call
/// so that settings changes take effect immediately without restarting.
/// </summary>
public interface IOpenAIService
{
    /// <summary>
    /// Sends <paramref name="prompt"/> to the configured OpenAI model in JSON-object mode
    /// and returns the raw JSON string from the first completion choice.
    /// Returns <see cref="string.Empty"/> if the API key is not configured or the response is blank.
    /// </summary>
    Task<string> SendAsync(string prompt, CancellationToken cancellationToken = default);
}
