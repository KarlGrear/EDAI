using System.Windows;
using EDAI.Core.Interfaces;
using EDAI.UI.ViewModels;

namespace EDAI.UI.Views;

public partial class EventConfigEditWindow : Window
{
    private readonly EventConfigEditViewModel _viewModel;
    private readonly IScriptingService _scriptingService;
    private readonly ISessionService _sessionService;
    private readonly ISettingsRepository _settingsRepo;

    public EventConfigEditWindow(EventConfigEditViewModel viewModel, IScriptingService scriptingService, ISessionService sessionService, ISettingsRepository settingsRepo)
    {
        _viewModel        = viewModel;
        _scriptingService = scriptingService;
        _sessionService   = sessionService;
        _settingsRepo     = settingsRepo;
        DataContext       = viewModel;
        InitializeComponent();
        _viewModel.CloseRequested += (_, _) => Close();

        viewModel.OpenScriptDesigner = (isProcessScript, existingScript) =>
        {
            var designerVm = new ScriptDesignerViewModel(_scriptingService, _sessionService);
            designerVm.Setup(isProcessScript, existingScript);
            var window = new ScriptDesignerWindow(designerVm, _settingsRepo) { Owner = this };
            return window.ShowDialog() == true ? window.ResultScript : null;
        };
    }

    public async Task LoadAsync(int? configId = null)
    {
        await _viewModel.LoadAsync(configId);
    }
}
