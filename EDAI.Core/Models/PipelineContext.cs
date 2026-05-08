namespace EDAI.Core.Models;

/// <summary>
/// Mutable bag of state passed through each stage of the AI pipeline for a single trigger.
/// Populated incrementally: the config and triggering event are set at creation,
/// <see cref="BuiltPrompt"/> is filled by the prompt builder, and
/// <see cref="RawAiResponse"/> / <see cref="ParsedResponse"/> are filled after the OpenAI call.
/// </summary>
public sealed class PipelineContext
{
    public required EventConfigurationModel Config { get; init; }
    public required ParsedJournalEvent TriggeringEvent { get; init; }
    public IReadOnlyList<ParsedJournalEvent> SecondaryEvents { get; init; } = [];
    public string? BuiltPrompt { get; set; }
    public string? RawAiResponse { get; set; }
    public AiResponse? ParsedResponse { get; set; }
}
