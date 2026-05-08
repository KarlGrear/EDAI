using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace EDAI.UI.Services;

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services) => _services = services;

    public void ShowSettings()            => ShowWindow<Views.SettingsWindow>();
    public void ShowEventConfigurations() => ShowWindow<Views.EventConfigSelectionWindow>();
    public void ShowCategoryManagement()  => ShowWindow<Views.CategoryManagementWindow>();

    public void ShowEventConfigEdit(int? configId, Action? onClosed = null)
    {
        var window = _services.GetRequiredService<Views.EventConfigEditWindow>();
        window.Owner = Application.Current.MainWindow;
        if (onClosed != null)
            window.Closed += (_, _) => onClosed();
        _ = window.LoadAsync(configId);
        window.Show();
    }

    public void ShowTest(int? configId = null)
    {
        var window = _services.GetRequiredService<Views.TestWindow>();
        window.Owner = Application.Current.MainWindow;
        _ = window.LoadAsync(configId);
        window.Show();
    }

    private void ShowWindow<TWindow>() where TWindow : Window
    {
        var window = _services.GetRequiredService<TWindow>();
        window.Owner = Application.Current.MainWindow;
        window.Show();
    }
}
