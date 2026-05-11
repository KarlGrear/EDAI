using System.ComponentModel;
using System.Windows;
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

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        // Capture all state synchronously while the window handle is still valid.
        bool maximized = WindowState == WindowState.Maximized;
        double w = maximized ? RestoreBounds.Width  : ActualWidth;
        double h = maximized ? RestoreBounds.Height : ActualHeight;
        double l = maximized ? RestoreBounds.Left   : Left;
        double t = maximized ? RestoreBounds.Top    : Top;
        bool alwaysOnTop = Topmost;

        SaveWindowStateAsync(w, h, l, t, maximized, alwaysOnTop);
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
