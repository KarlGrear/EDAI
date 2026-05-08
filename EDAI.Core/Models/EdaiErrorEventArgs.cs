namespace EDAI.Core.Models;

public sealed class EdaiErrorEventArgs : EventArgs
{
    public required string Source { get; init; }
    public required string Message { get; init; }
    public Exception? Exception { get; init; }
}
