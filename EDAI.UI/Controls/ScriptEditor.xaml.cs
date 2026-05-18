using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace EDAI.UI.Controls;

public partial class ScriptEditor : UserControl
{
    private CompletionWindow? _completionWindow;
    private bool _updatingText;

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(ScriptEditor),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnTextPropertyChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (ScriptEditor)d;
        if (ctrl._updatingText) return;
        ctrl._updatingText = true;
        ctrl.Editor.Text = (string)(e.NewValue ?? string.Empty);
        ctrl._updatingText = false;
    }

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(ScriptEditor),
            new PropertyMetadata(false, (d, e) => ((ScriptEditor)d).Editor.IsReadOnly = (bool)e.NewValue));

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public static readonly DependencyProperty CompletionTokensProperty =
        DependencyProperty.Register(
            nameof(CompletionTokens),
            typeof(IReadOnlyList<string>),
            typeof(ScriptEditor),
            new PropertyMetadata(null));

    public IReadOnlyList<string>? CompletionTokens
    {
        get => (IReadOnlyList<string>?)GetValue(CompletionTokensProperty);
        set => SetValue(CompletionTokensProperty, value);
    }

    public static readonly DependencyProperty ShowLineNumbersProperty =
        DependencyProperty.Register(
            nameof(ShowLineNumbers),
            typeof(bool),
            typeof(ScriptEditor),
            new PropertyMetadata(false, (d, e) => ((ScriptEditor)d).Editor.ShowLineNumbers = (bool)e.NewValue));

    public bool ShowLineNumbers
    {
        get => (bool)GetValue(ShowLineNumbersProperty);
        set => SetValue(ShowLineNumbersProperty, value);
    }

    public ScriptEditor()
    {
        InitializeComponent();
        Editor.TextArea.TextEntered += OnTextEntered;
        Editor.TextChanged += OnEditorTextChanged;
        Editor.TextArea.TextView.LineTransformers.Add(new EdaiTokenHighlighter());
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_updatingText) return;
        _updatingText = true;
        Text = Editor.Text;
        _updatingText = false;
    }

    private void OnTextEntered(object sender, TextCompositionEventArgs e)
    {
        if (e.Text != "|") return;

        if (_completionWindow != null)
        {
            _completionWindow.Close();
            return;
        }

        _completionWindow = new CompletionWindow(Editor.TextArea)
        {
            StartOffset = Editor.TextArea.Caret.Offset
        };
        foreach (var item in TokenCompletionProvider.Build(CompletionTokens))
            _completionWindow.CompletionList.CompletionData.Add(item);

        _completionWindow.Show();
        _completionWindow.Closed += (_, _) => _completionWindow = null;
    }
}
