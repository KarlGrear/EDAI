using System.ComponentModel;
using System.Windows;
using ICSharpCode.AvalonEdit.Highlighting;
using EDAI.Core.Interfaces;
using EDAI.UI.Services;
using EDAI.UI.ViewModels;

namespace EDAI.UI.Views;

public partial class ScriptDesignerWindow : Window
{
    private readonly ScriptDesignerViewModel _viewModel;
    private readonly ISettingsRepository _settingsRepo;

    public ScriptDesignerWindow(ScriptDesignerViewModel viewModel, ISettingsRepository settingsRepo)
    {
        _viewModel    = viewModel;
        _settingsRepo = settingsRepo;
        DataContext   = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ScriptEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
        ScriptEditorHighlighting.ApplyToEditor(ScriptEditor);

        ScriptEditor.Text = _viewModel.ScriptText;

        ScriptEditor.TextChanged += (_, _) => _viewModel.ScriptText = ScriptEditor.Text;

        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ScriptDesignerViewModel.ScriptText)
                && ScriptEditor.Text != _viewModel.ScriptText)
            {
                ScriptEditor.Text = _viewModel.ScriptText;
            }
        };

        var settings = await _settingsRepo.GetAsync();

        Width  = settings.ScriptDesignerWidth  > 0 ? settings.ScriptDesignerWidth  : 960;
        Height = settings.ScriptDesignerHeight > 0 ? settings.ScriptDesignerHeight : 700;

        if (settings.ScriptDesignerLeft.HasValue && settings.ScriptDesignerTop.HasValue)
        {
            double left = settings.ScriptDesignerLeft.Value;
            double top  = settings.ScriptDesignerTop.Value;

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

        ScriptEditor.Focus();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        SavePositionAsync();
    }

    private async void SavePositionAsync()
    {
        var settings = await _settingsRepo.GetAsync();
        settings.ScriptDesignerLeft   = Left;
        settings.ScriptDesignerTop    = Top;
        settings.ScriptDesignerWidth  = ActualWidth;
        settings.ScriptDesignerHeight = ActualHeight;
        await _settingsRepo.SaveAsync(settings);
    }

    /// <summary>
    /// Prepares the window for standalone (non-dialog) use from the main toolbar.
    /// Must be called before Show() — not ShowDialog().
    /// </summary>
    public void SetupStandalone() => _viewModel.SetupStandalone();

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsStandalone)
            Close();
        else
            DialogResult = true;
    }

    /// <summary>Returns the script text if OK was clicked; null if cancelled.</summary>
    public string? ResultScript => DialogResult == true ? _viewModel.ScriptText : null;
}
