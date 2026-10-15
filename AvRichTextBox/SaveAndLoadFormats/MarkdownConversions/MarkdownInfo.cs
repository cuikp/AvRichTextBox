namespace AvRichTextBox;

// Immutable format metadata follows the existing block/run clones used by editing and undo.
internal sealed record MarkdownBlockInfo(
    int HeadingLevel = 0,
    int QuoteDepth = 0,
    int ListIndent = 0,
    bool IsCodeBlock = false,
    string CodeLanguage = "",
    bool IsThematicBreak = false,
    bool LooseList = false);

internal sealed record MarkdownInlineInfo(bool IsCode = false, bool IsImage = false, string Title = "");
