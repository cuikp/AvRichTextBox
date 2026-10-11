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

    // \uN is followed by one fallback character ('?'), which is dropped; the text after it must stay.
    [AvaloniaFact]
    public void LoadRtf_UnicodeEscapeFollowedByText_KeepsTheText()
    {
        using var host = new EditorHost(@"{\rtf1\ansi\uc1\pard caf\u233? cr\u232?me, na\u239?ve and \u26085?\u26412?\u35486?text}");

        Assert.Equal("café crème, naïve and 日本語text", host.Document.Text.Trim('\r', '\n'));
    }

    // RTF \ansi without \ansicpg reads \'xx bytes as code page 1252.
    [AvaloniaFact]
    public void LoadRtf_HexEscapeWithoutCodePage_DecodesAsWindows1252()
    {
        using var host = new EditorHost(@"{\rtf1\ansi\pard caf\'e9 \'b7 na\'efve}");

        Assert.Equal("café · naïve", host.Document.Text.Trim('\r', '\n'));
    }
}
