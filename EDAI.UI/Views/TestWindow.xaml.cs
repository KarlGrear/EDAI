using System.ComponentModel;
using System.Windows;
using EDAI.Core.Interfaces;
using EDAI.UI.ViewModels;

namespace EDAI.UI.Views;

public partial class TestWindow : Window
{
    private readonly TestViewModel _viewModel;
    private readonly ISettingsRepository _settingsRepo;

    public TestWindow(TestViewModel viewModel, ISettingsRepository settingsRepo)
    {
        _viewModel   = viewModel;
        _settingsRepo = settingsRepo;
        DataContext  = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public async Task LoadAsync(int? configId = null)
    {
        await _viewModel.LoadAsync(configId);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var settings = await _settingsRepo.GetAsync();

        Width  = settings.TestWindowWidth  > 0 ? settings.TestWindowWidth  : 900;
        Height = settings.TestWindowHeight > 0 ? settings.TestWindowHeight : 680;

        if (settings.TestWindowLeft.HasValue && settings.TestWindowTop.HasValue)
        {
            double left = settings.TestWindowLeft.Value;
            double top  = settings.TestWindowTop.Value;

            double vLeft   = SystemParameters.VirtualScreenLeft;
            double vTop    = SystemParameters.VirtualScreenTop;
            double vRight  = vLeft + SystemParameters.VirtualScreenWidth;
            double vBottom = vTop  + SystemParameters.VirtualScreenHeight;

            bool onScreen = left + 100 <= vRight  && left + Width  >= vLeft
                         && top  + 50  <= vBottom && top  + Height >= vTop;

            if (onScreen)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = left;
                Top  = top;
            }
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        SavePositionAsync();
    }

    private async void SavePositionAsync()
    {
        var settings = await _settingsRepo.GetAsync();
        settings.TestWindowLeft   = Left;
        settings.TestWindowTop    = Top;
        settings.TestWindowWidth  = ActualWidth;
        settings.TestWindowHeight = ActualHeight;
        await _settingsRepo.SaveAsync(settings);
    }
}
