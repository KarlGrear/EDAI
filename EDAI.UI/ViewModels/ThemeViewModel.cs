using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Color = System.Windows.Media.Color;
using CommunityToolkit.Mvvm.Input;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;
using EDAI.UI.Services;

namespace EDAI.UI.ViewModels;

public sealed partial class ThemeViewModel : ObservableObject
{
    private readonly ISettingsRepository _repo;

    private SettingsModel _original = new();
    private SettingsModel _preview  = new();

    public ObservableCollection<string> Elements { get; } =
    [
        "Accent Color",
        "App Background",
        "App Text",
        "Toolbar Background",
        "Toolbar Text",
        "Button Color",
        "Button Text",
        "Event Background",
        "Control Border",
        "Control Hover",
        "Code: Comment",
        "Code: String / Char",
        "Code: Keyword",
        "Code: Type Keyword",
        "Code: Context Keyword",
        "Code: Modifier",
        "Code: Method / Function",
        "Code: Number",
        "Code: Preprocessor",
        "Code: Variable (Plain Text)",
        "Code: Line Numbers",
        "Code: Bracket Highlight",
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCustomColor))]
    [NotifyPropertyChangedFor(nameof(IsSyntaxElement))]
    [NotifyPropertyChangedFor(nameof(PreviewTabIndex))]
    private string _selectedElement = "Accent Color";

    [ObservableProperty] private Color _currentColor = Color.FromRgb(0xFF, 0x6D, 0x00);

    public bool HasCustomColor => GetCurrentElementHex(_preview) != null || SelectedElement == "Accent Color";
    public bool IsSyntaxElement => SelectedElement.StartsWith("Code:");
    public int  PreviewTabIndex => IsSyntaxElement ? 1 : 0;

    public event EventHandler? CloseRequested;
    // Fired when a syntax color changes so the code preview editor can redraw
    public event EventHandler? SyntaxColorUpdated;

    public ThemeViewModel(ISettingsRepository repo)
    {
        _repo = repo;
    }

    public async Task LoadAsync()
    {
        _original = await _repo.GetAsync();
        _preview  = CopySettings(_original);
        SyncPickerFromElement();
    }

    partial void OnSelectedElementChanged(string value)
    {
        SyncPickerFromElement();
        OnPropertyChanged(nameof(HasCustomColor));
    }

    partial void OnCurrentColorChanged(Color value)
    {
        if (_preview == null) return;
        SetCurrentElementHex(_preview, ColorToHex(value));
        App.ApplyAppearance(_preview);
        OnPropertyChanged(nameof(HasCustomColor));

        if (IsSyntaxElement)
            SyntaxColorUpdated?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ResetElement()
    {
        if (SelectedElement == "Accent Color")
            _preview.PrimaryColor = "#FF6D00";
        else
            SetCurrentElementHex(_preview, null);

        App.ApplyAppearance(_preview);
        SyncPickerFromElement();
        OnPropertyChanged(nameof(HasCustomColor));

        if (IsSyntaxElement)
            SyntaxColorUpdated?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task Save()
    {
        await _repo.SaveAsync(_preview);
        _original = CopySettings(_preview);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        App.ApplyAppearance(_original);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SyncPickerFromElement()
    {
        var hex = GetCurrentElementHex(_preview);

        if (hex == null)
        {
            hex = SelectedElement switch
            {
                "Accent Color"             => _preview.PrimaryColor,
                "App Background"           => "#121212",
                "App Text"                 => "#FFFFFF",
                "Toolbar Background"       => _preview.PrimaryColor,
                "Toolbar Text"             => "#FFFFFF",
                "Button Color"             => _preview.PrimaryColor,
                "Button Text"             => "#FFFFFF",
                "Event Background"         => "#1E1E1E",
                "Control Border"           => "#606060",
                "Control Hover"            => "#909090",
                "Code: Comment"               => ScriptEditorHighlighting.DefaultComment,
                "Code: String / Char"         => ScriptEditorHighlighting.DefaultString,
                "Code: Keyword"               => ScriptEditorHighlighting.DefaultKeyword,
                "Code: Type Keyword"          => ScriptEditorHighlighting.DefaultTypeKeyword,
                "Code: Context Keyword"       => ScriptEditorHighlighting.DefaultContextKeyword,
                "Code: Modifier"              => ScriptEditorHighlighting.DefaultModifier,
                "Code: Method / Function"     => ScriptEditorHighlighting.DefaultMethod,
                "Code: Number"                => ScriptEditorHighlighting.DefaultNumber,
                "Code: Preprocessor"          => ScriptEditorHighlighting.DefaultPreprocessor,
                "Code: Variable (Plain Text)" => ScriptEditorHighlighting.DefaultIdentifier,
                "Code: Line Numbers"          => ScriptEditorHighlighting.DefaultLineNumber,
                "Code: Bracket Highlight"     => ScriptEditorHighlighting.DefaultBracketMatch,
                _                          => "#FF6D00",
            };
        }

        CurrentColor = HexToColor(hex) ?? Color.FromRgb(0xFF, 0x6D, 0x00);
    }

    private string? GetCurrentElementHex(SettingsModel s) => SelectedElement switch
    {
        "Accent Color"                => null,
        "App Background"              => s.CustomBackgroundColor,
        "App Text"                    => s.CustomForegroundColor,
        "Toolbar Background"          => s.ToolbarBackground,
        "Toolbar Text"                => s.ToolbarForeground,
        "Button Color"                => s.ButtonBackground,
        "Button Text"                 => s.ButtonForeground,
        "Event Background"            => s.ControlBackground,
        "Control Border"              => s.ControlBorderColor,
        "Control Hover"               => s.ControlHoverBackground,
        "Code: Comment"               => s.SyntaxComment,
        "Code: String / Char"         => s.SyntaxString,
        "Code: Keyword"               => s.SyntaxKeyword,
        "Code: Type Keyword"          => s.SyntaxTypeKeyword,
        "Code: Context Keyword"       => s.SyntaxContextKeyword,
        "Code: Modifier"              => s.SyntaxModifier,
        "Code: Method / Function"     => s.SyntaxMethod,
        "Code: Number"                => s.SyntaxNumber,
        "Code: Preprocessor"          => s.SyntaxPreprocessor,
        "Code: Variable (Plain Text)" => s.SyntaxIdentifier,
        "Code: Line Numbers"          => s.SyntaxLineNumber,
        "Code: Bracket Highlight"     => s.SyntaxBracketMatch,
        _                             => null,
    };

    private void SetCurrentElementHex(SettingsModel s, string? hex)
    {
        switch (SelectedElement)
        {
            case "Accent Color":                s.PrimaryColor           = hex ?? "#FF6D00"; break;
            case "App Background":              s.CustomBackgroundColor  = hex; break;
            case "App Text":                    s.CustomForegroundColor  = hex; break;
            case "Toolbar Background":          s.ToolbarBackground      = hex; break;
            case "Toolbar Text":                s.ToolbarForeground      = hex; break;
            case "Button Color":                s.ButtonBackground       = hex; break;
            case "Button Text":                 s.ButtonForeground       = hex; break;
            case "Event Background":            s.ControlBackground      = hex; break;
            case "Control Border":              s.ControlBorderColor     = hex; break;
            case "Control Hover":               s.ControlHoverBackground = hex; break;
            case "Code: Comment":               s.SyntaxComment        = hex; break;
            case "Code: String / Char":         s.SyntaxString         = hex; break;
            case "Code: Keyword":               s.SyntaxKeyword        = hex; break;
            case "Code: Type Keyword":          s.SyntaxTypeKeyword    = hex; break;
            case "Code: Context Keyword":       s.SyntaxContextKeyword = hex; break;
            case "Code: Modifier":              s.SyntaxModifier       = hex; break;
            case "Code: Method / Function":     s.SyntaxMethod         = hex; break;
            case "Code: Number":                s.SyntaxNumber         = hex; break;
            case "Code: Preprocessor":          s.SyntaxPreprocessor   = hex; break;
            case "Code: Variable (Plain Text)": s.SyntaxIdentifier     = hex; break;
            case "Code: Line Numbers":          s.SyntaxLineNumber     = hex; break;
            case "Code: Bracket Highlight":     s.SyntaxBracketMatch   = hex; break;
        }
    }

    private static SettingsModel CopySettings(SettingsModel src) => new()
    {
        OpenAiApiKey             = src.OpenAiApiKey,
        OpenAiModel              = src.OpenAiModel,
        TtsVoiceName             = src.TtsVoiceName,
        TtsEnabled               = src.TtsEnabled,
        AlwaysOnTop              = src.AlwaysOnTop,
        ShowSplashScreen         = src.ShowSplashScreen,
        TrayNotificationsEnabled = src.TrayNotificationsEnabled,
        Theme                    = src.Theme,
        PrimaryColor             = src.PrimaryColor,
        CustomBackgroundColor    = src.CustomBackgroundColor,
        CustomForegroundColor    = src.CustomForegroundColor,
        ToolbarBackground        = src.ToolbarBackground,
        ToolbarForeground        = src.ToolbarForeground,
        ButtonBackground         = src.ButtonBackground,
        ButtonForeground         = src.ButtonForeground,
        ControlBackground        = src.ControlBackground,
        ControlHoverBackground   = src.ControlHoverBackground,
        ControlBorderColor       = src.ControlBorderColor,
        FontFamily               = src.FontFamily,
        FontSize                 = src.FontSize,
        WindowWidth              = src.WindowWidth,
        WindowHeight             = src.WindowHeight,
        WindowLeft               = src.WindowLeft,
        WindowTop                = src.WindowTop,
        IsMaximized              = src.IsMaximized,
        ScriptingAllowFileSystem       = src.ScriptingAllowFileSystem,
        ScriptingAllowNetwork          = src.ScriptingAllowNetwork,
        ScriptingAllowProcessExecution = src.ScriptingAllowProcessExecution,
        ScriptingAllowReflection       = src.ScriptingAllowReflection,
        SyntaxComment        = src.SyntaxComment,
        SyntaxString         = src.SyntaxString,
        SyntaxKeyword        = src.SyntaxKeyword,
        SyntaxTypeKeyword    = src.SyntaxTypeKeyword,
        SyntaxContextKeyword = src.SyntaxContextKeyword,
        SyntaxModifier       = src.SyntaxModifier,
        SyntaxMethod         = src.SyntaxMethod,
        SyntaxNumber         = src.SyntaxNumber,
        SyntaxPreprocessor   = src.SyntaxPreprocessor,
        SyntaxIdentifier     = src.SyntaxIdentifier,
        SyntaxLineNumber     = src.SyntaxLineNumber,
        SyntaxBracketMatch   = src.SyntaxBracketMatch,
    };

    private static string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private static Color? HexToColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        try
        {
            return (Color)System.Windows.Media.ColorConverter.ConvertFromString(hex)!;
        }
        catch { return null; }
    }
}
