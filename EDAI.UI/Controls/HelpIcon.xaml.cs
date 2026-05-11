using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace EDAI.UI.Controls;

public partial class HelpIcon : UserControl
{
    public static readonly DependencyProperty HelpTextProperty =
        DependencyProperty.Register(
            nameof(HelpText),
            typeof(string),
            typeof(HelpIcon),
            new PropertyMetadata(string.Empty, OnHelpTextChanged));

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
        if (d is HelpIcon icon)
            icon.HelpTextBlock.Text = (string)(e.NewValue ?? string.Empty);
    }

    private void TriggerButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HelpPopup.IsOpen && HelpPopup.Child is FrameworkElement popupRoot)
        {
            // Popup lives in a separate HwndHost visual tree.  DynamicResource bindings
            // on the Border handle background/border/foreground from Application resources,
            // but inherited font properties never cross the HwndHost boundary — push them
            // in explicitly each time the popup opens.
            TextElement.SetFontFamily(popupRoot, FontFamily);
            TextElement.SetFontSize(popupRoot, FontSize);
        }
        HelpPopup.IsOpen = !HelpPopup.IsOpen;
        e.Handled = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        HelpPopup.IsOpen = false;
        e.Handled = true;
    }
}
