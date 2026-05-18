using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace EDAI.UI.Controls;

public static class TokenCompletionProvider
{
    private static readonly string[] StaticPrefixes =
    [
        "trigger.", "result.", "secondary[0].", "secondary[1].", "secondary[2].",
        "status.", "market.", "navroute.", "modulesinfo.", "outfitting.",
        "shiplocker.", "shipyard.", "session.", "count(", "count(secondary)"
    ];

    public static IList<ICompletionData> Build(IReadOnlyList<string>? dynamicTokens)
    {
        var items = new List<ICompletionData>();
        foreach (var prefix in StaticPrefixes)
            items.Add(new TokenCompletionItem(prefix));
        if (dynamicTokens != null)
            foreach (var token in dynamicTokens)
                if (!Array.Exists(StaticPrefixes, p => p == token))
                    items.Add(new TokenCompletionItem(token));
        return items;
    }

    private sealed class TokenCompletionItem : ICompletionData
    {
        public TokenCompletionItem(string text) => Text = text;

        public System.Windows.Media.ImageSource? Image => null;
        public string Text { get; }
        public object Content => Text;
        public object Description => $"Insert |{Text}";
        public double Priority => 0;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            => textArea.Document.Replace(completionSegment, Text);
    }
}
