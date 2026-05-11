namespace EDAI.Core.Interfaces;

/// <summary>
/// Wraps Windows Text-to-Speech (<see cref="System.Speech.Synthesis.SpeechSynthesizer"/>)
/// with a background queue so TTS never blocks the UI or pipeline threads.
/// </summary>
public interface ITtsService
{
    /// <summary>
    /// Global enabled flag. When <c>false</c>, calls to <see cref="Enqueue"/> are silently
    /// ignored. Can be toggled at runtime without restarting.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>Returns the names of all SAPI voices installed on this machine.</summary>
    IReadOnlyList<string> GetAvailableVoices();

    /// <summary>
    /// Selects the voice used for speech. If the name is not found, logs a warning
    /// and leaves the current voice unchanged.
    /// </summary>
    void SetVoice(string voiceName);

    /// <summary>
    /// Adds <paramref name="text"/> to the TTS queue. Returns immediately.
    /// No-op when <see cref="IsEnabled"/> is <c>false</c> or text is blank.
    /// </summary>
    void Enqueue(string text);

    /// <summary>
    /// Enqueues <paramref name="text"/> and returns a <see cref="Task"/> that completes
    /// only after that specific utterance has finished being spoken. The task respects
    /// <paramref name="ct"/> — if cancelled, the utterance is still spoken (speech cannot
    /// be interrupted mid-word) but the task transitions to cancelled so the caller
    /// is unblocked.
    /// No-op (returns completed task) when <see cref="IsEnabled"/> is <c>false</c> or text is blank.
    /// </summary>
    Task SpeakAndWaitAsync(string text, CancellationToken ct = default);
}
