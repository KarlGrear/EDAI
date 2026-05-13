using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using EDAI.Core.Interfaces;
using EDAI.UI.ViewModels;

namespace EDAI.UI;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ISettingsRepository _settingsRepo;

    public MainWindow(MainWindowViewModel viewModel, ISettingsRepository settingsRepo)
    {
        _viewModel = viewModel;
        _settingsRepo = settingsRepo;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadSettingsAsync();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized && _viewModel.MinimizeToTray)
            HideToTray();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (Application.Current is App { IsShuttingDown: true })
        {
            base.OnClosing(e);
            return;
        }

        bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

        if (shiftHeld || !_viewModel.MinimizeToTray)
        {
            // Cancel the close so the window stays alive while we save state,
            // then call BeginShutdown which will re-enter OnClosing with IsShuttingDown=true.
            e.Cancel = true;
            base.OnClosing(e);
            _ = SaveThenExitAsync();
            return;
        }

        // Hide to tray instead of closing.
        e.Cancel = true;
        base.OnClosing(e);
        HideToTray();
    }

    private async Task SaveThenExitAsync()
    {
        CaptureWindowState(out var w, out var h, out var l, out var t, out var maximized);
        await SaveWindowStateAsync(w, h, l, t, maximized, Topmost);
        (Application.Current as App)?.BeginShutdown();
    }

    private void HideToTray()
    {
        CaptureWindowState(out var w, out var h, out var l, out var t, out var wasMaximized);
        _ = SaveWindowStateAsync(w, h, l, t, wasMaximized, Topmost);
        Hide();
    }

    private void CaptureWindowState(
        out double w, out double h, out double l, out double t, out bool wasMaximized)
    {
        wasMaximized = WindowState == WindowState.Maximized;

        if (WindowState == WindowState.Normal)
        {
            w = ActualWidth;
            h = ActualHeight;
            l = Left;
            t = Top;
        }
        else // Minimized or Maximized — RestoreBounds holds last normal size/position
        {
            w = RestoreBounds.Width  > 0 ? RestoreBounds.Width  : ActualWidth;
            h = RestoreBounds.Height > 0 ? RestoreBounds.Height : ActualHeight;
            l = RestoreBounds.Left;
            t = RestoreBounds.Top;
        }
    }

    private async Task SaveWindowStateAsync(
        double width, double height, double left, double top,
        bool isMaximized, bool alwaysOnTop)
    {
        var settings = await _settingsRepo.GetAsync();
        settings.WindowWidth  = width;
        settings.WindowHeight = height;
        settings.WindowLeft   = left;
        settings.WindowTop    = top;
        settings.IsMaximized  = isMaximized;
        settings.AlwaysOnTop  = alwaysOnTop;
        await _settingsRepo.SaveAsync(settings);
    }
}
