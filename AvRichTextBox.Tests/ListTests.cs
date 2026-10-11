using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Xunit;

namespace AvRichTextBox.Tests;

public class ListTests
{
    // Numbered list as RichEdit writes it: \pntext carries the finished marker, {\*\pn...} the definition.
    private const string NumberedList =
        @"{\rtf1{\fonttbl{\f0 Arial;}}" +
        @"{\pntext\f0 1.\tab}{\*\pn\pnlvlbody\pnf0\pnindent0\pnstart1\pndec{\pntxta.}}\pard\li360\fi-360\f0 one\par " +
        @"{\pntext\f0 2.\tab}{\*\pn\pnlvlbody\pnf0\pnindent0\pnstart1\pndec{\pntxta.}}\pard\li360\fi-360\f0 two\par}";

    // Bullet list as Word writes it: \listtext with the Symbol font bullet \'b7.
    private const string WordBulletList =
        @"{\rtf1{\fonttbl{\f0 Arial;}{\f1 Symbol;}}" +
        @"\pard\li720\fi-360{\listtext\f1\'b7\tab}\f0 apple\par}";

    private static List<Paragraph> Paragraphs(EditorHost host) => [.. host.Document.Blocks.OfType<Paragraph>()];

    private static string TextOf(Paragraph paragraph) => paragraph.Text.TrimEnd('\r');

    [AvaloniaFact]
    public void LoadRtf_PntextList_TakesTheMarkersOutOfTheText()
    {
        using var host = new EditorHost(NumberedList);

        List<Paragraph> paragraphs = Paragraphs(host);
        Assert.Equal(["1.", "2."], paragraphs.Take(2).Select(p => p.ListMarker));
        Assert.Equal(["one", "two"], paragraphs.Take(2).Select(TextOf));
        Assert.Equal(24, paragraphs[0].LeftIndent);
        Assert.Equal(-24, paragraphs[0].FirstLineIndent);
    }

    [AvaloniaFact]
    public void LoadRtf_WordListtextBullet_BecomesABulletMarker()
    {
        using var host = new EditorHost(WordBulletList);

        Paragraph paragraph = Paragraphs(host)[0];
        Assert.Equal("•", paragraph.ListMarker);
        Assert.Equal("apple", TextOf(paragraph));
    }

    [AvaloniaFact]
    public void ListMarker_IsShownBesideTheParagraph()
    {
        using var host = new EditorHost(NumberedList);

        Assert.Contains(host.Editor.GetVisualDescendants().OfType<TextBlock>(), block => block.Text == "1." && block.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void SaveRtf_List_SurvivesTheRoundTrip()
    {
        using var source = new EditorHost(NumberedList);

        string rtf = source.Editor.SaveRtf();
        using var reloaded = new EditorHost(rtf);

        Assert.Contains(@"\pntext", rtf);
        List<Paragraph> paragraphs = Paragraphs(reloaded);
        Assert.Equal(["1.", "2."], paragraphs.Take(2).Select(p => p.ListMarker));
        Assert.Equal(["one", "two"], paragraphs.Take(2).Select(TextOf));
        Assert.Equal(24, paragraphs[1].LeftIndent);
        Assert.Equal(-24, paragraphs[1].FirstLineIndent);
    }

    [AvaloniaFact]
    public void SaveRtf_BulletList_SurvivesTheRoundTrip()
    {
        using var source = new EditorHost(WordBulletList);

        using var reloaded = new EditorHost(source.Editor.SaveRtf());

        Paragraph paragraph = Paragraphs(reloaded)[0];
        Assert.Equal("•", paragraph.ListMarker);
        Assert.Equal("apple", TextOf(paragraph));
    }

    [AvaloniaFact]
    public void Enter_AtTheEndOfAListItem_ContinuesTheNumbering()
    {
        using var host = new EditorHost(NumberedList);

        host.Select(7, 0); // end of "two"
        host.Press(Key.Enter);
        host.Type("three");

        Paragraph third = Paragraphs(host)[2];
        Assert.Equal("3.", third.ListMarker);
        Assert.Equal("three", TextOf(third));
        Assert.Equal(24, third.LeftIndent);
    }

    [AvaloniaFact]
    public void Enter_InsideAListItem_SplitsItIntoTwoItems()
    {
        using var host = new EditorHost(NumberedList);

        host.Select(1, 0); // between "o" and "ne"
        host.Press(Key.Enter);

        List<Paragraph> paragraphs = Paragraphs(host);
        Assert.Equal(["o", "ne"], paragraphs.Take(2).Select(TextOf));
        Assert.Equal(["1.", "2."], paragraphs.Take(2).Select(p => p.ListMarker));
    }

    [AvaloniaFact]
    public void Backspace_AtTheStartOfAListItem_RemovesTheMarkerAndCtrlZRestoresIt()
    {
        using var host = new EditorHost(NumberedList);

        host.Select(4, 0); // start of "two"
        host.Press(Key.Back);

        Paragraph second = Paragraphs(host)[1];
        Assert.Equal("", second.ListMarker);
        Assert.Equal("two", TextOf(second));

        host.Press(Key.Z, RawInputModifiers.Control);

        Assert.Equal("2.", Paragraphs(host)[1].ListMarker);
    }

    [AvaloniaFact]
    public void Typing_InAListItem_GoesIntoItsText()
    {
        using var host = new EditorHost(NumberedList);

        host.Select(3, 0); // end of "one"
        host.Type("x");

        Assert.Equal("onex", TextOf(Paragraphs(host)[0]));
    }
}
