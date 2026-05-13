using System.Windows;
using EDAI.UI.Services;
using EDAI.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EDAI.UI.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await _viewModel.LoadAsync();
        _viewModel.CloseRequested += (_, _) => Close();
        _viewModel.ShowConfirmation = (message, title) =>
            MessageBox.Show(this, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
                == MessageBoxResult.Yes;
        _viewModel.OpenThemeRequested = () =>
            ((App)Application.Current).Services.GetRequiredService<INavigationService>().ShowTheme();
    }
}
