using System.Windows;
using EDAI.UI.ViewModels;

namespace EDAI.UI.Views;

public partial class EventConfigEditWindow : Window
{
    private readonly EventConfigEditViewModel _viewModel;

    public EventConfigEditWindow(EventConfigEditViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        _viewModel.CloseRequested += (_, _) => Close();
    }

    public async Task LoadAsync(int? configId = null)
    {
        await _viewModel.LoadAsync(configId);
    }
}
