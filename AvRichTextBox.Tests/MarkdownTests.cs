using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System.Text;
using Xunit;

namespace AvRichTextBox.Tests;

public class MarkdownTests
{
    private static FlowDocument Load(string markdown)
    {
        var document = new FlowDocument();
        document.LoadMarkdown(markdown);
        return document;
    }

    private static string Text(Paragraph paragraph) => string.Concat(paragraph.Inlines.Select(inline => inline.InlineText));
    private static IEnumerable<EditableRun> Runs(FlowDocument document) => document.AllParagraphs.SelectMany(paragraph => paragraph.Inlines).OfType<EditableRun>();

    [AvaloniaFact]
    public void LoadMarkdown_ImportsHeadingsAndNestedInlineFormatting()
    {
        var document = Load("# Heading\n\nPlain **bold and *italic*** with ~~removed~~ and `a * b`.\n");
        var paragraphs = document.Blocks.Cast<Paragraph>().ToList();
        Assert.Equal(2, paragraphs.Count);
        Assert.Equal("Heading", Text(paragraphs[0]));
        Assert.True(paragraphs[0].FontSize > paragraphs[1].FontSize);
        Assert.Equal("Plain bold and italic with removed and a * b.", Text(paragraphs[1]));
        Assert.Contains(Runs(document), run => run.InlineText == "italic" && run.FontWeight == FontWeight.Bold && run.FontStyle == FontStyle.Italic);
        Assert.Contains(Runs(document), run => run.InlineText == "removed" && run.TextDecorations!.Any(decoration => decoration.Location == TextDecorationLocation.Strikethrough));
        Assert.Contains(Runs(document), run => run.InlineText == "a * b" && run.MarkdownInfo?.IsCode == true);
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Headings_KeepTheirLevelAfterEditingAndRoundTrip(int level)
    {
        using var host = new EditorHost();
        host.Editor.LoadMarkdown(new string('#', level) + " Title");
        EditorHost.Pump();
        host.Select(0, 5);
        host.Type("Edited");
        string saved = host.Editor.SaveMarkdown();
        Assert.StartsWith(new string('#', level) + " ", saved);
        Assert.Contains("Edited", saved);
        Assert.Equal(level, Assert.Single(Load(saved).Blocks).MarkdownInfo!.HeadingLevel);
    }

    [AvaloniaFact]
    public void ListsAndQuotes_KeepNestedStructureAndNumbering()
    {
        const string markdown = "> 3. first\n>    - nested\n> 4. second\n\nOutside";
        var document = Load(markdown);
        string saved = document.SaveMarkdown();
        var reloaded = Load(saved);
        Assert.Equal(document.AllParagraphs.Select(Text), reloaded.AllParagraphs.Select(Text));
        Assert.Equal(document.AllParagraphs.Select(paragraph => paragraph.ListMarker), reloaded.AllParagraphs.Select(paragraph => paragraph.ListMarker));
        Assert.Equal(document.AllParagraphs.Select(paragraph => paragraph.MarkdownInfo!.QuoteDepth), reloaded.AllParagraphs.Select(paragraph => paragraph.MarkdownInfo!.QuoteDepth));
        Assert.Contains(document.AllParagraphs, paragraph => paragraph.ListMarker == "3.");
        Assert.Contains(document.AllParagraphs, paragraph => paragraph.ListMarker == "4.");
        Assert.Contains("    - nested", saved);
    }

    [AvaloniaFact]
    public void CodeBlocksAndCodeSpans_PreserveBackticksAndWhitespace()
    {
        const string markdown = "````csharp\nvar text = \"```\";\n\n````\n\nUse `` `literal` `` and `  spaces  `.";
        var document = Load(markdown);
        string saved = document.SaveMarkdown();
        var reloaded = Load(saved);
        Assert.Equal(document.AllParagraphs.Select(Text), reloaded.AllParagraphs.Select(Text));
        Assert.StartsWith("````csharp\n", saved);
        Assert.Equal("csharp", reloaded.AllParagraphs[0].MarkdownInfo!.CodeLanguage);
        Assert.Equal(Runs(document).Select(run => run.MarkdownInfo?.IsCode), Runs(reloaded).Select(run => run.MarkdownInfo?.IsCode));
    }

    [AvaloniaFact]
    public void LinksAndImageReferences_PreserveTextDestinationAndTitle()
    {
        var document = Load("[a **bold** link](https://example.com/a_(b) \"A title\") ![picture](images/photo.png \"Image title\")");
        var reloaded = Load(document.SaveMarkdown());
        var links = Runs(reloaded).OfType<EditableHyperlink>().ToList();
        Assert.Contains(links, link => link.NavigateUri == "https://example.com/a_(b)" && link.MarkdownInfo!.Title == "A title");
        Assert.Contains(links, link => link.NavigateUri == "images/photo.png" && link.LinkDisplayText == "picture" && link.MarkdownInfo!.IsImage && link.MarkdownInfo.Title == "Image title");
        Assert.Equal(document.Text, reloaded.Text);
    }

    [AvaloniaFact]
    public void Table_ImportsCellsAlignmentAndEscapedPipes()
    {
        const string markdown = "| Name | Value |\n| :--- | ---: |\n| A \\| B | **42** |";
        var document = Load(markdown);
        var table = Assert.Single(document.Blocks.OfType<Table>());
        Assert.Equal(2, table.ColDefs.Count);
        Assert.Equal(2, table.RowDefs.Count);
        Assert.Equal("A | B", Text(table.Cells.Single(cell => cell.RowNo == 1 && cell.ColNo == 0).CellBlocks.Cast<Paragraph>().Single()));
        Assert.Equal(TextAlignment.Right, table.Cells.Single(cell => cell.RowNo == 1 && cell.ColNo == 1).CellBlocks.Cast<Paragraph>().Single().TextAlignment);
        var reloaded = Load(document.SaveMarkdown());
        Assert.Equal(document.Text, reloaded.Text);
        Assert.Equal(4, Assert.Single(reloaded.Blocks.OfType<Table>()).Cells.Count);
    }

    [AvaloniaFact]
    public void LiteralMarkupUnicodeAndLineBreaks_SurviveRoundTrip()
    {
        const string markdown = "Escaped \\*stars\\* and \\[brackets\\] &amp; <tag> \u03B1 \u2211 \U0001F642.\nsoft line  \nhard line";
        var document = Load(markdown);
        Assert.Contains("*stars* and [brackets] & <tag> \u03B1 \u2211 \U0001F642. soft line", document.Text);
        Assert.Contains(document.AllParagraphs[0].Inlines, inline => inline is EditableLineBreak);
        Assert.Equal(document.Text, Load(document.SaveMarkdown()).Text);
    }

    [AvaloniaFact]
    public void LoadMarkdown_EmptyInputReplacesOldContentAndRemainsEditable()
    {
        using var host = new EditorHost();
        host.Editor.LoadMarkdown("old");
        host.Editor.LoadMarkdown("");
        EditorHost.Pump();
        Assert.Equal("", host.Editor.SaveMarkdown());
        host.Type("new");
        Assert.Equal("new", host.Editor.SaveMarkdown());
    }

    [AvaloniaFact]
    public void LoadMarkdown_NullInputLeavesDocumentUnchangedAndRestoresUndoState()
    {
        var document = Load("**keep**");
        Assert.Throws<ArgumentNullException>(() => document.LoadMarkdown((string)null!));
        Assert.Equal("**keep**", document.SaveMarkdown());
        Assert.False(StaticProperties.DisableUndoStack);
    }

    [AvaloniaFact]
    public void StreamMethods_UseUtf8FromCurrentPositionAndLeaveStreamsOpen()
    {
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("skip\u03B1\u2211\U0001F642 **bold**"));
        input.Position = 4;
        var document = new FlowDocument();
        document.LoadMarkdown(input);
        Assert.True(input.CanRead);
        using var output = new MemoryStream();
        output.WriteByte(42);
        document.SaveMarkdown(output);
        Assert.True(output.CanWrite);
        Assert.Equal("*\u03B1\u2211\U0001F642 **bold**", Encoding.UTF8.GetString(output.ToArray()));
    }

    [AvaloniaFact]
    public void InsertMarkdown_ReplacesSelectionAndSupportsRepeatedUndoRedo()
    {
        using var host = new EditorHost();
        host.Editor.LoadMarkdown("before old after");
        EditorHost.Pump();
        host.Select(7, 3);
        host.Document.InsertMarkdown("**new**\n\nsecond");
        EditorHost.Pump();
        string inserted = host.Editor.SaveMarkdown();
        Assert.Contains("before **new**", inserted);
        Assert.Contains("second after", inserted);
        for (int i = 0; i < 3; i++)
        {
            host.Press(Key.Z, RawInputModifiers.Control);
            Assert.Equal("before old after", host.Editor.SaveMarkdown());
            host.Press(Key.Y, RawInputModifiers.Control);
            Assert.Equal(inserted, host.Editor.SaveMarkdown());
        }
    }

    [AvaloniaTheory]
    [InlineData(0, 0)]
    [InlineData(0, 3)]
    [InlineData(3, 0)]
    public void InsertMarkdown_HandlesParagraphEdges(int start, int length)
    {
        using var host = new EditorHost();
        host.Editor.LoadMarkdown("old");
        EditorHost.Pump();
        host.Select(start, length);
        host.Document.InsertMarkdown("**new**");
        EditorHost.Pump();
        string expected = "old"[..start] + "**new**" + "old"[(start + length)..];
        Assert.Equal(expected, host.Editor.SaveMarkdown());
        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Equal("old", host.Editor.SaveMarkdown());
    }

    [AvaloniaFact]
    public void InsertMarkdown_InTableCellKeepsOtherCellsAndCanBeUndone()
    {
        using var host = new EditorHost();
        host.Editor.LoadMarkdown("| A | B |\n|---|---|\n| left | right |");
        EditorHost.Pump();
        var target = host.Document.AllParagraphs.Single(paragraph => Text(paragraph) == "left");
        string original = host.Editor.SaveMarkdown();
        host.Select(target.StartInDoc, 4);
        host.Document.InsertMarkdown("**changed**\n\nextra");
        EditorHost.Pump();
        Assert.Contains("right", host.Editor.SaveMarkdown());
        Assert.Contains("changed", host.Editor.SaveMarkdown());
        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Equal(original, host.Editor.SaveMarkdown());
        host.Press(Key.Y, RawInputModifiers.Control);
        Assert.Contains("extra", host.Editor.SaveMarkdown());
    }

    [AvaloniaFact]
    public void InsertMarkdown_AcrossCellsRejectsBeforeChangingDocument()
    {
        var document = Load("| A | B |\n|---|---|");
        string original = document.SaveMarkdown();
        document.SelectHeadless(0, document.DocEndPoint);
        Assert.Throws<NotSupportedException>(() => document.InsertMarkdown("replacement"));
        Assert.Equal(original, document.SaveMarkdown());
        Assert.False(StaticProperties.DisableUndoStack);
    }

    [AvaloniaFact]
    public void TextRangeMarkdown_SaveClipsTextAndLoadReplacesOnlyTheRange()
    {
        using var host = new EditorHost();
        host.Editor.LoadMarkdown("before **selected** after");
        EditorHost.Pump();
        using var range = new TextRange(host.Document, 7, 15);
        using var output = new MemoryStream();
        range.Save(output, ContentDataFormat.Markdown);
        Assert.Equal("**selected**", Encoding.UTF8.GetString(output.ToArray()));
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("*replacement*"));
        range.Load(input, ContentDataFormat.Markdown);
        EditorHost.Pump();
        Assert.Equal("before *replacement* after", host.Editor.SaveMarkdown());
        Assert.True(input.CanRead);
    }

    [AvaloniaFact]
    public async Task ClipboardMarkdown_CopiesSelectionAndPastesFormattedText()
    {
        using var host = new EditorHost();
        host.Editor.LoadMarkdown("before **selected** after");
        EditorHost.Pump();
        host.Select(7, 8);
        Assert.True(await host.Editor.CopyMarkdownAsync());
        Assert.Equal("**selected**", await host.Window.Clipboard!.TryGetTextAsync());
        host.Select(0, host.Document.DocEndPoint);
        Assert.True(await host.Editor.PasteMarkdownAsync());
        EditorHost.Pump();
        Assert.Equal("**selected**", host.Editor.SaveMarkdown());
        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Equal("before **selected** after", host.Editor.SaveMarkdown());
    }

    [AvaloniaFact]
    public async Task ClipboardMarkdown_RespectsCopyAndReadOnlyGuards()
    {
        using var host = new EditorHost();
        host.Editor.LoadMarkdown("keep");
        EditorHost.Pump();
        await host.Window.Clipboard!.SetTextAsync("**other**");
        host.Select(0, 4);
        host.Editor.DisableUserCopy = true;
        Assert.False(await host.Editor.CopyMarkdownAsync());
        Assert.Equal("**other**", await host.Window.Clipboard!.TryGetTextAsync());
        host.Editor.IsReadOnly = true;
        Assert.False(await host.Editor.PasteMarkdownAsync());
        Assert.Equal("keep", host.Editor.SaveMarkdown());
    }

    [AvaloniaTheory]
    [InlineData("- first\n\n  second paragraph\n\n- next")]
    [InlineData("> outer\n>\n> > inner\n>\n> end")]
    [InlineData("before\n\n---\n\nafter")]
    [InlineData("    code * literal\n    next line")]
    [InlineData("Text &copy; and &#x1F642;.")]
    [InlineData("![](image.png) and [](https://example.com)")]
    [InlineData("![A *formatted* image](image.png \"Image\")")]
    [InlineData("***both*** **bold** *italic* ~~removed~~")]
    [InlineData("literal \\* \\_ \\` \\\\ and a@b.example")]
    public void SupportedSyntax_RoundTripsWithoutLosingTextOrBlockStructure(string markdown)
    {
        var first = Load(markdown);
        var second = Load(first.SaveMarkdown());
        Assert.Equal(first.AllParagraphs.Select(Text), second.AllParagraphs.Select(Text));
        Assert.Equal(first.AllParagraphs.Select(paragraph => paragraph.ListMarker), second.AllParagraphs.Select(paragraph => paragraph.ListMarker));
        Assert.Equal(first.Blocks.Select(block => block.GetType()), second.Blocks.Select(block => block.GetType()));
        Assert.Equal(first.Blocks.Select(block => block.MarkdownInfo), second.Blocks.Select(block => block.MarkdownInfo));
        Assert.Equal(Runs(first).OfType<EditableHyperlink>().Select(link => link.NavigateUri), Runs(second).OfType<EditableHyperlink>().Select(link => link.NavigateUri));
    }

    [AvaloniaFact]
    public void InsertMarkdown_OverATableRestoresTheTableAndItsEditableCellsOnUndo()
    {
        using var host = new EditorHost();
        host.Editor.LoadMarkdown("before\n\n| A | B |\n|---|---|\n| left | right |\n\nafter");
        EditorHost.Pump();
        string original = host.Editor.SaveMarkdown();
        host.Select(0, host.Document.DocEndPoint);
        host.Document.InsertMarkdown("replacement");
        EditorHost.Pump();
        Assert.Empty(host.Document.Blocks.OfType<Table>());
        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Equal(original, host.Editor.SaveMarkdown());
        var cell = host.Document.AllParagraphs.Single(paragraph => Text(paragraph) == "left");
        host.Select(cell.StartInDoc, 4);
        host.Document.InsertMarkdown("edited");
        EditorHost.Pump();
        Assert.Contains("edited", host.Editor.SaveMarkdown());
        Assert.DoesNotContain("left", host.Editor.SaveMarkdown());
    }

    [AvaloniaTheory]
    [InlineData("    indented text")]
    [InlineData("trailing spaces  ")]
    [InlineData("    ")]
    [InlineData("# heading *literal* [brackets] 1. list")]
    [InlineData("1. not a list")]
    [InlineData("2) not a list")]
    [InlineData("- not a list")]
    [InlineData("+ not a list")]
    [InlineData("> not a quote")]
    [InlineData("---")]
    [InlineData("===")]
    [InlineData("#hashtag and # sign")]
    [InlineData("a_b _c_ *d* ~~e~~ `f` <g> [h] &amp; |i|")]
    [InlineData("C:\\Users\\name and <https://example.com>")]
    public void SaveMarkdown_ExportsLiteralEditorTextWithoutIntroducingMarkup(string text)
    {
        var document = Load("");
        var paragraph = Assert.Single(document.Blocks.OfType<Paragraph>());
        paragraph.Inlines.Clear();
        paragraph.Inlines.Add(new EditableRun(text));
        var reloaded = Load(document.SaveMarkdown());
        var result = Assert.IsType<Paragraph>(Assert.Single(reloaded.Blocks));
        Assert.Equal(text, Text(result));
        Assert.Equal(new MarkdownBlockInfo(), result.MarkdownInfo);
        Assert.False(result.HasListMarker);
        Assert.Empty(result.Inlines.OfType<EditableHyperlink>());
        Assert.All(result.Inlines.OfType<EditableRun>(), run => Assert.Equal(FontWeight.Normal, run.FontWeight));
    }

    // Block markers after a hard line break start a line as well.
    [AvaloniaFact]
    public void SaveMarkdown_EscapesBlockMarkersAfterLineBreaks()
    {
        var document = Load("");
        var paragraph = Assert.Single(document.Blocks.OfType<Paragraph>());
        paragraph.Inlines.Clear();
        paragraph.Inlines.Add(new EditableRun("first"));
        paragraph.Inlines.Add(new EditableLineBreak());
        paragraph.Inlines.Add(new EditableRun("1. second"));
        paragraph.Inlines.Add(new EditableLineBreak());
        paragraph.Inlines.Add(new EditableRun("---"));
        var reloaded = Load(document.SaveMarkdown());
        var result = Assert.IsType<Paragraph>(Assert.Single(reloaded.Blocks));
        Assert.Equal(Text(paragraph), Text(result));
        Assert.False(result.HasListMarker);
    }

    [AvaloniaFact]
    public void SaveMarkdown_LeavesOrdinaryPunctuationUnescaped()
    {
        const string text = "Version 1.2 - see a > b, C# notes (draft), \"quotes\", a+b=c, snake_case and AT&T!";
        var document = Load("");
        var paragraph = Assert.Single(document.Blocks.OfType<Paragraph>());
        paragraph.Inlines.Clear();
        paragraph.Inlines.Add(new EditableRun(text));

        Assert.Equal(text, document.SaveMarkdown());
    }

    [AvaloniaFact]
    public void SaveMarkdown_HeadingsAndTableHeadersHaveNoBoldMarkers()
    {
        const string markdown = "# Title\n\n| A | B |\n| --- | --- |\n| 1 | 2 |";

        Assert.Equal(markdown, Load(markdown).SaveMarkdown());
    }

    [AvaloniaFact]
    public void SaveMarkdown_HeadingEndingWithHash_KeepsTheHash()
    {
        var reloaded = Load(Load("# Issue \\#").SaveMarkdown());

        Assert.Equal("Issue #", Text(Assert.Single(reloaded.AllParagraphs)));
    }

    [AvaloniaTheory]
    [InlineData("- a\n- b\n    - c\n- d\n\n1. x\n2. y", "- a\n- b\n  - c\n- d\n\n1. x\n2. y")]
    [InlineData("- a\n\n- b", "- a\n\n- b")]
    [InlineData("1. one\n2. two\n\nText\n\n4. four", "1. one\n2. two\n\nText\n\n4. four")]
    public void SaveMarkdown_TightListsHaveNoBlankLinesAndLooseListsKeepThem(string expected, string markdown)
    {
        Assert.Equal(expected, Load(markdown).SaveMarkdown());
    }

    // Avalonia does not resolve the CSS generic name "monospace"; equal widths for narrow and wide letters show that
    // a monospaced font was found.
    [AvaloniaFact]
    public void Code_UsesAMonospacedFont()
    {
        var document = Load("Text `code` here\n\n```\nblock\n```");

        foreach (var run in new[] { Runs(document).First(run => run.Text == "code"), Runs(document).First(run => run.Text!.StartsWith("block")) })
        {
            var typeface = new Typeface(run.FontFamily);
            double narrow = new TextLayout("iiiiiiiiii", typeface, 16, Brushes.Black).Width;
            double wide = new TextLayout("WWWWWWWWWW", typeface, 16, Brushes.Black).Width;
            Assert.Equal(narrow, wide, 1);
        }
    }
}
