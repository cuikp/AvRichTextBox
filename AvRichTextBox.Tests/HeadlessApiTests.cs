using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace AvRichTextBox.Tests;

public class HeadlessApiTests
{
    private static FlowDocument Headless(string rtf)
    {
        FlowDocument document = new();
        document.LoadRtfHeadless(rtf);
        return document;
    }

    private static List<Paragraph> Paragraphs(FlowDocument document) => [.. document.Blocks.OfType<Paragraph>()];

    private static string TextOf(Paragraph paragraph) => paragraph.Text.TrimEnd('\r');

    [AvaloniaFact]
    public void AppendRtfToRtf_WithoutNewLine_ContinuesTheLastParagraph()
    {
        string rtf = FlowDocument.AppendRtfToRtf(@"{\rtf1\pard abc}", @"{\rtf1\pard\b def}", insertNewLine: false);

        FlowDocument result = Headless(rtf);
        Assert.Equal(["abcdef"], Paragraphs(result).Select(TextOf).Where(text => text != ""));
        Assert.Equal(FontWeight.Bold, result.Blocks.OfType<Paragraph>().SelectMany(p => p.Inlines).OfType<EditableRun>().Single(run => run.Text == "def").FontWeight);
    }

    [AvaloniaFact]
    public void AppendRtfToRtf_WithNewLine_StartsExactlyOneNewParagraph()
    {
        string rtf = FlowDocument.AppendRtfToRtf(@"{\rtf1\pard abc}", @"{\rtf1\pard def}", insertNewLine: true);

        Assert.Equal(["abc", "def"], Paragraphs(Headless(rtf)).Select(TextOf));
    }

    [AvaloniaFact]
    public void AppendRtfToRtf_ToAnEmptyBase_ReturnsTheAppendedText()
    {
        string rtf = FlowDocument.AppendRtfToRtf("", @"{\rtf1\pard def}");

        Assert.Equal(["def"], Paragraphs(Headless(rtf)).Select(TextOf));
    }

    [AvaloniaFact]
    public void ReplaceSelectionWithText_WithoutControl_ReplacesAndKeepsTheReplacedFormat()
    {
        FlowDocument document = Headless(@"{\rtf1\pard\b bold \b0 old\b  bold}");

        document.SelectHeadless(5, 3);
        bool replaced = document.ReplaceSelectionWithText("new");

        Assert.True(replaced);
        FlowDocument reloaded = Headless(document.SaveRtfHeadless());
        Assert.Equal("bold new bold", TextOf(Paragraphs(reloaded)[0]));
        Assert.Equal(FontWeight.Normal, reloaded.Blocks.OfType<Paragraph>().SelectMany(p => p.Inlines).OfType<EditableRun>().Single(run => run.Text == "new").FontWeight);
    }

    [AvaloniaFact]
    public void ApplyBlackOnWhite_OverridesAThemeTextColor()
    {
        var editor = new RichTextBox();
        var window = new Window { Width = 600, Height = 400, Content = editor };
        // A theme that colors every TextBlock including derived types (":is(TextBlock)"): the document paragraphs
        // derive from TextBlock and would take it.
        window.Styles.Add(new Style(x => x.Is<TextBlock>()) { Setters = { new Setter(TextBlock.ForegroundProperty, Brushes.Red) } });
        window.Show();
        editor.LoadRtf(@"{\rtf1\pard abc}");
        EditorHost.Pump();

        TextBlock Paragraph() => editor.GetVisualDescendants().OfType<TextBlock>().First(block => block.GetType().Name == "EditableParagraph");
        Assert.Equal(Colors.Red, ((ISolidColorBrush)Paragraph().Foreground!).Color);

        editor.ApplyBlackOnWhite();
        EditorHost.Pump();

        Assert.Equal(Colors.Black, ((ISolidColorBrush)Paragraph().Foreground!).Color);
        window.Close();
    }
}
