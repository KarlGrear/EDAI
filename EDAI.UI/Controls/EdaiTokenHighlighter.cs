using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace EDAI.UI.Controls;

public sealed class EdaiTokenHighlighter : DocumentColorizingTransformer
{
    private static readonly SolidColorBrush CompleteBrush;
    private static readonly SolidColorBrush OpenBrush;

    static EdaiTokenHighlighter()
    {
        CompleteBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4E, 0xC9, 0xB0));
        CompleteBrush.Freeze();
        OpenBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8A, 0xD0, 0xBF));
        OpenBrush.Freeze();
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        var text = CurrentContext.Document.GetText(line);
        int i = 0;
        while (i < text.Length)
        {
            int start = text.IndexOf('|', i);
            if (start < 0) break;
            int end = text.IndexOf('|', start + 1);
            if (end >= 0)
            {
                int startOffset = line.Offset + start;
                int endOffset   = line.Offset + end + 1;
                ChangeLinePart(startOffset, endOffset, element =>
                {
                    element.TextRunProperties.SetForegroundBrush(CompleteBrush);
                    var face = element.TextRunProperties.Typeface;
                    element.TextRunProperties.SetTypeface(new Typeface(
                        face.FontFamily, face.Style, FontWeights.Bold, face.Stretch));
                });
                i = end + 1;
            }
            else
            {
                ChangeLinePart(line.Offset + start, line.Offset + text.Length, element =>
                    element.TextRunProperties.SetForegroundBrush(OpenBrush));
                break;
            }
        }
    }
}
