using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace AvRichTextBox;

public partial class RichTextBox
{
    private static bool IsImageFile(IStorageFile file) => Path.GetExtension(file.Name).ToLowerInvariant() is
        ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp" or ".ico" or ".tif" or ".tiff";

    private static bool HasImageData(IDataTransfer data) => data.Contains(DataFormat.Bitmap) ||
        data.TryGetFiles()?.OfType<IStorageFile>().Any(IsImageFile) == true;

    private void RichTextBox_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = !IsReadOnly && e.DragEffects.HasFlag(DragDropEffects.Copy) && HasImageData(e.DataTransfer)
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void RichTextBox_Drop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        if (IsReadOnly || !e.DragEffects.HasFlag(DragDropEffects.Copy) || !HasImageData(e.DataTransfer) ||
            GetImageDropPosition(e.GetPosition(this)) is not int position)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = DragDropEffects.Copy;
        await InsertDroppedImagesAsync(e.DataTransfer, position);
    }

    internal int? GetImageDropPosition(Point position)
    {
        int? result = null;
        double nearestDistance = double.MaxValue;
        foreach (EditableParagraph visual in DocIC.GetVisualDescendants().OfType<EditableParagraph>())
        {
            if (!visual.IsVisible || visual.DataContext is not Paragraph paragraph ||
                this.TranslatePoint(position, visual) is not Point local)
                continue;

            double dx = Math.Max(0, Math.Max(-local.X, local.X - visual.Bounds.Width));
            double dy = Math.Max(0, Math.Max(-local.Y, local.Y - visual.Bounds.Height));
            double distance = dx * dx + dy * dy;
            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            int character = visual.TextLayout.HitTestPoint(local).TextPosition;
            result = paragraph.StartInDoc + Math.Clamp(character, 0, Math.Max(0, paragraph.TextLength - 1));
        }
        return result;
    }

    internal async Task<bool> InsertDroppedImagesAsync(IDataTransfer data, int position)
    {
        FlowDocument document = FlowDoc;
        if (IsReadOnly || position < 0 || position > document.DocEndPoint)
            return false;

        using var target = new TextRange(document, position, position);
        List<(Bitmap? Bitmap, IStorageFile? File)> sources = [];
        List<Bitmap> ownedBitmaps = [];
        bool inserted = false;
        try
        {
            // Read the transfer before the drop event returns. Copy bitmaps so the document owns their lifetime.
            foreach (IDataTransferItem item in data.Items)
            {
                if (item.TryGetBitmap() is Bitmap bitmap)
                {
                    using var stream = new MemoryStream();
                    bitmap.Save(stream, PngBitmapEncoderOptions.Default);
                    stream.Position = 0;
                    var copy = new Bitmap(stream);
                    ownedBitmaps.Add(copy);
                    sources.Add((copy, null));
                }
                else if (item.TryGetFile() is IStorageFile file && IsImageFile(file))
                    sources.Add((null, file));
            }

            List<Bitmap> images = [];
            foreach (var source in sources)
            {
                if (source.Bitmap is Bitmap bitmap)
                {
                    images.Add(bitmap);
                    continue;
                }

                try
                {
                    await using Stream stream = await source.File!.OpenReadAsync();
                    var image = new Bitmap(stream);
                    ownedBitmaps.Add(image);
                    images.Add(image);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
                {
                    Debug.WriteLine($"Image drop could not read {source.File!.Name}: {ex.Message}");
                }
            }

            // A file read can finish after the editor becomes read-only or its document is replaced or cleared.
            if (!IsReadOnly && ReferenceEquals(document, FlowDoc) && document.TextRanges.Contains(target))
                inserted = document.InsertImagesAt(target.Start, images);

            return inserted;
        }
        finally
        {
            if (!inserted)
                foreach (Bitmap bitmap in ownedBitmaps)
                    bitmap.Dispose();
        }
    }
}
