using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;

namespace AvRichTextBox.Tests;

public class RtfImportTests
{
    private static string PngHex(int width, int height)
    {
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var stream = new MemoryStream();
#pragma warning disable CS0618 // same PNG encoder overload the library itself uses for RTF export
        bitmap.Save(stream);
#pragma warning restore CS0618
        return Convert.ToHexString(stream.ToArray());
    }

    private static Image SingleImage(EditorHost host) =>
        Assert.IsType<Image>(Assert.Single(host.Document.Blocks.OfType<Paragraph>().SelectMany(p => p.Inlines).OfType<EditableInlineUIContainer>()).GetChild());

    [AvaloniaFact]
    public void LoadRtf_PictureWithoutData_StillLoadsTheText()
    {
        using var host = new EditorHost(@"{\rtf1\pard ab{\pict\pngblip\picw4\pich3\picwgoal60\pichgoal45 }cd}");

        // The image keeps its place in the text (shown as '@').
        Assert.Equal("ab@cd", host.Document.Text.Trim('\r', '\n'));
    }

    [AvaloniaFact]
    public void LoadRtf_PictureWithUndecodableData_StillLoadsTheText()
    {
        using var host = new EditorHost(@"{\rtf1\pard ab{\pict\pngblip\picw4\pich3\picwgoal60\pichgoal45 00ff00ff00ff}cd}");

        // The image keeps its place in the text (shown as '@').
        Assert.Equal("ab@cd", host.Document.Text.Trim('\r', '\n'));
    }

    [AvaloniaFact]
    public void LoadRtf_PictureWithoutGoalSize_UsesTheBitmapSize()
    {
        using var host = new EditorHost($@"{{\rtf1\pard ab{{\pict\pngblip\picw4\pich3\picwgoal0\pichgoal0 {PngHex(4, 3)}}}cd}}");

        Image image = SingleImage(host);
        Assert.Equal(4, image.Width);
        Assert.Equal(3, image.Height);
    }
}
