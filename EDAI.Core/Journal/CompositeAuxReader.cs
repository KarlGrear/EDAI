using EDAI.Core.Interfaces;

namespace EDAI.Core.Journal;

/// <summary>
/// Wraps <see cref="JournalAuxFileReader"/> and adds the "session" identifier,
/// which delegates to <see cref="ISessionService"/> rather than the journal directory.
/// Registered as the <see cref="IJournalAuxFileReader"/> singleton so all callers
/// (TemplateEngine, ConditionEvaluator, PromptBuilder, ResponseParser) transparently
/// gain |session.key| support without changes.
/// </summary>
public sealed class CompositeAuxReader : IJournalAuxFileReader
{
    private readonly JournalAuxFileReader _inner;
    private readonly ISessionService _session;

    public IReadOnlyList<string> KnownIdentifiers { get; }

    public CompositeAuxReader(JournalAuxFileReader inner, ISessionService session)
    {
        _inner  = inner;
        _session = session;
        KnownIdentifiers = inner.KnownIdentifiers.Append("session").OrderBy(k => k).ToList();
    }

    public string? Read(string identifier) =>
        string.Equals(identifier, "session", StringComparison.OrdinalIgnoreCase)
            ? _session.ReadJson()
            : _inner.Read(identifier);
}
