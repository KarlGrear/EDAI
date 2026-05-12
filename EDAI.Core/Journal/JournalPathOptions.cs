using EDAI.Core.Models;

namespace EDAI.Core.Journal;

/// <summary>
/// Mutable singleton that carries the journal directory path from settings into
/// services that need it. Set by App.xaml.cs after settings are loaded but before
/// the journal watcher is started.
/// </summary>
public sealed class JournalPathOptions
{
    public string Path { get; set; } = SettingsModel.DefaultJournalPath;
}
