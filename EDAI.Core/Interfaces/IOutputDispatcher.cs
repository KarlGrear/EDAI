using EDAI.Core.Models;

namespace EDAI.Core.Interfaces;

/// <summary>
/// Final stage of the AI pipeline. Dispatches a completed <see cref="PipelineContext"/>
/// to all output channels: the UI (via event), TTS, and the response log in SQLite.
/// </summary>
public interface IOutputDispatcher
{
    /// <summary>
    /// Raised on a background thread whenever an AI response is ready for display.
    /// Subscribers (e.g. ViewModels, tray service) must marshal to the UI thread themselves.
    /// </summary>
    event EventHandler<AiResponseReceivedEventArgs> ResponseReceived;

    /// <summary>
    /// Dispatches the pipeline result: raises <see cref="ResponseReceived"/>,
    /// enqueues TTS speech, and persists a <see cref="ResponseLogModel"/> record.
    /// </summary>
    Task DispatchAsync(PipelineContext context, CancellationToken cancellationToken = default);
}
