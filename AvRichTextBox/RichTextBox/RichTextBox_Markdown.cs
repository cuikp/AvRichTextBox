using Avalonia.Controls;
using Avalonia.Input.Platform;
using System.Text;

namespace AvRichTextBox;

public partial class RichTextBox
{
    public void LoadMarkdown(string markdown) => FlowDoc.LoadMarkdown(markdown);
    public string SaveMarkdown() => FlowDoc.SaveMarkdown();
    public void LoadMarkdownDoc(string fileName) => LoadMarkdown(File.ReadAllText(fileName, Encoding.UTF8));
    public void SaveMarkdownDoc(string fileName) => File.WriteAllText(fileName, SaveMarkdown(), new UTF8Encoding(false));

    /// <summary>Copies the selection as Markdown text. Returns false when copying is disabled or no clipboard is available.</summary>
    public async Task<bool> CopyMarkdownAsync()
    {
        if (DisableUserCopy || FlowDoc.Selection.Length == 0) return false;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return false;
        await clipboard.SetTextAsync(FlowDoc.SaveMarkdown(FlowDoc.Selection));
        return true;
    }

    /// <summary>Parses clipboard text as Markdown and replaces the current selection.</summary>
    public async Task<bool> PasteMarkdownAsync()
    {
        if (IsReadOnly) return false;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return false;
        var document = FlowDoc;
        using var range = new TextRange(document, document.Selection.Start, document.Selection.End);
        string? markdown = await clipboard.TryGetTextAsync();
        if (IsReadOnly || FlowDoc != document || !document.TextRanges.Contains(range) || string.IsNullOrEmpty(markdown)) return false;
        document.InsertMarkdown(markdown, range);
        UpdateCurrentParagraphLayout();
        return true;
    }
}
