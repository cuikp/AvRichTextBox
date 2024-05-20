using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.IO;

namespace AvRichTextBox;

internal static class HelperMethods
{
   internal static Color ColorFromHex(string hex) { return Color.Parse(hex); }
   internal static double TwipToPix(double unitTwip) { return Convert.ToInt32(96.0 / 1440 * unitTwip); }
   internal static double PixToTwip(double unitPix) { return Convert.ToInt32(15 * unitPix); }
   internal static double EMUToPix(double unitTwip) { return Convert.ToInt32(96 / (double)914400 * unitTwip); }
   internal static double PixToEMU(double unitPix) { return Convert.ToInt32(914400 / (double)96 * unitPix); }
   internal static double PointsToPixels(double pt) { return Convert.ToDouble(pt * 96 / 72); }
   internal static double InchesToPixels(double inch) { return Convert.ToDouble(inch * 96); }
   internal static double PixelsToPoints(double px) { return px * 72 / 96; }


   public static void ResizeAndSaveBitmap(Bitmap originalBitmap, int newWidth, int newHeight, Stream memoryStream)
   {
      var renderTarget = new RenderTargetBitmap(new PixelSize(newWidth, newHeight));

      using (var context = renderTarget.CreateDrawingContext())
      {
         context.DrawImage(originalBitmap, new Rect(0, 0, newWidth, newHeight));
      }

      renderTarget.Save(memoryStream);  // png by default
   }

   public static Brush HighlightColorValueToBrush(string hcv)
   {
      switch (hcv.ToLower())
      {
         case "darkyellow": return new SolidColorBrush(ColorFromHex("#FF9ACD32"));
         case "cyan": return new SolidColorBrush(ColorFromHex("#FFE8EBF9"));
         case "green": return new SolidColorBrush(ColorFromHex("#FF98FB98"));
         case "yellow": return new SolidColorBrush(ColorFromHex("#FFFFE0C0"));
         case "red": return new SolidColorBrush(ColorFromHex("#FFFF4500"));
         case "blue": return new SolidColorBrush(ColorFromHex("#FFE3BFFF"));
         case "black": return new SolidColorBrush(ColorFromHex("#FFaaaaaa"));
         case "None": return new SolidColorBrush(Colors.White);
         default: return new SolidColorBrush(Colors.Black);
      }

      //return (Brush)new BrushConverter().ConvertFromString(hcv);

   }

}
