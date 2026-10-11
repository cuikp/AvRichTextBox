using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvRichTextBox.Tests;

public class LayoutTests
{
    [AvaloniaFact]
    public void FirstLayoutPass_MeasuresTheParagraphTextBeforeLoaded()
    {
        // Before the editor is shown its document does not exist yet; such hosts assign one themselves.
        var editor = new RichTextBox { FlowDocument = new FlowDocument() };
        editor.LoadRtf(@"{\rtf1\pard one\par two\par three}");
        var window = new Window { Width = 600, Height = 400, Content = editor, SizeToContent = SizeToContent.Height };

        // Show runs the first layout pass synchronously; Loaded is raised later by the dispatcher. Hosts that
        // render a document once (printing, image export) read this first measurement.
        window.Show();
        double firstPassHeight = ParagraphHeights(editor);

        EditorHost.Pump();
        double finalHeight = ParagraphHeights(editor);

        Assert.True(finalHeight > 0);
        Assert.Equal(finalHeight, firstPassHeight, 1);
        window.Close();
    }

    private static double ParagraphHeights(RichTextBox editor) =>
        editor.GetVisualDescendants().OfType<TextBlock>().Where(block => block.GetType().Name == "EditableParagraph").Sum(block => block.Bounds.Height);
}
