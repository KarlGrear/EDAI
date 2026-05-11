using System.Text;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;

namespace EDAI.Core.Pipeline;

/// <summary>
/// Builds the user message sent to OpenAI by concatenating the config prompt,
/// the triggering event JSON, any secondary event JSON, and the expected results schema.
/// <see cref="SystemPersona"/> is the fixed system prompt added by <see cref="IOpenAIService"/>.
/// </summary>
public sealed class PromptBuilder : IPromptBuilder
{
    private readonly IJournalAuxFileReader _auxReader;

    public PromptBuilder(IJournalAuxFileReader auxReader)
    {
        _auxReader = auxReader;
    }

    public const string SystemPersona =
        "You are EDAI, the onboard computer AI of an Elite Dangerous commander. " +
        "You have access to ship telemetry, navigation data, and galactic knowledge. " +
        "Respond only with a valid JSON object matching the schema provided. " +
        "Do not include explanation, markdown, or any text outside the JSON object.";

    public string Build(PipelineContext context)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(context.Config.Prompt))
        {
            var resolvedPrompt = TemplateEngine.Apply(
                context.Config.Prompt,
                context.TriggeringEvent.RawJson,
                resultJson: null,
                auxProvider: _auxReader.Read);
            sb.AppendLine(resolvedPrompt);
            sb.AppendLine();
        }

        if (context.Config.SendFullTriggerEvent)
        {
            sb.AppendLine("Triggering event:");
            sb.AppendLine(context.TriggeringEvent.RawJson);
        }

        if (context.SecondaryEvents.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Additional context events:");
            foreach (var e in context.SecondaryEvents)
                sb.AppendLine(e.RawJson);
        }

        if (!string.IsNullOrWhiteSpace(context.Config.ExpectedResultsSchema))
        {
            sb.AppendLine();
            sb.AppendLine("Respond with a JSON object matching exactly this schema:");
            sb.AppendLine(context.Config.ExpectedResultsSchema);
        }

        context.BuiltPrompt = sb.ToString().TrimEnd();
        return context.BuiltPrompt;
    }
}
