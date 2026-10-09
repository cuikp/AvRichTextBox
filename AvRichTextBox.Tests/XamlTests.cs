using Avalonia.Headless.XUnit;
using Avalonia.Media;
using System.Globalization;
using Xunit;

namespace AvRichTextBox.Tests;

public class XamlTests
{
    private static EditorHost LoadXaml(string sectionContent, string sectionAttributes = "")
    {
        var host = new EditorHost();
        host.Editor.LoadXamlString($"<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" {sectionAttributes}>{sectionContent}</Section>");
        EditorHost.Pump();
        return host;
    }

    private static List<Paragraph> Paragraphs(EditorHost host) => [.. host.Document.Blocks.OfType<Paragraph>()];

    private static EditableRun RunWithText(EditorHost host, string text) =>
        Assert.Single(host.Runs, run => run.Text == text);

    [AvaloniaFact]
    public void LoadXaml_FontSizeInPoints_IsConvertedToPixels()
    {
        using var host = LoadXaml("<Paragraph><Run FontSize=\"12pt\">abc</Run></Paragraph>");

        Assert.Equal(16, RunWithText(host, "abc").FontSize, 6);
    }

    [AvaloniaFact]
    public void LoadXaml_DecimalNumbers_AreReadCultureInvariant()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            using var host = LoadXaml("<Paragraph><Run FontSize=\"10.5\">abc</Run></Paragraph>");

            Assert.Equal(10.5, RunWithText(host, "abc").FontSize);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [AvaloniaFact]
    public void LoadXaml_TableColumnWidth_IsReadCultureInvariant()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            using var host = LoadXaml(
                "<Table><Table.Columns><TableColumn Width=\"100.5\"/></Table.Columns>" +
                "<TableRowGroup><TableRow><TableCell><Paragraph>a</Paragraph></TableCell></TableRow></TableRowGroup></Table>");

            Table table = Assert.Single(host.Document.Blocks.OfType<Table>());
            Assert.Equal(100.5, table.ColDefs[0].Width.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [AvaloniaFact]
    public void LoadXaml_ParagraphFormatting_IsInheritedByItsRuns()
    {
        using var host = LoadXaml("<Paragraph FontWeight=\"bold\" FontStyle=\"italic\"><Run>abc</Run></Paragraph>");

        EditableRun run = RunWithText(host, "abc");
        Assert.Equal(FontWeight.Bold, run.FontWeight);
        Assert.Equal(FontStyle.Italic, run.FontStyle);
    }

    [AvaloniaFact]
    public void LoadXaml_TextDirectlyInAParagraph_IsKept()
    {
        using var host = LoadXaml("<Paragraph>before <Run FontWeight=\"Bold\">bold</Run> after</Paragraph>");

        Assert.Equal("before bold after", Paragraphs(host)[0].Text.TrimEnd('\r'));
    }

    [AvaloniaFact]
    public void LoadXaml_SpanBoldItalicUnderline_FormatTheirText()
    {
        using var host = LoadXaml("<Paragraph><Bold><Italic>bi</Italic></Bold><Underline>u</Underline><Span Foreground=\"#FF0000\">red</Span></Paragraph>");

        EditableRun boldItalic = RunWithText(host, "bi");
        Assert.Equal(FontWeight.Bold, boldItalic.FontWeight);
        Assert.Equal(FontStyle.Italic, boldItalic.FontStyle);
        Assert.Contains(RunWithText(host, "u").TextDecorations!, decoration => decoration.Location == TextDecorationLocation.Underline);
        Assert.Equal(Colors.Red, ((ISolidColorBrush)RunWithText(host, "red").Foreground!).Color);
    }

    [AvaloniaFact]
    public void LoadXaml_SectionFont_IsInheritedThroughNestedBlocks()
    {
        using var host = LoadXaml("<Section><Paragraph><Run>abc</Run></Paragraph></Section>", "FontFamily=\"Courier New\"");

        Assert.Equal("Courier New", RunWithText(host, "abc").FontFamily.Name);
    }

    [AvaloniaFact]
    public void LoadXaml_DecimalList_BecomesNumberedListParagraphs()
    {
        using var host = LoadXaml(
            "<List MarkerStyle=\"Decimal\"><ListItem><Paragraph>one</Paragraph></ListItem><ListItem><Paragraph>two</Paragraph></ListItem></List>");

        List<Paragraph> paragraphs = Paragraphs(host);
        Assert.Equal(["1.", "2."], paragraphs.Select(p => p.ListMarker));
        Assert.Equal(["one", "two"], paragraphs.Select(p => p.Text.TrimEnd('\r')));
        Assert.Equal(48, paragraphs[0].LeftIndent);
        Assert.Equal(-24, paragraphs[0].FirstLineIndent);
    }

    [AvaloniaFact]
    public void LoadXaml_EmptyParagraph_KeepsTheInheritedFont()
    {
        using var host = LoadXaml("<Paragraph FontFamily=\"Courier New\"></Paragraph>");

        Assert.Equal("Courier New", Assert.Single(host.Runs).FontFamily.Name);
    }

    [AvaloniaFact]
    public void SaveRtf_RunWithInheritedPlaceholderFont_WritesNoPlaceholderFontName()
    {
        using var host = LoadXaml("<Paragraph><Run FontFamily=\"$Default\">abc</Run></Paragraph>");

        Assert.DoesNotContain(FontFamily.Default.Name, host.Editor.SaveRtf());
    }

    [AvaloniaFact]
    public void SaveXaml_List_SurvivesTheRoundTrip()
    {
        using var source = new EditorHost(
            @"{\rtf1{\fonttbl{\f0 Arial;}}{\pntext\f0 1.\tab}{\*\pn\pnlvlbody\pnf0\pnindent0\pnstart1\pndec{\pntxta.}}\pard\li360\fi-360\f0 one\par}");

        string xaml = source.Editor.SaveXamlString();
        using var reloaded = new EditorHost();
        reloaded.Editor.LoadXamlString(xaml);
        EditorHost.Pump();

        Paragraph paragraph = Paragraphs(reloaded)[0];
        Assert.Equal("1.", paragraph.ListMarker);
        Assert.Equal("one", paragraph.Text.TrimEnd('\r'));
    }
}
