using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Color = System.Windows.Media.Color;
using CommunityToolkit.Mvvm.Input;
using EDAI.Core.Interfaces;
using EDAI.Core.Models;

namespace EDAI.UI.ViewModels;

public sealed partial class ThemeViewModel : ObservableObject
{
    private readonly ISettingsRepository _repo;

    // Snapshot of settings when the window opened — used to revert on cancel
    private SettingsModel _original = new();
    // Live copy mutated for preview
    private SettingsModel _preview = new();

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
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCustomColor))]
    private string _selectedElement = "Accent Color";

    [ObservableProperty] private Color _currentColor = Color.FromRgb(0xFF, 0x6D, 0x00);

    // True when the selected element has a custom override (Reset is meaningful)
    public bool HasCustomColor => GetCurrentElementHex(_preview) != null || SelectedElement == "Accent Color";

    public event EventHandler? CloseRequested;

    public ThemeViewModel(ISettingsRepository repo)
    {
        _repo = repo;
    }

    public async Task LoadAsync()
    {
        _original = await _repo.GetAsync();
        // Deep-copy into preview
        _preview = CopySettings(_original);
        SyncPickerFromElement();
    }

    // When the element dropdown changes, pull the color for that element into the picker
    partial void OnSelectedElementChanged(string value)
    {
        SyncPickerFromElement();
        OnPropertyChanged(nameof(HasCustomColor));
    }

    // When the picker changes, apply immediately as a live preview
    partial void OnCurrentColorChanged(Color value)
    {
        if (_preview == null) return;
        SetCurrentElementHex(_preview, ColorToHex(value));
        App.ApplyAppearance(_preview);
        OnPropertyChanged(nameof(HasCustomColor));
    }

    [RelayCommand]
    private void ResetElement()
    {
        if (SelectedElement == "Accent Color")
        {
            // Reset accent to default EDAI orange
            _preview.PrimaryColor = "#FF6D00";
        }
        else
        {
            SetCurrentElementHex(_preview, null);
        }
        App.ApplyAppearance(_preview);
        SyncPickerFromElement();
        OnPropertyChanged(nameof(HasCustomColor));
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
        // Revert to original appearance
        App.ApplyAppearance(_original);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SyncPickerFromElement()
    {
        var hex = SelectedElement switch
        {
            "Accent Color"       => _preview.PrimaryColor,
            "App Background"     => _preview.CustomBackgroundColor,
            "App Text"           => _preview.CustomForegroundColor,
            "Toolbar Background" => _preview.ToolbarBackground,
            "Toolbar Text"       => _preview.ToolbarForeground,
            "Button Color"       => _preview.ButtonBackground,
            "Button Text"        => _preview.ButtonForeground,
            "Event Background"   => _preview.ControlBackground,
            "Control Border"     => _preview.ControlBorderColor,
            "Control Hover"      => _preview.ControlHoverBackground,
            _                    => null,
        };

        // Fallback to a representative color when no custom override is set
        if (hex == null)
        {
            hex = SelectedElement switch
            {
                "Accent Color"       => _preview.PrimaryColor,
                "App Background"     => "#121212",
                "App Text"           => "#FFFFFF",
                "Toolbar Background" => _preview.PrimaryColor,
                "Toolbar Text"       => "#FFFFFF",
                "Button Color"       => _preview.PrimaryColor,
                "Button Text"        => "#FFFFFF",
                "Event Background"   => "#1E1E1E",
                "Control Border"     => "#606060",
                "Control Hover"      => "#909090",
                _                    => "#FF6D00",
            };
        }

        CurrentColor = HexToColor(hex) ?? Color.FromRgb(0xFF, 0x6D, 0x00);
    }

    private string? GetCurrentElementHex(SettingsModel s) => SelectedElement switch
    {
        "Accent Color"       => null,   // accent is always "set"
        "App Background"     => s.CustomBackgroundColor,
        "App Text"           => s.CustomForegroundColor,
        "Toolbar Background" => s.ToolbarBackground,
        "Toolbar Text"       => s.ToolbarForeground,
        "Button Color"       => s.ButtonBackground,
        "Button Text"        => s.ButtonForeground,
        "Event Background"   => s.ControlBackground,
        "Control Border"     => s.ControlBorderColor,
        "Control Hover"      => s.ControlHoverBackground,
        _                    => null,
    };

    private void SetCurrentElementHex(SettingsModel s, string? hex)
    {
        switch (SelectedElement)
        {
            case "Accent Color":        s.PrimaryColor            = hex ?? "#FF6D00"; break;
            case "App Background":      s.CustomBackgroundColor   = hex; break;
            case "App Text":            s.CustomForegroundColor   = hex; break;
            case "Toolbar Background":  s.ToolbarBackground       = hex; break;
            case "Toolbar Text":        s.ToolbarForeground       = hex; break;
            case "Button Color":        s.ButtonBackground        = hex; break;
            case "Button Text":         s.ButtonForeground        = hex; break;
            case "Event Background":    s.ControlBackground       = hex; break;
            case "Control Border":      s.ControlBorderColor      = hex; break;
            case "Control Hover":       s.ControlHoverBackground  = hex; break;
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
    };

    private static string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private static Color? HexToColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        try
        {
            var c = (System.Windows.Media.Color)
                System.Windows.Media.ColorConverter.ConvertFromString(hex)!;
            return c;
        }
        catch { return null; }
    }
}
