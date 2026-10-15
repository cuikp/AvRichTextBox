using Avalonia.Input.Platform;
using System.Text;

namespace AvRichTextBox;

public partial class RichTextBox
{
    private static readonly DataFormat<byte[]> CustomContentClipboardFormat =
        DataFormat.CreateBytesPlatformFormat("AvRichTextBox Custom Content");

    private async Task<bool> PasteCustomContentAsync(IClipboard clipboard)
    {
        var document = FlowDoc;
        using var range = new TextRange(document, document.Selection.Start, document.Selection.End);
        byte[]? data = await clipboard.TryGetValueAsync(CustomContentClipboardFormat);
        if (data == null) return false;
        if (IsReadOnly || FlowDoc != document || !document.TextRanges.Contains(range)) return true;
        document.InsertCustomSelectionXaml(Encoding.UTF8.GetString(data), range);
        UpdateCurrentParagraphLayout();
        return true;
    }
}
