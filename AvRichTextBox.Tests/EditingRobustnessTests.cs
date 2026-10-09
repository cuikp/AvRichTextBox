using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Xunit;

namespace AvRichTextBox.Tests;

public class EditingRobustnessTests
{
    [AvaloniaFact]
    public void GetSelectedParagraphs_SelectionAcrossParagraphs_ReturnsEveryOverlappingParagraph()
    {
        using var host = new EditorHost(@"{\rtf1\pard one\par two\par three}");

        host.Select(1, 6);

        Assert.Equal(["one", "two"], host.Document.GetSelectedParagraphs.Select(p => p.Text.TrimEnd('\r')));
    }

    [AvaloniaFact]
    public void GetSelectedParagraphs_CollapsedSelection_ReturnsTheCaretParagraph()
    {
        using var host = new EditorHost(@"{\rtf1\pard one\par two\par three}");

        host.Select(5, 0);

        Assert.Equal(["two"], host.Document.GetSelectedParagraphs.Select(p => p.Text.TrimEnd('\r')));
    }

    [AvaloniaFact]
    public void LoadRtf_TextBeforeAnyParagraphControlWord_IsRead()
    {
        using var host = new EditorHost(@"{\rtf1 Hello}");

        Assert.Contains("Hello", host.Document.Text);
    }

    [AvaloniaFact]
    public void LoadRtf_EmptyParagraphAfterAnotherDocument_AcceptsTyping()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc}");

        host.Editor.LoadRtf(@"{\rtf1\pard\par}");
        EditorHost.Pump();
        host.Type("x");

        Assert.Equal("x", host.Document.Text.Trim('\r', '\n'));
    }

    [AvaloniaFact]
    public void GetFormatting_FontWeight_ReturnsTheWeightAtTheCaret()
    {
        using var host = new EditorHost(@"{\rtf1\pard\b bold\b0  plain}");

        host.Select(2, 0);

        Assert.Equal(FontWeight.Bold, host.Document.Selection.GetFormatting(Inline.FontWeightProperty));
    }

    [AvaloniaFact]
    public void Typing_OverWholeDocumentSelection_ReplacesTheText()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc\par def}");

        host.Document.SelectAll();
        EditorHost.Pump();
        host.Type("x");

        Assert.Equal("x", host.Document.Text.Trim('\r', '\n'));
    }

    [AvaloniaFact]
    public void Typing_AfterDeletingAllText_KeepsTheDeletedFormat()
    {
        using var host = new EditorHost(@"{\rtf1\pard\b abc\b0\par}");

        host.Document.SelectAll();
        EditorHost.Pump();
        host.Press(Key.Delete);
        host.Type("x");

        Assert.Equal(FontWeight.Bold, Assert.Single(host.Runs, run => run.Text == "x").FontWeight);
    }

    [AvaloniaFact]
    public void Typing_AfterBackspacingTheOnlyChar_KeepsItsFormat()
    {
        using var host = new EditorHost(@"{\rtf1\pard\b a\b0}");

        host.Select(1, 0);
        host.Press(Key.Back);
        host.Type("x");

        Assert.Equal(FontWeight.Bold, Assert.Single(host.Runs, run => run.Text == "x").FontWeight);
    }

    [AvaloniaFact]
    public void Delete_AtTheEndOfAFormattedRun_RemovesTheNextChar()
    {
        using var host = new EditorHost(@"{\rtf1\pard\b ab\b0 cd}");

        // Arrowing right leaves the caret at the end of the bold run rather than at the start of the next one.
        host.Select(0, 0);
        host.Press(Key.Right);
        host.Press(Key.Right);
        host.Press(Key.Delete);

        Assert.Equal("abd", host.Document.Text.Trim('\r', '\n'));
    }

    [AvaloniaFact]
    public void Escape_WithoutPopup_ReachesTheHostingWindowUnhandled()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc}");
        bool? handledAtWindow = null;
        host.Window.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Escape)
                handledAtWindow = e.Handled;
        }, RoutingStrategies.Bubble, handledEventsToo: true);

        host.Press(Key.Escape);

        Assert.False(handledAtWindow);
    }

    [AvaloniaFact]
    public void Loading_DoesNotTakeTheFocusFromTheHostingWindow()
    {
        var textBox = new TextBox();
        var panel = new StackPanel { Children = { textBox } };
        var window = new Window { Width = 600, Height = 400, Content = panel };
        window.Show();
        textBox.Focus();
        EditorHost.Pump();
        Assert.True(textBox.IsFocused, $"Precondition, focused element: {window.FocusManager?.GetFocusedElement()}");

        panel.Children.Add(new RichTextBox());
        EditorHost.Pump();

        Assert.True(textBox.IsFocused, $"Focused element: {window.FocusManager?.GetFocusedElement()}");
        window.Close();
    }
}
