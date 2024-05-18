using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvRichTextBox;

public class UniqueBitmap
{
    public UniqueBitmap(Bitmap ubmap, int w, int h, int cIndex) { uBitmap = ubmap; maxWidth = w; maxHeight = h; consecutiveIndex = cIndex; }

    internal Bitmap uBitmap;
    internal int maxWidth;
    internal int maxHeight;
    internal int consecutiveIndex;

}

