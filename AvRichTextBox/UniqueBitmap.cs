using Avalonia.Media.Imaging;

namespace AvRichTextBox;

public class UniqueBitmap(Bitmap ubmap, int w, int h, int cIndex)
{
   internal Bitmap uBitmap = ubmap;
    internal int maxWidth = w;
    internal int maxHeight = h;
    internal int consecutiveIndex = cIndex;

}

