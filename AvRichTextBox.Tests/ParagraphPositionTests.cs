using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvRichTextBox.Tests;

// DocICRelativeTop/Left place caret and selection: the paragraph position relative to the document ItemsControl.
public class ParagraphPositionTests
{
    // Margin of the StackPanel in the ItemsPanelTemplate of DocIC.
    private const double ItemsPanelMargin = 5;

    private static double ExpectedLeft(double inner = 0) =>
        PagePadding.Left + ItemsPanelMargin + ParagraphMargin.Left + ParagraphBorder.Left + inner;

    private static double ExpectedTop() =>
        PagePadding.Top + ItemsPanelMargin + ParagraphMargin.Top + ParagraphBorder.Top;

    private static readonly Thickness PagePadding = new(30, 20, 10, 10);
    private static readonly Thickness ParagraphMargin = new(7, 11, 0, 0);
    private static readonly Thickness ParagraphBorder = new(3, 4, 0, 0);

    private static List<Paragraph> Paragraphs(EditorHost host) => [.. host.Document.Blocks.OfType<Paragraph>()];

    private static ItemsControl DocIC(EditorHost host) =>
        host.Editor.GetVisualDescendants().OfType<ItemsControl>().First(ic => ic.Name == "DocIC");

    private static EditableParagraph EditableOf(EditorHost host, Paragraph paragraph) =>
        host.Editor.GetVisualDescendants().OfType<EditableParagraph>().First(ep => ReferenceEquals(ep.DataContext, paragraph));

    // Position from the arranged layout rectangles, without TranslatePoint.
    private static Point LayoutOffset(Visual visual, Visual ancestor)
    {
        double x = 0, y = 0;
        for (Visual? current = visual; current != null && current != ancestor; current = current.GetVisualParent())
        {
            x += current.Bounds.X;
            y += current.Bounds.Y;
        }
        return new Point(x, y);
    }

    private static void Format(EditorHost host, Paragraph paragraph)
    {
        host.Document.PagePadding = PagePadding;
        paragraph.Margin = ParagraphMargin;
        paragraph.BorderThickness = ParagraphBorder;
        EditorHost.Pump();
    }

    // Moves the caret into the paragraph, which updates its relative position.
    private static void PlaceCaretIn(EditorHost host, Paragraph paragraph)
    {
        host.Select(paragraph.StartInDoc + 1, 0);
        EditorHost.Pump();
    }

    [AvaloniaFact]
    public void Paragraph_WithPagePaddingMarginAndBorder_HasTheExpectedPosition()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc\par}");
        Paragraph paragraph = Paragraphs(host)[0];
        Format(host, paragraph);

        PlaceCaretIn(host, paragraph);

        Assert.Equal(ExpectedLeft(), paragraph.DocICRelativeLeft, 3);
        Assert.Equal(ExpectedTop(), paragraph.DocICRelativeTop, 3);
    }

    [AvaloniaFact]
    public void ListParagraph_StartsAfterTheMarkerColumn()
    {
        using var host = new EditorHost(
            @"{\rtf1{\fonttbl{\f0 Arial;}}" +
            @"{\pntext\f0 1.\tab}{\*\pn\pnlvlbody\pnf0\pnindent0\pnstart1\pndec{\pntxta.}}\pard\li360\fi-360\f0 one\par}");
        Paragraph paragraph = Paragraphs(host)[0];
        Format(host, paragraph);

        PlaceCaretIn(host, paragraph);

        // \li360\fi-360: IndentMargin 0, marker column 24 px.
        Assert.Equal(ExpectedLeft(24), paragraph.DocICRelativeLeft, 3);
        Assert.Equal(ExpectedTop(), paragraph.DocICRelativeTop, 3);
    }

    [AvaloniaFact]
    public void TableCellParagraph_IsPlacedRelativeToTheDocumentNotTheCell()
    {
        using var host = new EditorHost(
            @"{\rtf1\pard before\par" +
            @"\trowd\cellx2000\cellx4000\pard\intbl left\cell\pard\intbl right\cell\row" +
            @"\pard after\par}");
        host.Document.PagePadding = PagePadding;
        EditorHost.Pump();
        Paragraph before = Paragraphs(host)[0];
        Paragraph right = host.Document.AllParagraphs.First(p => p.Text.StartsWith("right"));
        PlaceCaretIn(host, before);

        PlaceCaretIn(host, right);

        Point expected = LayoutOffset(EditableOf(host, right), DocIC(host));
        Assert.Equal(expected.X, right.DocICRelativeLeft, 3);
        Assert.Equal(expected.Y, right.DocICRelativeTop, 3);
        Assert.True(right.DocICRelativeTop > before.DocICRelativeTop);
        Assert.True(right.DocICRelativeLeft > PagePadding.Left + ItemsPanelMargin);
    }

    [AvaloniaFact]
    public void Paragraph_KeepsItsPositionWhenTheDocumentIsScrolled()
    {
        string body = string.Concat(Enumerable.Range(1, 60).Select(i => $@"line {i}\par "));
        using var host = new EditorHost($@"{{\rtf1\pard {body}}}");
        Paragraph paragraph = Paragraphs(host)[45];
        Format(host, paragraph);
        ScrollViewer scrollViewer = host.Editor.GetVisualDescendants().OfType<ScrollViewer>().First(sv => sv.Name == "FlowDocSV");

        PlaceCaretIn(host, paragraph);

        scrollViewer.Offset = new Vector(0, 0);
        EditorHost.Pump();
        paragraph.CallRequestTextLayoutInfoStart();
        EditorHost.Pump();
        double topUnscrolled = paragraph.DocICRelativeTop;
        double leftUnscrolled = paragraph.DocICRelativeLeft;

        scrollViewer.Offset = new Vector(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        EditorHost.Pump();
        paragraph.CallRequestTextLayoutInfoStart();
        EditorHost.Pump();

        Point expected = LayoutOffset(EditableOf(host, paragraph), DocIC(host));
        Assert.True(scrollViewer.Offset.Y > 100);
        Assert.Equal(topUnscrolled, paragraph.DocICRelativeTop, 3);
        Assert.Equal(leftUnscrolled, paragraph.DocICRelativeLeft, 3);
        Assert.Equal(expected.Y, paragraph.DocICRelativeTop, 3);
        Assert.Equal(expected.X, paragraph.DocICRelativeLeft, 3);
    }
}
