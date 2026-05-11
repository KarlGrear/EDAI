using System.Windows;
using EDAI.UI.ViewModels;

namespace EDAI.UI.Views;

public partial class ThemeWindow : Window
{
    private readonly ThemeViewModel _viewModel;

    public ThemeWindow(ThemeViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await _viewModel.LoadAsync();
        _viewModel.CloseRequested += (_, _) => Close();
    }
}
