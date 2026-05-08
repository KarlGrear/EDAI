using System.Windows;
using EDAI.UI.ViewModels;

namespace EDAI.UI.Views;

public partial class CategoryManagementWindow : Window
{
    private readonly CategoryManagementViewModel _viewModel;

    public CategoryManagementWindow(CategoryManagementViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await _viewModel.LoadAsync();
    }
}
