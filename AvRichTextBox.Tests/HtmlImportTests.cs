using Avalonia.Headless.XUnit;
using Avalonia.Media;
using System.Globalization;
using Xunit;

namespace AvRichTextBox.Tests;

public class HtmlImportTests
{
    // The HTML reader takes the column count from <colgroup>.
    private const string TableHtml =
        "<html><body><table style=\"width:{0}px\"><colgroup><col/></colgroup><tr><td>a</td></tr></table></body></html>";

    private static double LoadedTableWidth(string width)
    {
        using var host = new EditorHost();
        host.Editor.LoadHtml(string.Format(CultureInfo.InvariantCulture, TableHtml, width));
        EditorHost.Pump();

        return Assert.Single(host.Document.Blocks.OfType<Table>()).Width;
    }

    [AvaloniaFact]
    public void LoadHtml_TableWidth_IsReadCultureInvariant()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            double invariantWidth = LoadedTableWidth("100.5");
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal(invariantWidth, LoadedTableWidth("100.5"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    private static EditorHost LoadBody(string body)
    {
        var host = new EditorHost();
        host.Editor.LoadHtml("<html><body>" + body + "</body></html>");
        EditorHost.Pump();
        return host;
    }

    private static string[] ParagraphTexts(IEnumerable<Block> blocks) =>
        [.. blocks.OfType<Paragraph>().Select(paragraph => paragraph.Text.TrimEnd('\r', '\a'))];

    [AvaloniaTheory]
    [InlineData("<div><p>one</p></div>", "one")]
    [InlineData("<div><div><section><p>one</p><p>two</p></section></div></div>", "one|two")]
    [InlineData("<main><article><header><h2>title</h2></header><div><p>body</p></div></article></main>", "title|body")]
    [InlineData("<blockquote><p>quoted</p></blockquote>", "quoted")]
    public void LoadHtml_ParagraphsInContainers_AreRead(string body, string expected)
    {
        using var host = LoadBody(body);

        Assert.Equal(expected.Split('|'), ParagraphTexts(host.Document.Blocks));
    }

    // Mail clients write lines as text directly in div elements, with <div><br></div> for an empty line.
    [AvaloniaFact]
    public void LoadHtml_TextDirectlyInContainers_FormsParagraphs()
    {
        using var host = LoadBody("<div>first line</div><div><br></div><div>  <b>bold</b> and <i>italic</i><br></div>\n<span>loose</span>");

        Assert.Equal(["first line", "", "bold and italic", "loose"], ParagraphTexts(host.Document.Blocks));
    }

    [AvaloniaFact]
    public void LoadHtml_Headings_AreBoldAndSized()
    {
        using var host = LoadBody("<h1>Big</h1><h3>Small</h3>");

        var runs = host.Document.Blocks.OfType<Paragraph>().Select(paragraph => paragraph.Inlines.OfType<EditableRun>().First()).ToList();
        Assert.Equal(["Big", "Small"], runs.Select(run => run.Text));
        Assert.All(runs, run => Assert.Equal(FontWeight.Bold, run.FontWeight));
        Assert.Equal([40.0, 32.0], runs.Select(run => run.FontSize));
    }

    [AvaloniaFact]
    public void LoadHtml_Lists_GetMarkersAndIndents()
    {
        using var host = LoadBody("<ul><li>a</li><li>b<ol><li>c</li><li>d</li></ol></li></ul><ol start=\"3\"><li>e</li></ol>");

        var items = host.Document.Blocks.OfType<Paragraph>()
            .Select(paragraph => (paragraph.Text.TrimEnd('\r'), paragraph.ListMarker, paragraph.LeftIndent)).ToList();
        Assert.Equal([("a", "•", 24.0), ("b", "•", 24.0), ("c", "1.", 56.0), ("d", "2.", 56.0), ("e", "3.", 24.0)], items);
    }

    [AvaloniaFact]
    public void LoadHtml_TableCells_ReadPlainTextAndNestedTables()
    {
        using var host = LoadBody("<table><colgroup><col/><col/></colgroup><tr><td>plain</td>" +
            "<td><table><colgroup><col/></colgroup><tr><td>inner</td></tr></table></td></tr></table>");

        var table = Assert.Single(host.Document.Blocks.OfType<Table>());
        Assert.Equal(["plain"], ParagraphTexts(table.Cells[0].CellBlocks));
        var nested = Assert.Single(table.Cells[1].CellBlocks.OfType<Table>());
        Assert.Equal(["inner"], ParagraphTexts(nested.Cells[0].CellBlocks));
    }

    [AvaloniaFact]
    public void LoadHtml_TableInContainer_IsReadOnce()
    {
        using var host = LoadBody("<div><table><colgroup><col/></colgroup><tr><td><p>cell</p></td></tr></table></div><p>after</p>");

        Assert.Collection(host.Document.Blocks,
            block => Assert.IsType<Table>(block),
            block => Assert.Equal("after", Assert.IsType<Paragraph>(block).Text.TrimEnd('\r')));
    }

    [AvaloniaFact]
    public void LoadHtml_ScriptAndStyle_AreSkipped()
    {
        using var host = LoadBody("<style>p { color: red; }</style><script>var x = 1;</script><p>text</p>");

        Assert.Equal(["text"], ParagraphTexts(host.Document.Blocks));
    }
}
