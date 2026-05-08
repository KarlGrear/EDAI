namespace EDAI.Core.Models;

public sealed class JournalLineReceivedEventArgs : EventArgs
{
    public required JournalLine Line { get; init; }
}
