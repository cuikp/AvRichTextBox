using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Xunit;

namespace AvRichTextBox.Tests;

public class ImageDropTests
{
    private static WriteableBitmap Bitmap(int width = 2, int height = 3) =>
        new(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

    private static async Task<IStorageFile> File(EditorHost host, string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "ImageDropFiles", name);
        return Assert.IsAssignableFrom<IStorageFile>(await host.Window.StorageProvider.TryGetFileFromPathAsync(new Uri(path)));
    }

    private static void AssertFileClosed(IStorageFile file)
    {
        using var exclusive = new FileStream(file.Path.LocalPath, FileMode.Open, FileAccess.Read, FileShare.None);
        Assert.True(exclusive.Length > 0);
    }

    private static DataTransfer Transfer(params DataTransferItem[] items)
    {
        var data = new DataTransfer();
        foreach (var item in items)
            data.Add(item);
        return data;
    }

    private static string[] ParagraphTexts(IEnumerable<Block> blocks) =>
        blocks.OfType<Paragraph>().Select(p => p.Text.TrimEnd('\r', '\a')).ToArray();

    private static Image[] Images(IEnumerable<Block> blocks) =>
        blocks.OfType<Paragraph>().SelectMany(p => p.Inlines).OfType<EditableInlineUIContainer>()
            .Select(inline => Assert.IsType<Image>(inline.GetChild())).ToArray();

    [AvaloniaFact]
    public async Task DropBitmap_InsertsAtDropPositionWithoutReplacingTheSelection()
    {
        using var host = new EditorHost(@"{\rtf1\pard abcd}");
        host.Select(0, 4);
        using var bitmap = Bitmap();
        var data = Transfer(DataTransferItem.Create(DataFormat.Bitmap, bitmap));

        Assert.True(await host.Editor.InsertDroppedImagesAsync(data, 2));
        EditorHost.Pump();

        Assert.Equal(["ab", "@", "cd"], ParagraphTexts(host.Document.Blocks));
        var image = Assert.Single(Images(host.Document.Blocks));
        Assert.NotSame(bitmap, image.Source);
        Assert.Equal(bitmap.PixelSize, Assert.IsAssignableFrom<Avalonia.Media.Imaging.Bitmap>(image.Source).PixelSize);
        Assert.Single(host.Document.Undos);
        Assert.Equal(host.Document.Blocks.Last().StartInDoc, host.Document.Selection.Start);
    }

    [AvaloniaFact]
    public async Task DroppedBitmap_RemainsUsableAfterTheSourceIsDisposed()
    {
        using var host = new EditorHost(@"{\rtf1\pard abcd}");
        using (var bitmap = Bitmap())
            Assert.True(await host.Editor.InsertDroppedImagesAsync(Transfer(DataTransferItem.Create(DataFormat.Bitmap, bitmap)), 2));

        using var reloaded = new EditorHost(host.Editor.SaveRtf());

        Image image = Assert.Single(Images(reloaded.Document.Blocks));
        Assert.Equal(new PixelSize(2, 3), Assert.IsAssignableFrom<Avalonia.Media.Imaging.Bitmap>(image.Source).PixelSize);
    }

    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task DropBitmap_AtParagraphBoundary_KeepsAnEditableParagraphAfterTheImage(int position)
    {
        using var host = new EditorHost(@"{\rtf1\pard abcd}");
        using var bitmap = Bitmap();

        Assert.True(await host.Editor.InsertDroppedImagesAsync(Transfer(DataTransferItem.Create(DataFormat.Bitmap, bitmap)), position));
        EditorHost.Pump();

        Assert.Equal(position == 0 ? ["@", "abcd"] : ["abcd", "@", ""], ParagraphTexts(host.Document.Blocks));
        host.Type("x");
        Assert.Contains("x", host.Document.Blocks.Last().Text);
    }

    [AvaloniaFact]
    public async Task DropBitmap_IntoAnEmptyDocument_CanBeUndoneAndRedone()
    {
        using var host = new EditorHost();
        using var bitmap = Bitmap();
        Assert.True(await host.Editor.InsertDroppedImagesAsync(Transfer(DataTransferItem.Create(DataFormat.Bitmap, bitmap)), 0));
        EditorHost.Pump();
        Assert.Equal(["@", ""], ParagraphTexts(host.Document.Blocks));

        host.Document.Undo();
        EditorHost.Pump();
        Assert.Equal([""], ParagraphTexts(host.Document.Blocks));

        host.Document.Redo();
        EditorHost.Pump();
        Assert.Equal(["@", ""], ParagraphTexts(host.Document.Blocks));
    }

    [AvaloniaFact]
    public async Task DropFiles_KeepsImageOrderClosesStreamsAndUsesOneUndoStep()
    {
        using var host = new EditorHost(@"{\rtf1\pard abcd}");
        var first = await File(host, "first.png");
        var invalid = await File(host, "invalid.png");
        var last = await File(host, "last.png");
        var data = Transfer(DataTransferItem.CreateFile(first), DataTransferItem.CreateFile(invalid), DataTransferItem.CreateFile(last));

        Assert.True(await host.Editor.InsertDroppedImagesAsync(data, 2));
        EditorHost.Pump();
        Assert.Equal(["ab", "@", "@", "cd"], ParagraphTexts(host.Document.Blocks));
        Assert.Equal([new PixelSize(2, 3), new PixelSize(4, 5)], Images(host.Document.Blocks)
            .Select(image => Assert.IsAssignableFrom<Avalonia.Media.Imaging.Bitmap>(image.Source).PixelSize));
        AssertFileClosed(first);
        AssertFileClosed(invalid);
        AssertFileClosed(last);
        Assert.Single(host.Document.Undos);

        for (int attempt = 0; attempt < 2; attempt++)
        {
            host.Document.Undo();
            EditorHost.Pump();
            Assert.Equal(["abcd"], ParagraphTexts(host.Document.Blocks));
            host.Document.Redo();
            EditorHost.Pump();
            Assert.Equal(["ab", "@", "@", "cd"], ParagraphTexts(host.Document.Blocks));
        }
    }

    [AvaloniaFact]
    public async Task DropBitmap_IntoTableCell_StaysInThatCellAndCanBeUndone()
    {
        using var host = new EditorHost(@"{\rtf1\trowd\cellx2000\cellx4000\pard\intbl abcd\cell next\cell\row}");
        var table = Assert.Single(host.Document.Blocks.OfType<Table>());
        var cell = table.Cells.First();
        int position = cell.CellBlocks[0].StartInDoc + 2;
        using var bitmap = Bitmap();

        Assert.True(await host.Editor.InsertDroppedImagesAsync(Transfer(DataTransferItem.Create(DataFormat.Bitmap, bitmap)), position));
        EditorHost.Pump();
        Assert.Single(host.Document.Blocks);
        Assert.Equal(["ab", "@", "cd"], ParagraphTexts(cell.CellBlocks));
        Assert.Equal(["next"], ParagraphTexts(table.Cells.Last().CellBlocks));

        host.Document.Undo();
        EditorHost.Pump();
        Assert.Equal(["abcd"], ParagraphTexts(cell.CellBlocks));
        host.Document.Redo();
        EditorHost.Pump();
        Assert.Equal(["ab", "@", "cd"], ParagraphTexts(cell.CellBlocks));
    }

    [AvaloniaFact]
    public async Task DropBitmap_BeforeAnExistingImage_KeepsBothImages()
    {
        using var host = new EditorHost();
        using var first = Bitmap(2, 3);
        using var second = Bitmap(4, 5);
        Assert.True(await host.Editor.InsertDroppedImagesAsync(Transfer(DataTransferItem.Create(DataFormat.Bitmap, first)), 0));
        Assert.True(await host.Editor.InsertDroppedImagesAsync(Transfer(DataTransferItem.Create(DataFormat.Bitmap, second)), 0));
        EditorHost.Pump();

        Assert.Equal(["@", "@", ""], ParagraphTexts(host.Document.Blocks));
        Assert.Equal([new PixelSize(4, 5), new PixelSize(2, 3)], Images(host.Document.Blocks)
            .Select(image => Assert.IsAssignableFrom<Avalonia.Media.Imaging.Bitmap>(image.Source).PixelSize));
    }

    [AvaloniaFact]
    public async Task DropIntoReadOnlyEditor_DoesNotChangeTheDocument()
    {
        using var host = new EditorHost(@"{\rtf1\pard abcd}");
        host.Editor.IsReadOnly = true;
        var file = await File(host, "first.png");

        Assert.False(await host.Editor.InsertDroppedImagesAsync(Transfer(DataTransferItem.CreateFile(file)), 2));
        AssertFileClosed(file);
        Assert.Equal(["abcd"], ParagraphTexts(host.Document.Blocks));
        Assert.Empty(host.Document.Undos);
    }

    [AvaloniaTheory]
    [InlineData("notes.txt")]
    [InlineData("invalid.png")]
    public async Task DropWithoutReadableImages_LeavesDocumentAndUndoUnchanged(string name)
    {
        using var host = new EditorHost(@"{\rtf1\pard abcd}");
        host.Select(1, 2);
        var file = await File(host, name);

        Assert.False(await host.Editor.InsertDroppedImagesAsync(Transfer(DataTransferItem.CreateFile(file)), 2));
        Assert.Equal(["abcd"], ParagraphTexts(host.Document.Blocks));
        Assert.Equal(1, host.Document.Selection.Start);
        Assert.Equal(2, host.Document.Selection.Length);
        Assert.Empty(host.Document.Undos);
    }

    [AvaloniaFact]
    public async Task DropWhileReadingData_TracksTheDropPositionThroughTextEdits()
    {
        using var host = new EditorHost(@"{\rtf1\pard abcd}");
        using var bitmap = Bitmap();
        var data = Transfer(DataTransferItem.Create(DataFormat.Bitmap, () =>
        {
            host.Select(0, 0);
            host.Type("x");
            return bitmap;
        }));

        Assert.True(await host.Editor.InsertDroppedImagesAsync(data, 2));
        EditorHost.Pump();
        Assert.Equal(["xab", "@", "cd"], ParagraphTexts(host.Document.Blocks));
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DropWhileReadingData_DoesNotWriteIntoAReplacedOrReadOnlyDocument(bool makeReadOnly)
    {
        using var host = new EditorHost(@"{\rtf1\pard abcd}");
        using var bitmap = Bitmap();
        var data = Transfer(DataTransferItem.Create(DataFormat.Bitmap, () =>
        {
            if (makeReadOnly)
                host.Editor.IsReadOnly = true;
            else
                host.Editor.LoadRtf(@"{\rtf1\pard replacement}");
            return bitmap;
        }));

        Assert.False(await host.Editor.InsertDroppedImagesAsync(data, 2));
        Assert.Equal([makeReadOnly ? "abcd" : "replacement"], ParagraphTexts(host.Document.Blocks));
        Assert.Empty(Images(host.Document.Blocks));
    }

    [AvaloniaTheory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public void DropPosition_UsesThePointInTheTargetParagraphAtAnyZoom(double zoom)
    {
        using var host = new EditorHost(@"{\rtf1\pard first\par target}");
        host.Editor.Zoom = zoom;
        EditorHost.Pump();
        Paragraph target = host.Document.Blocks.OfType<Paragraph>().Last();
        EditableParagraph visual = Assert.Single(host.Editor.GetVisualDescendants().OfType<EditableParagraph>(), p => p.DataContext == target);
        Rect character = visual.TextLayout.HitTestTextPosition(2);
        Point point = visual.TranslatePoint(new Point(character.X + 0.1, character.Y + character.Height / 2), host.Editor)!.Value;

        Assert.Equal(target.StartInDoc + 2, host.Editor.GetImageDropPosition(point));
        Assert.True(DragDrop.GetAllowDrop(host.Editor));
    }

}
