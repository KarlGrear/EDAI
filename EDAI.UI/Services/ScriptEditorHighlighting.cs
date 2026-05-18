using System.Windows;
using System.Windows.Media;
using EDAI.Core.Models;
using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using ColorConverter = System.Windows.Media.ColorConverter;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

namespace EDAI.UI.Services;

/// <summary>
/// Applies configurable syntax highlight colors to every AvalonEdit C# editor instance.
/// Patches the shared definition (affects all instances) and provides per-editor
/// application for line numbers, bracket match background, and default foreground.
/// </summary>
public static class ScriptEditorHighlighting
{
    // VS Code Dark+ defaults
    public const string DefaultComment        = "#6A9955";
    public const string DefaultString         = "#CE9178";
    public const string DefaultKeyword        = "#569CD6";
    public const string DefaultTypeKeyword    = "#4EC9B0";
    public const string DefaultContextKeyword = "#9CDCFE";
    public const string DefaultModifier       = "#569CD6";
    public const string DefaultMethod         = "#DCDCAA";
    public const string DefaultNumber         = "#B5CEA8";
    public const string DefaultPreprocessor   = "#C586C0";
    public const string DefaultIdentifier     = "#D4D4D4";
    public const string DefaultLineNumber     = "#858585";
    public const string DefaultBracketMatch   = "#3A3D41";

    private static string _comment        = DefaultComment;
    private static string _string         = DefaultString;
    private static string _keyword        = DefaultKeyword;
    private static string _typeKeyword    = DefaultTypeKeyword;
    private static string _contextKeyword = DefaultContextKeyword;
    private static string _modifier       = DefaultModifier;
    private static string _method         = DefaultMethod;
    private static string _number         = DefaultNumber;
    private static string _preprocessor   = DefaultPreprocessor;
    private static string _identifier     = DefaultIdentifier;
    private static string _lineNumber     = DefaultLineNumber;
    private static string _bracketMatch   = DefaultBracketMatch;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads syntax colors from settings and patches the shared C# highlighting
    /// definition. Call from App.ApplyAppearance and ThemeViewModel live preview.
    /// </summary>
    public static void Apply(SettingsModel settings)
    {
        _comment        = settings.SyntaxComment        ?? DefaultComment;
        _string         = settings.SyntaxString         ?? DefaultString;
        _keyword        = settings.SyntaxKeyword        ?? DefaultKeyword;
        _typeKeyword    = settings.SyntaxTypeKeyword    ?? DefaultTypeKeyword;
        _contextKeyword = settings.SyntaxContextKeyword ?? DefaultContextKeyword;
        _modifier       = settings.SyntaxModifier       ?? DefaultModifier;
        _method         = settings.SyntaxMethod         ?? DefaultMethod;
        _number         = settings.SyntaxNumber         ?? DefaultNumber;
        _preprocessor   = settings.SyntaxPreprocessor   ?? DefaultPreprocessor;
        _identifier     = settings.SyntaxIdentifier     ?? DefaultIdentifier;
        _lineNumber     = settings.SyntaxLineNumber     ?? DefaultLineNumber;
        _bracketMatch   = settings.SyntaxBracketMatch   ?? DefaultBracketMatch;

        PatchDefinition();
    }

    /// <summary>
    /// Applies per-editor colors (line numbers, default foreground, bracket match)
    /// to a specific TextEditor instance. Call once per editor after Apply().
    /// </summary>
    public static void ApplyToEditor(TextEditor editor)
    {
        editor.LineNumbersForeground = Brush(_lineNumber);
        editor.Foreground            = Brush(_identifier);

        // Wire bracket matching if not already done
        EnsureBracketHighlighter(editor);
    }

    // ── Shared definition patching ────────────────────────────────────────────

    private static void PatchDefinition()
    {
        var def = HighlightingManager.Instance.GetDefinition("C#");
        if (def == null) return;

        // All named colors in CSharp-Mode.xshd — map each to the closest theme bucket.
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Basic tokens
            ["Comment"]              = _comment,
            ["String"]               = _string,
            ["Char"]                 = _string,
            ["Preprocessor"]         = _preprocessor,
            ["NumberLiteral"]        = _number,
            // Method calls — built-in MethodCall rule already exists in the XSHD
            ["MethodCall"]           = _method,
            // String interpolation {expr} — built-in StringInterpolation span in the XSHD
            ["StringInterpolation"]  = _identifier,
            // Access + member modifiers — XSHD has Visibility (public/private/…) and Modifiers (static/readonly/…)
            ["Visibility"]           = _modifier,
            ["Modifiers"]            = _modifier,
            // Built-in type names
            ["ValueTypeKeywords"]    = _typeKeyword,
            ["ReferenceTypeKeywords"]= _typeKeyword,
            // Context / soft keywords
            ["ContextKeywords"]      = _contextKeyword,
            // Everything else folds into the general keyword color
            ["Keywords"]             = _keyword,
            ["NamespaceKeywords"]    = _keyword,
            ["TrueFalse"]            = _keyword,
            ["TypeKeywords"]         = _keyword,
            ["GotoKeywords"]         = _keyword,
            ["ExceptionKeywords"]    = _keyword,
            ["CheckedKeyword"]       = _keyword,
            ["UnsafeKeywords"]       = _keyword,
            ["OperatorKeywords"]     = _keyword,
            ["ParameterModifiers"]   = _keyword,
            ["GetSetAddRemove"]      = _keyword,
            ["NullOrValueKeywords"]  = _keyword,
            ["ThisOrBaseReference"]  = _keyword,
            ["SemanticKeywords"]     = _keyword,
        };

        foreach (var color in def.NamedHighlightingColors)
        {
            if (map.TryGetValue(color.Name, out var hex))
            {
                color.Foreground = new SimpleHighlightingBrush(ParseColor(hex));
                color.FontWeight = null;
            }
        }
    }

    // ── Per-editor bracket highlighting ───────────────────────────────────────

    private static void EnsureBracketHighlighter(TextEditor editor)
    {
        var area = editor.TextArea;
        if (area.TextView.BackgroundRenderers.OfType<BracketMatchRenderer>().Any())
            return;

        var renderer = new BracketMatchRenderer();
        area.TextView.BackgroundRenderers.Add(renderer);

        area.Caret.PositionChanged += (_, _) =>
        {
            int offset = area.Caret.Offset;
            var doc    = editor.Document;
            if (doc == null) { renderer.Clear(); return; }

            var (a, b) = FindMatchingBrackets(doc.Text, offset);
            var brush  = new WpfSolidColorBrush(ParseColor(_bracketMatch)) { Opacity = 0.6 };
            renderer.SetHighlight(a, b, brush);
            area.TextView.Redraw();
        };
    }

    private static (int a, int b) FindMatchingBrackets(string text, int caretOffset)
    {
        if (text.Length == 0) return (-1, -1);

        // Check the character at and just before the caret
        foreach (int pos in new[] { caretOffset, caretOffset - 1 })
        {
            if (pos < 0 || pos >= text.Length) continue;
            char ch = text[pos];
            if (!IsBracket(ch)) continue;

            int match = FindMatch(text, pos, ch);
            if (match >= 0) return (pos, match);
        }

        return (-1, -1);
    }

    private static bool IsBracket(char c) => c is '(' or ')' or '{' or '}' or '[' or ']';

    private static int FindMatch(string text, int pos, char open)
    {
        char close;
        int dir;
        switch (open)
        {
            case '(': close = ')'; dir =  1; break;
            case ')': close = '('; dir = -1; break;
            case '{': close = '}'; dir =  1; break;
            case '}': close = '{'; dir = -1; break;
            case '[': close = ']'; dir =  1; break;
            case ']': close = '['; dir = -1; break;
            default:  return -1;
        }

        int depth = 1;
        int i = pos + dir;
        while (i >= 0 && i < text.Length)
        {
            if (text[i] == open)  depth++;
            else if (text[i] == close) { if (--depth == 0) return i; }
            i += dir;
        }
        return -1;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WpfColor ParseColor(string hex)
    {
        try { return (WpfColor)ColorConverter.ConvertFromString(hex)!; }
        catch { return WpfColors.White; }
    }

    private static WpfSolidColorBrush Brush(string hex) => new(ParseColor(hex));

    // ── Bracket match renderer ────────────────────────────────────────────────

    private sealed class BracketMatchRenderer : IBackgroundRenderer
    {
        private int _posA = -1, _posB = -1;
        private WpfBrush? _brush;

        public KnownLayer Layer => KnownLayer.Background;

        public void SetHighlight(int a, int b, WpfBrush brush)
        {
            _posA = a; _posB = b; _brush = brush;
        }

        public void Clear() => (_posA, _posB) = (-1, -1);

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_brush == null || (_posA < 0 && _posB < 0)) return;
            DrawAt(textView, drawingContext, _posA);
            DrawAt(textView, drawingContext, _posB);
        }

        private void DrawAt(TextView textView, DrawingContext ctx, int offset)
        {
            if (offset < 0 || offset >= textView.Document.TextLength) return;
            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(
                         textView, new ICSharpCode.AvalonEdit.Document.TextSegment { StartOffset = offset, Length = 1 }))
            {
                ctx.DrawRectangle(_brush, null, rect);
            }
        }
    }
}
