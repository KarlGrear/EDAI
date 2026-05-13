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
            // App is exiting via tray "Exit" — let the close proceed.
            base.OnClosing(e);
            return;
        }

        bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

        if (shiftHeld || !_viewModel.MinimizeToTray)
        {
            // Exit the app: allow the close, then trigger shutdown on the next dispatcher frame.
            base.OnClosing(e);
            Dispatcher.BeginInvoke(() => (Application.Current as App)?.BeginShutdown());
            return;
        }

        // Hide to tray instead of closing.
        e.Cancel = true;
        base.OnClosing(e);
        HideToTray();
    }

    private void HideToTray()
    {
        // RestoreBounds holds the last normal (non-minimized, non-maximized) size/position.
        bool wasMaximized = WindowState == WindowState.Maximized;
        double w, h, l, t;

        if (WindowState == WindowState.Normal)
        {
            w = ActualWidth;
            h = ActualHeight;
            l = Left;
            t = Top;
        }
        else // Minimized or Maximized — use restore bounds
        {
            w = RestoreBounds.Width  > 0 ? RestoreBounds.Width  : ActualWidth;
            h = RestoreBounds.Height > 0 ? RestoreBounds.Height : ActualHeight;
            l = RestoreBounds.Left;
            t = RestoreBounds.Top;
        }

        SaveWindowStateAsync(w, h, l, t, wasMaximized, Topmost);
        Hide();
    }

    private async void SaveWindowStateAsync(
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
