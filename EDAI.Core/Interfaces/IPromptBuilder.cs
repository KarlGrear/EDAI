using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

/// <summary>
/// Assembles the user-facing portion of the OpenAI prompt from a <see cref="PipelineContext"/>.
/// The system persona is handled separately by <see cref="IOpenAIService"/>; this builds
/// only the user message: config prompt + triggering event JSON + secondary events + schema.
/// </summary>
public interface IPromptBuilder
{
    /// <summary>
    /// Builds the user message, stores it in <see cref="PipelineContext.BuiltPrompt"/>,
    /// and returns it. Mutates the context in place.
    /// </summary>
    string Build(PipelineContext context);
}
