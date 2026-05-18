using System.Windows;
using ICSharpCode.AvalonEdit.Highlighting;
using EDAI.UI.Services;
using EDAI.UI.ViewModels;

namespace EDAI.UI.Views;

public partial class ScriptDesignerWindow : Window
{
    private readonly ScriptDesignerViewModel _viewModel;

    public ScriptDesignerWindow(ScriptDesignerViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
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

        ScriptEditor.Focus();
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
