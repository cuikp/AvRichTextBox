using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Xunit;

namespace AvRichTextBox.Tests;

public class OverlayTests
{
    [AvaloniaFact]
    public void CaretAndSelection_AreShownInAWindowWithoutAdornerLayer()
    {
        var editor = new RichTextBox();
        // A window template with just a content presenter, as custom shells and docking hosts often use:
        // there is no VisualLayerManager, so AdornerLayer.GetAdornerLayer finds nothing.
        var window = new Window
        {
            Width = 600,
            Height = 400,
            Content = editor,
            Template = new FuncControlTemplate<Window>((owner, _) => new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                [!ContentPresenter.ContentProperty] = owner[!ContentControl.ContentProperty],
            }),
        };
        window.Show();
        EditorHost.Pump();

        Assert.Same(window, TopLevel.GetTopLevel(editor.SelectionPath));

        window.Close();
    }
}
