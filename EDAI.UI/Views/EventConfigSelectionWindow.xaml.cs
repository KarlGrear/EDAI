using System.Windows;
using EDAI.UI.ViewModels;

namespace EDAI.UI.Views;

public partial class EventConfigSelectionWindow : Window
{
    private readonly EventConfigSelectionViewModel _viewModel;

    public EventConfigSelectionWindow(EventConfigSelectionViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await _viewModel.LoadAsync();
    }
}
