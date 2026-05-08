using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace EDAI.UI.Controls;

/// <summary>
/// Small info-icon button that reveals a dismissible help bubble explaining a UI setting.
/// Bind <see cref="HelpText"/> to supply the explanation text shown inside the bubble.
/// </summary>
public partial class HelpIcon : UserControl
{
    /// <summary>Identifies the <see cref="HelpText"/> dependency property.</summary>
    public static readonly DependencyProperty HelpTextProperty =
        DependencyProperty.Register(
            nameof(HelpText),
            typeof(string),
            typeof(HelpIcon),
            new PropertyMetadata(string.Empty, OnHelpTextChanged));

    /// <summary>
    /// The explanation text displayed inside the help bubble when the icon is clicked.
    /// Supports multi-line text via standard newline characters.
    /// </summary>
    public string HelpText
    {
        get => (string)GetValue(HelpTextProperty);
        set => SetValue(HelpTextProperty, value);
    }

    public HelpIcon()
    {
        InitializeComponent();
    }

    private static void OnHelpTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Mirror the DP value to the TextBlock directly so the popup content
        // stays in sync even when the popup hasn't been opened yet.
        if (d is HelpIcon icon)
            icon.HelpTextBlock.Text = (string)(e.NewValue ?? string.Empty);
    }

    private void TriggerButton_Click(object sender, RoutedEventArgs e)
    {
        HelpPopup.IsOpen = !HelpPopup.IsOpen;
        e.Handled = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        HelpPopup.IsOpen = false;
        e.Handled = true;
    }
}
