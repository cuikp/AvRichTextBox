using Avalonia.Headless.XUnit;
using Avalonia.Media;
using RtfDomParserAv;
using Xunit;

namespace AvRichTextBox.Tests;

public class RtfColorTableTests
{
    private const string ColorsTable = @"\red0\green0\blue0;\red255\green0\blue0;\red0\green0\blue255;";

    private static EditableRun RunWithText(EditorHost host, string text) =>
        Assert.Single(host.Runs, run => run.Text == text);

    private static void AssertColor(Color expected, IBrush? brush) =>
        Assert.Equal(expected, Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color);

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoadRtf_Foreground_UsesTheActualColorTableIndex(bool leadingAutoColor)
    {
        string table = (leadingAutoColor ? ";" : "") + ColorsTable;
        int first = leadingAutoColor ? 1 : 0;
        using var host = new EditorHost($@"{{\rtf1{{\colortbl{table}}}\pard\cf{first} black\cf{first + 1} red\cf{first + 2} blue}}");

        AssertColor(Colors.Black, RunWithText(host, "black").Foreground);
        AssertColor(Colors.Red, RunWithText(host, "red").Foreground);
        AssertColor(Colors.Blue, RunWithText(host, "blue").Foreground);
    }

    [AvaloniaTheory]
    [InlineData("highlight", false)]
    [InlineData("highlight", true)]
    [InlineData("cb", false)]
    [InlineData("cb", true)]
    [InlineData("chcbpat", false)]
    [InlineData("chcbpat", true)]
    public void LoadRtf_Background_UsesTheActualColorTableIndex(string controlWord, bool leadingAutoColor)
    {
        string table = (leadingAutoColor ? ";" : "") + ColorsTable;
        int first = leadingAutoColor ? 1 : 0;
        using var host = new EditorHost($@"{{\rtf1{{\colortbl{table}}}\pard\{controlWord}{first} black\{controlWord}{first + 1} red\{controlWord}{first + 2} blue}}");

        AssertColor(Colors.Black, RunWithText(host, "black").Background);
        AssertColor(Colors.Red, RunWithText(host, "red").Background);
        AssertColor(Colors.Blue, RunWithText(host, "blue").Background);
    }

    [AvaloniaFact]
    public void LoadRtf_EmptyFirstColor_RestoresAutomaticForegroundAndBackground()
    {
        using var host = new EditorHost(@"{\rtf1{\colortbl;\red255\green0\blue0;}\pard\cf1 red\cf0 automatic\highlight1 highlighted\highlight0 clear\cb1 filled\cb0 empty}");

        AssertColor(Colors.Red, RunWithText(host, "red").Foreground);
        AssertColor(Colors.Black, RunWithText(host, "automatic").Foreground);
        AssertColor(Colors.Red, RunWithText(host, "highlighted").Background);
        AssertColor(Colors.Transparent, RunWithText(host, "clear").Background);
        AssertColor(Colors.Red, RunWithText(host, "filled").Background);
        AssertColor(Colors.Transparent, RunWithText(host, "empty").Background);
    }

    [AvaloniaFact]
    public void LoadRtf_EmptyAndDuplicateColors_KeepTheirTablePositions()
    {
        using var host = new EditorHost(@"{\rtf1{\colortbl;\red255\green0\blue0;;\red255\green0\blue0;\red0\green0\blue255;}\pard\cf1 red\cf2 automatic\cf3 duplicate\cf4 blue}");

        AssertColor(Colors.Red, RunWithText(host, "red").Foreground);
        AssertColor(Colors.Black, RunWithText(host, "automatic").Foreground);
        AssertColor(Colors.Red, RunWithText(host, "duplicate").Foreground);
        AssertColor(Colors.Blue, RunWithText(host, "blue").Foreground);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void SaveAndLoadRtf_KeepsImportedForegroundAndBackground(bool leadingAutoColor)
    {
        string table = (leadingAutoColor ? ";" : "") + ColorsTable;
        int first = leadingAutoColor ? 1 : 0;
        using var source = new EditorHost($@"{{\rtf1{{\colortbl{table}}}\pard\cf{first + 1}\highlight{first + 2} text}}");
        using var reloaded = new EditorHost(source.Editor.SaveRtf());

        AssertColor(Colors.Red, RunWithText(reloaded, "text").Foreground);
        AssertColor(Colors.Blue, RunWithText(reloaded, "text").Background);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void RawDocument_ColorTable_UsesTheActualColorTableIndex(bool leadingAutoColor)
    {
        string table = (leadingAutoColor ? ";" : "") + ColorsTable;
        int first = leadingAutoColor ? 1 : 0;
        var document = new RTFRawDocument();
        document.LoadRTFText($@"{{\rtf1{{\colortbl{table}}}\pard text}}");

        Assert.Equal(first + 3, document.ColorTable.Count);
        Assert.Equal(Colors.Black, document.ColorTable.GetColor(first, Colors.Transparent));
        Assert.Equal(Colors.Red, document.ColorTable.GetColor(first + 1, Colors.Transparent));
        Assert.Equal(Colors.Blue, document.ColorTable.GetColor(first + 2, Colors.Transparent));
        if (leadingAutoColor)
            Assert.Equal(Colors.Transparent, document.ColorTable.GetColor(0, Colors.Transparent));
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void WriteColorTable_KeepsImportedTablePositions(bool leadingAutoColor)
    {
        string table = (leadingAutoColor ? ";" : "") + ColorsTable;
        var document = new RTFDomDocument();
        document.LoadRTFText($@"{{\rtf1{{\colortbl{table}}}\pard text}}");
        using var output = new StringWriter();
        using (var writer = new RTFWriter(output))
        {
            document.ColorTable.Write(writer);
            writer.Flush();
        }

        var reloaded = new RTFDomDocument();
        reloaded.LoadRTFText($@"{{\rtf1{output}\pard text}}");

        Assert.Equal(document.ColorTable.Count, reloaded.ColorTable.Count);
        for (int index = 0; index < document.ColorTable.Count; index++)
            Assert.Equal(document.ColorTable[index], reloaded.ColorTable[index]);
    }

    [AvaloniaFact]
    public void RawDocument_EmptyAndDuplicateColors_KeepTheirTablePositions()
    {
        var document = new RTFRawDocument();
        document.LoadRTFText(@"{\rtf1{\colortbl;\red255\green0\blue0;;\red255\green0\blue0;\red0\green0\blue255;}\pard text}");

        Assert.Equal(5, document.ColorTable.Count);
        Assert.Equal(Colors.Red, document.ColorTable.GetColor(1, Colors.Black));
        Assert.Equal(Colors.Black, document.ColorTable.GetColor(2, Colors.Black));
        Assert.Equal(Colors.Red, document.ColorTable.GetColor(3, Colors.Black));
        Assert.Equal(Colors.Blue, document.ColorTable.GetColor(4, Colors.Black));
    }
}
