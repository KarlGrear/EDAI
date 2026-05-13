using System.Windows;
using EDAI.UI.ViewModels;

namespace EDAI.UI.Views;

public partial class HistoryWindow : Window
{
    private readonly HistoryViewModel _vm;

    public HistoryWindow(HistoryViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        Closed += (_, _) => _vm.Dispose();
    }

    public Task LoadAsync() => _vm.LoadAsync();
}
