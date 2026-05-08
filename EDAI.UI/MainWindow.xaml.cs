using System.ComponentModel;
using System.Windows;
using EDAI.Core.Interfaces;
using EDAI.UI.ViewModels;

namespace EDAI.UI;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ISettingsRepository _settingsRepo;
    private bool _isRealClose;

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
        if (!_isRealClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        SaveWindowState();
        base.OnClosing(e);
    }

    public void RealClose()
    {
        _isRealClose = true;
        Close();
    }

    private async void SaveWindowState()
    {
        var settings = await _settingsRepo.GetAsync();
        settings.WindowWidth = ActualWidth;
        settings.WindowHeight = ActualHeight;
        settings.WindowLeft = Left;
        settings.WindowTop = Top;
        settings.AlwaysOnTop = Topmost;
        await _settingsRepo.SaveAsync(settings);
    }
}
