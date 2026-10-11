using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace AvRichTextBox;

/// <summary>
/// Fixed paper surface for the RichTextBox: black on white, independent of the application theme.
/// </summary>
/// <remarks>
/// The document paragraphs (EditableParagraph) derive from TextBlock. A theme that sets the TextBlock foreground
/// through a style wins over the foreground inherited from the RichTextBox - newly typed runs without their own
/// foreground then appeared in the theme text color. This style sits closer to the paragraph than any
/// application or theme style and applies only to the document tree (DocIC), not to the context menu or overlays.
/// Runs with their own RTF color keep it.
/// </remarks>
public static class PaperSurface
{
    public static void ApplyBlackOnWhite(this RichTextBox richTextBox)
    {
        richTextBox.Background = Brushes.White;
        richTextBox.Foreground = Brushes.Black;

        var paragraphStyle = new Style(x => x.OfType<ItemsControl>().Name("DocIC").Descendant().Is<TextBlock>());
        paragraphStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
        richTextBox.Styles.Add(paragraphStyle);
    }
}
