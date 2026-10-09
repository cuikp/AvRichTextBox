using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Text.RegularExpressions;
using Xunit;

namespace AvRichTextBox.Tests;

public class RtfExportTests
{
    private static EditableRun RunWithText(EditorHost host, string text) =>
        Assert.Single(host.Runs, run => run.Text == text);

    [AvaloniaFact]
    public void SaveRtf_BoldAtParagraphEnd_DoesNotCarryIntoTheNextParagraph()
    {
        using var source = new EditorHost(@"{\rtf1\pard\b one\b0\par\pard two}");
        Assert.Equal(FontWeight.Normal, RunWithText(source, "two").FontWeight);

        // "one" ends bold in the saved RTF; \pard does not reset character formatting.
        string rtf = source.Editor.SaveRtf();
        using var reloaded = new EditorHost(rtf);

        Assert.Equal(FontWeight.Normal, RunWithText(reloaded, "two").FontWeight);
    }

    [AvaloniaFact]
    public void SaveRtf_FontSizeSetInPixels_IsWrittenAsTheSamePointSize()
    {
        using var host = new EditorHost(@"{\rtf1\pard abc}");
        List<string> changed = [];

        // A toolbar sets points as device-independent pixels with a precomputed factor (96.0 / 72.0). Truncating
        // the converted value loses a half point whenever the floating-point result lands just below the integer
        // (10pt -> 13.333333333333332 px -> 9.999999999999998pt -> \fs19).
        const double dipsPerPoint = 96.0 / 72.0;
        for (int halfPoints = 4; halfPoints <= 144; halfPoints++)
        {
            host.Editor.LoadRtf(@"{\rtf1\pard abc}");
            EditorHost.Pump();
            host.Select(0, 3);
            host.Document.Selection.ApplyFormatting(Avalonia.Controls.Documents.Inline.FontSizeProperty, halfPoints / 2.0 * dipsPerPoint);
            string rtf = host.Editor.SaveRtf();
            if (!rtf.Contains($@"\fs{halfPoints} "))
                changed.Add($"{halfPoints}: {rtf[rtf.IndexOf(@"\pard")..]}");
        }

        Assert.Empty(changed);
    }

    [AvaloniaFact]
    public void SaveRtf_ImageWithoutExplicitSize_KeepsItsPixelSizeAndHasNoLineBreaksInThePicture()
    {
        using var host = new EditorHost(@"{\rtf1\pard ab}");
        var bitmap = new WriteableBitmap(new PixelSize(4, 3), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        var container = new EditableInlineUIContainer(new Image { Source = bitmap });
        Paragraph paragraph = host.Document.Blocks.OfType<Paragraph>().First();
        paragraph.Inlines.Insert(1, container);
        host.Document.UpdateBlockAndInlineStarts(paragraph);
        EditorHost.Pump();

        string rtf = host.Editor.SaveRtf();

        Match picture = Regex.Match(rtf, @"\{\\pict[^}]*\}");
        Assert.True(picture.Success, rtf);
        Assert.Contains(@"\picwgoal60", picture.Value);
        Assert.Contains(@"\pichgoal45", picture.Value);
        Assert.DoesNotContain("\n", picture.Value);
    }

    [AvaloniaFact]
    public void SaveRtf_DocumentEndingWithATable_KeepsTheClosingRow()
    {
        using var host = new EditorHost(@"{\rtf1\trowd\cellx1000\cellx2000\pard\intbl a\cell b\cell\row}");

        string rtf = host.Editor.SaveRtf();

        Assert.Matches(@"\\row\s*\}$", rtf.TrimEnd());
    }
}
