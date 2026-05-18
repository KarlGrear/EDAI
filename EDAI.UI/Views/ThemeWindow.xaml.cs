using System.Windows;
using ICSharpCode.AvalonEdit.Highlighting;
using EDAI.UI.Services;
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
        Loaded += OnLoaded;
        _viewModel.CloseRequested     += (_, _) => Close();
        _viewModel.SyntaxColorUpdated += (_, _) => RefreshCodePreview();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
        SetupCodePreview();
    }

    private void RefreshCodePreview()
    {
        // Re-apply per-editor colors (foreground, line numbers)
        ScriptEditorHighlighting.ApplyToEditor(CodePreviewEditor);
        // Cycle the definition to flush AvalonEdit's per-line highlight cache
        var def = CodePreviewEditor.SyntaxHighlighting;
        CodePreviewEditor.SyntaxHighlighting = null;
        CodePreviewEditor.SyntaxHighlighting = def;
    }

    private void SetupCodePreview()
    {
        CodePreviewEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
        ScriptEditorHighlighting.ApplyToEditor(CodePreviewEditor);

        CodePreviewEditor.Text =
            """
            // Navigate to the next waypoint in route
            using System;
            using System.Linq;

            #region FlightComputer

            public class FlightComputer
            {
                private int    _jumpCount   = 0;
                private string _destination = "Colonia";

                public bool PlanJump(string starSystem)
                {
                    if (starSystem == null)
                        return false;

                    var remaining = NavRoute.GetRemainingJumps();
                    Console.WriteLine($"Jumping to {starSystem}");
                    _jumpCount += 1;
                    return remaining > 0;
                }

                public string FormatStatus()
                {
                    return $"Jumps: {_jumpCount} / Destination: {_destination}";
                }
            }

            #endregion
            """;
    }
}
