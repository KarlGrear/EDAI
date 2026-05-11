namespace EDAI.UI.Services;

/// <summary>
/// Abstracts Win32 file open/save dialogs so ViewModels stay free of WPF types.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Opens a Save File dialog. Returns the chosen path, or <c>null</c> if the
    /// user cancelled.
    /// </summary>
    string? SaveFile(string title, string defaultFileName, string filter);

    /// <summary>
    /// Opens an Open File dialog. Returns the chosen path, or <c>null</c> if the
    /// user cancelled.
    /// </summary>
    string? OpenFile(string title, string filter);
}
