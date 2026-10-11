using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Xunit;

namespace AvRichTextBox.Tests;

public class FormattingTests
{
    // "aa bb cc" with "bb" (chars 3-4) formatted; selecting exactly "bb" splits off zero-length edge runs.
    private static EditorHost HostWithFormattedMiddleRun(string formatOn, string formatOff) =>
        new($@"{{\rtf1\pard aa {formatOn} bb{formatOff}  cc}}");

    private static EditableRun RunWithText(EditorHost host, string text) =>
        Assert.Single(host.Runs, run => run.Text == text);

    [AvaloniaFact]
    public void CtrlB_OnExactlySelectedBoldRun_RemovesBold()
    {
        using var host = HostWithFormattedMiddleRun(@"\b", @"\b0");

        host.Select(3, 2);
        host.Press(Key.B, RawInputModifiers.Control);

        Assert.Equal(FontWeight.Normal, RunWithText(host, "bb").FontWeight);
    }

    [AvaloniaFact]
    public void CtrlI_OnExactlySelectedItalicRun_RemovesItalic()
    {
        using var host = HostWithFormattedMiddleRun(@"\i", @"\i0");

        host.Select(3, 2);
        host.Press(Key.I, RawInputModifiers.Control);

        Assert.Equal(FontStyle.Normal, RunWithText(host, "bb").FontStyle);
    }

    [AvaloniaFact]
    public void CtrlU_OnExactlySelectedUnderlinedRun_RemovesUnderline()
    {
        using var host = HostWithFormattedMiddleRun(@"\ul", @"\ulnone");

        host.Select(3, 2);
        host.Press(Key.U, RawInputModifiers.Control);

        Assert.Null(RunWithText(host, "bb").TextDecorations);
    }

    [AvaloniaFact]
    public void TypingFormat_WithoutSelection_AppliesToTheNextTypedText()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc}");

        host.Select(3, 0);
        host.Document.ApplyFormattingOrTypingFormat(Inline.FontSizeProperty, 30.0);
        host.Type("x");

        Assert.Equal(30.0, RunWithText(host, "x").FontSize);
        Assert.NotEqual(30.0, RunWithText(host, "abc").FontSize);
    }

    [AvaloniaFact]
    public void TypingFormat_WithSelection_AppliesToTheSelectionAtOnce()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc}");

        host.Select(0, 3);
        host.Document.ApplyFormattingOrTypingFormat(Inline.FontSizeProperty, 30.0);
        EditorHost.Pump();

        Assert.Equal(30.0, RunWithText(host, "abc").FontSize);
    }

    // A toolbar Bold button without a selection: like Ctrl+B, the next typed text toggles against the caret format.
    [AvaloniaFact]
    public void TypingFormat_FontWeightBold_TogglesTheNextTypedText()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc\b def}");

        host.Select(3, 0);
        host.Document.ApplyFormattingOrTypingFormat(Inline.FontWeightProperty, FontWeight.Bold);
        host.Type("x");
        host.Select(7, 0);
        host.Document.ApplyFormattingOrTypingFormat(Inline.FontWeightProperty, FontWeight.Bold);
        host.Type("y");

        Assert.Equal(FontWeight.Bold, RunWithText(host, "x").FontWeight);
        Assert.Equal(FontWeight.Normal, RunWithText(host, "y").FontWeight);
    }

    // A second click on the toolbar button before typing cancels the first, as a second Ctrl+B does.
    [AvaloniaFact]
    public void TypingFormat_FontWeightBoldTwice_LeavesTheNextTypedTextUnchanged()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc}");

        host.Select(3, 0);
        host.Document.ApplyFormattingOrTypingFormat(Inline.FontWeightProperty, FontWeight.Bold);
        host.Document.ApplyFormattingOrTypingFormat(Inline.FontWeightProperty, FontWeight.Bold);
        host.Type("x");

        // Nothing pending: the typed text continues the existing run.
        Assert.Equal(FontWeight.Normal, RunWithText(host, "abcx").FontWeight);
    }

    [AvaloniaFact]
    public void TypingFormat_UnderlineTwice_LeavesTheNextTypedTextUnchanged()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc}");

        host.Select(3, 0);
        host.Document.ApplyFormattingOrTypingFormat(Inline.TextDecorationsProperty, TextDecorationLocation.Underline);
        host.Document.ApplyFormattingOrTypingFormat(Inline.TextDecorationsProperty, TextDecorationLocation.Underline);
        host.Type("x");

        Assert.Null(RunWithText(host, "abcx").TextDecorations);
    }

    [AvaloniaFact]
    public void FontSize_AppliedTwiceToTheSameSelection_TakesTheSecondValue()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc}");

        host.Select(0, 3);
        host.Document.ApplyFormattingOrTypingFormat(Inline.FontSizeProperty, 20.0);
        EditorHost.Pump();
        host.Document.ApplyFormattingOrTypingFormat(Inline.FontSizeProperty, 30.0);
        EditorHost.Pump();

        Assert.Equal(30.0, RunWithText(host, "abc").FontSize);
    }

    [AvaloniaFact]
    public void Enter_AtTheEndOfAParagraph_ContinuesTheCharacterFormat()
    {
        using var host = new EditorHost(@"{\rtf1{\fonttbl{\f0 Arial;}}\pard\f0\fs20\b abc}");
        EditableRun last = RunWithText(host, "abc");

        host.Select(3, 0);
        host.Press(Key.Enter);

        // The new paragraph carries the format before anything is typed - a formatting toolbar shows it at the caret.
        Paragraph second = host.Document.Blocks.OfType<Paragraph>().ElementAt(1);
        EditableRun empty = Assert.IsType<EditableRun>(Assert.Single(second.Inlines));
        Assert.Equal(last.FontFamily.Name, empty.FontFamily.Name);
        Assert.Equal(last.FontSize, empty.FontSize);
        Assert.Equal(FontWeight.Bold, empty.FontWeight);

        host.Type("x");
        Assert.Equal("Arial", RunWithText(host, "x").FontFamily.Name);
    }

    [AvaloniaFact]
    public void Enter_AfterAHyperlinkAtTheLineEnd_StartsPlainText()
    {
        using var host = new EditorHost(@"{\rtf1\pard see {\field{\*\fldinst HYPERLINK ""https://example.com""}{\fldrslt link}}}");
        Assert.Contains(host.Document.Blocks.OfType<Paragraph>().First().Inlines, inline => inline is EditableHyperlink);

        host.Select(host.Document.Blocks.OfType<Paragraph>().First().Text.TrimEnd('\r').Length, 0);
        host.Press(Key.Enter);

        Paragraph second = host.Document.Blocks.OfType<Paragraph>().ElementAt(1);
        Assert.IsNotType<EditableHyperlink>(Assert.Single(second.Inlines));
    }

    [AvaloniaFact]
    public void GetCaretFormat_ReportsAPendingTypingFormat()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc}");

        host.Select(1, 0);
        host.Document.ApplyFormattingOrTypingFormat(Inline.FontSizeProperty, 30.0);

        Assert.Equal(30.0, host.Document.GetCaretFormat(Inline.FontSizeProperty));
    }

    // ApplyFormatting leaves a backward start bias; a formatting toolbar reads the format right afterwards.
    [AvaloniaFact]
    public void GetFormatting_AfterFormattingASelectionAtTheDocumentStart_ReportsTheNewFormat()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc}");

        host.Select(0, 3);
        host.Document.Selection.ApplyFormatting(Inline.FontSizeProperty, 30.0);
        EditorHost.Pump();

        Assert.Equal(30.0, host.Document.Selection.GetFormatting(Inline.FontSizeProperty));
    }

    [AvaloniaFact]
    public void GetFormatting_AfterFormattingASelectionAfterAnotherRun_ReportsTheNewFormat()
    {
        using var host = new EditorHost(@"{\rtf1\pard aa bb cc}");

        host.Select(3, 2);
        host.Document.Selection.ApplyFormatting(Inline.FontWeightProperty, FontWeight.Bold);
        EditorHost.Pump();

        Assert.Equal(FontWeight.Bold, host.Document.Selection.GetFormatting(Inline.FontWeightProperty));
        Assert.Equal(3, host.Document.Selection.Start);
        Assert.Equal(2, host.Document.Selection.Length);
    }

    [AvaloniaFact]
    public void TypingFormat_DoesNotReapplyAnEarlierBoldToggle()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc}");

        host.Select(3, 0);
        host.Press(Key.B, RawInputModifiers.Control);
        host.Type("a");
        // Moving the caret programmatically (like a mouse click) does not reset the toggle state.
        host.Select(0, 0);
        host.Document.ApplyFormattingOrTypingFormat(Inline.FontSizeProperty, 30.0);
        host.Type("x");

        Assert.Equal(FontWeight.Normal, RunWithText(host, "x").FontWeight);
    }
}
