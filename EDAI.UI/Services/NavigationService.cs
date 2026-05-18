using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace EDAI.UI.Services;

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services) => _services = services;

    public void ShowSettings(Action? onClosed = null)
    {
        if (!TryGetNewWindow<Views.SettingsWindow>(out var w)) return;
        if (onClosed != null) w.Closed += (_, _) => onClosed();
        w.Show();
    }
    public void ShowTheme()               => ShowSingle<Views.ThemeWindow>();
    public void ShowAbout()               => ShowSingle<Views.AboutWindow>();
    public void ShowEventConfigurations() => ShowSingle<Views.EventConfigSelectionWindow>();

    public void ShowHistory()
    {
        if (!TryGetNewWindow<Views.HistoryWindow>(out var w)) return;
        _ = w.LoadAsync();
        w.Show();
    }

    public void ShowCategoryManagement(Action? onClosed = null)
    {
        if (!TryGetNewWindow<Views.CategoryManagementWindow>(out var w)) return;
        if (onClosed != null) w.Closed += (_, _) => onClosed();
        w.Show();
    }

    public void ShowEventConfigEdit(int? configId, Action? onClosed = null)
    {
        if (!TryGetNewWindow<Views.EventConfigEditWindow>(out var w)) return;
        if (onClosed != null) w.Closed += (_, _) => onClosed();
        _ = w.LoadAsync(configId);
        w.Show();
    }

    public void ShowTest(int? configId = null)
    {
        // No owner: the test window must stay visible when the main window is minimized
        // so the user can paste events and observe tray notifications in that state.
        if (!TryGetNewWindow<Views.TestWindow>(out var w, setOwner: false)) return;
        _ = w.LoadAsync(configId);
        w.Show();
    }

    public void ShowScriptDesigner()
    {
        if (!TryGetNewWindow<Views.ScriptDesignerWindow>(out var w, setOwner: false)) return;
        w.SetupStandalone();
        w.Show();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ShowSingle<TWindow>() where TWindow : Window
    {
        if (!TryGetNewWindow<TWindow>(out var w)) return;
        w.Show();
    }

    /// <summary>
    /// If a window of type <typeparamref name="TWindow"/> is already open, brings it
    /// to the foreground and returns <c>false</c> (caller should not open another).
    /// Otherwise resolves a new instance from DI, optionally sets the owner, applies
    /// the current font, and returns <c>true</c> so the caller can finish configuring
    /// and show it.
    /// </summary>
    private bool TryGetNewWindow<TWindow>(out TWindow window, bool setOwner = true) where TWindow : Window
    {
        var existing = Application.Current.Windows.OfType<TWindow>().FirstOrDefault();
        if (existing != null)
        {
            if (existing.WindowState == WindowState.Minimized)
                existing.WindowState = WindowState.Normal;
            existing.Activate();
            window = null!;
            return false;
        }

        window = _services.GetRequiredService<TWindow>();
        if (setOwner)
            window.Owner = Application.Current.MainWindow;
        App.ApplyFontToWindow(window);
        return true;
    }
}
