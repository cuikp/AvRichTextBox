using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Xunit;

namespace AvRichTextBox.Tests;

public class TableEditingTests
{
    private static EditorHost CreateDocument()
    {
        var host = new EditorHost();
        host.Editor.LoadXamlString(
            "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
            "<Paragraph><Bold>XY</Bold></Paragraph><Paragraph>ZW</Paragraph>" +
            "<Table><Table.Columns><TableColumn Width=\"150\"/><TableColumn Width=\"150\"/></Table.Columns>" +
            "<TableRowGroup><TableRow><TableCell><Paragraph>abcd</Paragraph><Paragraph>efgh</Paragraph></TableCell>" +
            "<TableCell><Paragraph>neighbor</Paragraph></TableCell></TableRow></TableRowGroup></Table>" +
            "<Paragraph>tail</Paragraph></Section>");
        EditorHost.Pump();
        return host;
    }

    private static Cell TargetCell(EditorHost host) => Assert.Single(host.Document.Blocks.OfType<Table>()).Cells[0];

    [AvaloniaTheory]
    [InlineData(0, 0)]
    [InlineData(0, 2)]
    [InlineData(0, 4)]
    [InlineData(1, 0)]
    [InlineData(1, 2)]
    [InlineData(1, 4)]
    public void PasteInCell_RepeatedUndo_RestoresTheDocument(int paragraphIndex, int offset)
    {
        using var host = CreateDocument();
        string original = host.Editor.SaveXamlString();
        host.Select(0, 2);
        host.Press(Key.C, RawInputModifiers.Control);
        int start = TargetCell(host).CellBlocks[paragraphIndex].StartInDoc + offset;
        string paragraphText = paragraphIndex == 0 ? "abcd" : "efgh";

        for (int attempt = 0; attempt < 3; attempt++)
        {
            host.Select(start, 0);
            host.Press(Key.V, RawInputModifiers.Control);
            Assert.Equal(paragraphText.Insert(offset, "XY"),
                TargetCell(host).CellBlocks[paragraphIndex].Text.TrimEnd('\r', '\a'));
            host.Press(Key.Z, RawInputModifiers.Control);
            Assert.Equal(original, host.Editor.SaveXamlString());
        }
    }

    [AvaloniaTheory]
    [InlineData(0, 2)]
    [InlineData(1, 3)]
    public void PasteMultipleParagraphsInCell_UndoRedo_RestoresBothStates(int offset, int length)
    {
        using var host = CreateDocument();
        string original = host.Editor.SaveXamlString();
        host.Select(0, 5);
        host.Press(Key.C, RawInputModifiers.Control);
        host.Select(TargetCell(host).CellBlocks[0].StartInDoc + offset, length);
        host.Press(Key.V, RawInputModifiers.Control);
        string pasted = host.Editor.SaveXamlString();
        Assert.Equal(3, TargetCell(host).CellBlocks.Count);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            host.Press(Key.Z, RawInputModifiers.Control);
            Assert.Equal(original, host.Editor.SaveXamlString());
            host.Press(Key.Y, RawInputModifiers.Control);
            Assert.Equal(pasted, host.Editor.SaveXamlString());
        }
    }

    [AvaloniaFact]
    public void DeleteInCell_UndoThenPaste_RestoresTheDocument()
    {
        using var host = CreateDocument();
        string original = host.Editor.SaveXamlString();
        host.Select(0, 5);
        host.Press(Key.C, RawInputModifiers.Control);
        int start = TargetCell(host).CellBlocks[0].StartInDoc + 1;
        host.Select(start, 2);
        host.Press(Key.Delete);
        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Equal(original, host.Editor.SaveXamlString());

        host.Select(start, 0);
        host.Press(Key.V, RawInputModifiers.Control);
        Assert.Equal(3, TargetCell(host).CellBlocks.Count);
        Assert.Equal(host.Document.GetAllParagraphs, host.Document.AllParagraphs);
        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Equal(original, host.Editor.SaveXamlString());
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplaceAcrossCellParagraphs_UndoRedo_RestoresBothStates(bool paste)
    {
        using var host = CreateDocument();
        string original = host.Editor.SaveXamlString();
        host.Select(0, 2);
        host.Press(Key.C, RawInputModifiers.Control);
        host.Select(TargetCell(host).CellBlocks[0].StartInDoc + 1, 7);
        host.Press(paste ? Key.V : Key.Delete, paste ? RawInputModifiers.Control : RawInputModifiers.None);
        string edited = host.Editor.SaveXamlString();

        for (int attempt = 0; attempt < 3; attempt++)
        {
            host.Press(Key.Z, RawInputModifiers.Control);
            Assert.Equal(original, host.Editor.SaveXamlString());
            host.Press(Key.Y, RawInputModifiers.Control);
            Assert.Equal(edited, host.Editor.SaveXamlString());
        }
    }

    [AvaloniaFact]
    public async Task DemoDocument_PasteAndUndoInEachCell_RestoresTheDocument()
    {
        using var host = new EditorHost();
        string path = Path.Combine(AppContext.BaseDirectory, "TestFiles", "TestDocumentXamlPackage.xamlp");
        Assert.True(await XamlConversions.LoadXamlPackage(path, host.Document));
        host.Document.InitializeDocument();
        EditorHost.Pump();
        string original = host.Editor.SaveXamlString();
        host.Select(0, 5);
        host.Press(Key.C, RawInputModifiers.Control);
        var table = Assert.Single(host.Document.Blocks.OfType<Table>());

        for (int cellIndex = 0; cellIndex < table.Cells.Count; cellIndex++)
        {
            foreach (var paragraph in table.Cells[cellIndex].CellBlocks.OfType<Paragraph>().ToList())
            {
                host.Select(paragraph.StartInDoc + 1, 0);
                host.Press(Key.V, RawInputModifiers.Control);
                host.Press(Key.Z, RawInputModifiers.Control);
                Assert.Equal(original, host.Editor.SaveXamlString());
            }
        }

        Cell multiParagraphCell = table.Cells.First(c => c.CellBlocks.Count > 1);
        int start = multiParagraphCell.CellBlocks[0].StartInDoc + 1;
        host.Select(start, multiParagraphCell.CellBlocks[1].StartInDoc - start);
        host.Press(Key.V, RawInputModifiers.Control);
        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Equal(original, host.Editor.SaveXamlString());
    }

    [AvaloniaFact]
    public void CopyWithinCell_RepeatedPasteUndo_RestoresTheDocument()
    {
        using var host = CreateDocument();
        string original = host.Editor.SaveXamlString();
        int start = TargetCell(host).CellBlocks[0].StartInDoc;
        host.Select(start, 2);
        host.Press(Key.C, RawInputModifiers.Control);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            host.Select(start + 2, 0);
            host.Press(Key.V, RawInputModifiers.Control);
            Assert.Equal("ababcd", TargetCell(host).CellBlocks[0].Text.TrimEnd('\r', '\a'));
            host.Press(Key.Z, RawInputModifiers.Control);
            Assert.Equal(original, host.Editor.SaveXamlString());
        }
    }

    [AvaloniaFact]
    public void ConsecutiveCellPastes_UndoRedo_RestoresEachState()
    {
        using var host = CreateDocument();
        string original = host.Editor.SaveXamlString();
        host.Select(0, 2);
        host.Press(Key.C, RawInputModifiers.Control);
        host.Select(TargetCell(host).CellBlocks[0].StartInDoc + 1, 0);
        host.Press(Key.V, RawInputModifiers.Control);
        string firstPaste = host.Editor.SaveXamlString();
        host.Select(TargetCell(host).CellBlocks[1].StartInDoc + 2, 0);
        host.Press(Key.V, RawInputModifiers.Control);
        string secondPaste = host.Editor.SaveXamlString();

        for (int attempt = 0; attempt < 3; attempt++)
        {
            host.Press(Key.Z, RawInputModifiers.Control);
            Assert.Equal(firstPaste, host.Editor.SaveXamlString());
            host.Press(Key.Z, RawInputModifiers.Control);
            Assert.Equal(original, host.Editor.SaveXamlString());
            host.Press(Key.Y, RawInputModifiers.Control);
            Assert.Equal(firstPaste, host.Editor.SaveXamlString());
            host.Press(Key.Y, RawInputModifiers.Control);
            Assert.Equal(secondPaste, host.Editor.SaveXamlString());
        }
    }

    [AvaloniaTheory]
    [InlineData(0, 0)]
    [InlineData(0, 2)]
    [InlineData(1, 4)]
    [InlineData(2, 5)]
    public void PasteOutsideTable_RepeatedUndoRedo_RestoresBothStates(int offset, int length)
    {
        using var host = new EditorHost(@"{\rtf1\pard abcd\par efgh\par tail}");
        string original = host.Editor.SaveXamlString();
        host.Select(0, 5);
        host.Press(Key.C, RawInputModifiers.Control);
        host.Select(offset, length);
        host.Press(Key.V, RawInputModifiers.Control);
        string pasted = host.Editor.SaveXamlString();

        for (int attempt = 0; attempt < 3; attempt++)
        {
            host.Press(Key.Z, RawInputModifiers.Control);
            Assert.Equal(original, host.Editor.SaveXamlString());
            host.Press(Key.Y, RawInputModifiers.Control);
            Assert.Equal(pasted, host.Editor.SaveXamlString());
        }
    }

    [AvaloniaFact]
    public void PasteIntoEmptyCell_UndoRedo_KeepsTheCell()
    {
        using var host = CreateDocument();
        host.Select(TargetCell(host).CellBlocks[0].StartInDoc, 4);
        host.Press(Key.Delete);
        string original = host.Editor.SaveXamlString();
        host.Select(0, 2);
        host.Press(Key.C, RawInputModifiers.Control);
        host.Select(TargetCell(host).CellBlocks[0].StartInDoc, 0);
        host.Press(Key.V, RawInputModifiers.Control);
        string pasted = host.Editor.SaveXamlString();
        Assert.Equal("XY", TargetCell(host).CellBlocks[0].Text.TrimEnd('\r', '\a'));

        for (int attempt = 0; attempt < 3; attempt++)
        {
            host.Press(Key.Z, RawInputModifiers.Control);
            Assert.Equal(original, host.Editor.SaveXamlString());
            host.Press(Key.Y, RawInputModifiers.Control);
            Assert.Equal(pasted, host.Editor.SaveXamlString());
        }
    }

    [AvaloniaFact]
    public void PasteEndingAtNextCellParagraphStart_UndoRestoresTheDocument()
    {
        using var host = CreateDocument();
        string original = host.Editor.SaveXamlString();
        host.Select(0, 2);
        host.Press(Key.C, RawInputModifiers.Control);
        host.Select(TargetCell(host).CellBlocks[0].StartInDoc + 1, 4);
        host.Press(Key.V, RawInputModifiers.Control);
        Assert.StartsWith("aXY", TargetCell(host).GetText);
        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Equal(original, host.Editor.SaveXamlString());
    }

    // The selection "bcd" plus the paragraph end reaches the start of the next paragraph; the paste replaces it
    // and joins both paragraphs.
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PasteOverSelectionEndingAtNextParagraphStart_InsertsTheContent(bool plainText)
    {
        using var host = new EditorHost(@"{\rtf1\pard abcd\par efgh\par tail}");
        string original = host.Editor.SaveXamlString();
        if (plainText)
        {
            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText("ab"));
            await host.Window.Clipboard!.SetDataAsync(data);
        }
        else
        {
            host.Select(0, 2);
            host.Press(Key.C, RawInputModifiers.Control);
        }

        host.Select(1, 4);
        host.Press(Key.V, RawInputModifiers.Control);
        Assert.Equal("aabefgh\rtail", host.Document.Text.TrimEnd('\r'));

        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Equal(original, host.Editor.SaveXamlString());
    }

    private static async Task<EditorHost> LoadDemoDocument()
    {
        var host = new EditorHost();
        string path = Path.Combine(AppContext.BaseDirectory, "TestFiles", "TestDocumentXamlPackage.xamlp");
        Assert.True(await XamlConversions.LoadXamlPackage(path, host.Document));
        host.Document.InitializeDocument();
        EditorHost.Pump();
        return host;
    }

    private static string FirstLineText(EditorHost host) => ((Paragraph)host.Document.Blocks[0]).Text.TrimEnd('\r');

    // Home and Shift+End select the whole text of the title line. The paragraph end mark stays outside the selection,
    // so the title paragraph must stay and take the pasted content, and the empty paragraph after it must stay too.
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PasteOverWholeLineSelectedWithShiftEnd_ReplacesTheLineText(bool plainText)
    {
        using var host = await LoadDemoDocument();
        string original = host.Editor.SaveXamlString();
        int blockCount = host.Document.Blocks.Count;
        if (plainText)
        {
            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText("The"));
            await host.Window.Clipboard!.SetDataAsync(data);
        }
        else
        {
            var body = host.Document.Blocks.OfType<Paragraph>().First(paragraph => paragraph.Text.StartsWith("The cat"));
            host.Select(body.StartInDoc, 3);
            host.Press(Key.C, RawInputModifiers.Control);
        }

        host.Select(0, 0);
        host.Press(Key.Home);
        host.Press(Key.End, RawInputModifiers.Shift);
        Assert.Equal("The (wonderful, blunderful) cat", host.Document.Selection.Text);
        host.Press(Key.V, RawInputModifiers.Control);
        string pasted = host.Editor.SaveXamlString();
        Assert.Equal("The", FirstLineText(host));
        Assert.Equal(blockCount, host.Document.Blocks.Count);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            host.Press(Key.Z, RawInputModifiers.Control);
            Assert.Equal(original, host.Editor.SaveXamlString());
            host.Press(Key.Y, RawInputModifiers.Control);
            Assert.Equal(pasted, host.Editor.SaveXamlString());
        }
    }

    [AvaloniaFact]
    public async Task DeleteWholeLineSelectedWithShiftEnd_KeepsTheEmptyParagraph()
    {
        using var host = await LoadDemoDocument();
        string original = host.Editor.SaveXamlString();
        int blockCount = host.Document.Blocks.Count;

        host.Select(0, 0);
        host.Press(Key.Home);
        host.Press(Key.End, RawInputModifiers.Shift);
        host.Press(Key.Delete);
        string deleted = host.Editor.SaveXamlString();
        Assert.Equal("", FirstLineText(host));
        Assert.Equal(blockCount, host.Document.Blocks.Count);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            host.Press(Key.Z, RawInputModifiers.Control);
            Assert.Equal(original, host.Editor.SaveXamlString());
            host.Press(Key.Y, RawInputModifiers.Control);
            Assert.Equal(deleted, host.Editor.SaveXamlString());
        }
    }

    [AvaloniaTheory]
    [InlineData(0, 0)]
    [InlineData(1, 2)]
    public async Task PastePlainTextInCell_UndoRedo_RestoresBothStates(int offset, int length)
    {
        using var host = CreateDocument();
        string original = host.Editor.SaveXamlString();
        var data = new DataTransfer();
        data.Add(DataTransferItem.CreateText("XY"));
        await host.Window.Clipboard!.SetDataAsync(data);
        host.Select(TargetCell(host).CellBlocks[0].StartInDoc + offset, length);
        host.Press(Key.V, RawInputModifiers.Control);
        string pasted = host.Editor.SaveXamlString();
        Assert.Equal("abcd".Remove(offset, length).Insert(offset, "XY"),
            TargetCell(host).CellBlocks[0].Text.TrimEnd('\r', '\a'));

        for (int attempt = 0; attempt < 3; attempt++)
        {
            host.Press(Key.Z, RawInputModifiers.Control);
            Assert.Equal(original, host.Editor.SaveXamlString());
            host.Press(Key.Y, RawInputModifiers.Control);
            Assert.Equal(pasted, host.Editor.SaveXamlString());
        }
    }
}
