using System.Windows;
using EDAI.UI.ViewModels;

namespace EDAI.UI.Views;

public partial class TestWindow : Window
{
    private readonly TestViewModel _viewModel;

    public TestWindow(TestViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public async Task LoadAsync(int? configId = null)
    {
        await _viewModel.LoadAsync(configId);
    }
}
