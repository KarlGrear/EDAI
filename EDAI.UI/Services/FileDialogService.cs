using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace EDAI.UI.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? SaveFile(string title, string defaultFileName, string filter)
    {
        var dlg = new SaveFileDialog
        {
            Title      = title,
            FileName   = defaultFileName,
            Filter     = filter,
            DefaultExt = ".edai.json",
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? OpenFile(string title, string filter)
    {
        var dlg = new OpenFileDialog
        {
            Title  = title,
            Filter = filter,
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? BrowseFolder(string title, string? initialPath)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description            = title,
            UseDescriptionForTitle = true,
            SelectedPath           = initialPath ?? string.Empty,
            ShowNewFolderButton    = true,
        };
        return dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? dlg.SelectedPath
            : null;
    }
}
