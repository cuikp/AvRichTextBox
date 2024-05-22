using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Diagnostics;
using System.IO;
using AvColor = Avalonia.Media.Color;

namespace AvRichTextBox;

internal static class HelperMethods
{
   internal static AvColor ColorFromHex(string hex) { return AvColor.Parse(hex); }
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

   public static string WordHighlightColorValueToHexString(string hcv)
   {
      switch (hcv.ToLower())
      {
         case "darkyellow": return "#FF9ACD32";
         case "blueviolet": return "#FF931FDF";
         case "cyan": return "#FFE8EBF9";
         case "green": return "#FF98FB98";
         case "yellow": return "#FFFFE0C0";
         case "red": return "#FFFF4500";
         case "blue": return "#FFE3BFFF";
         case "black": case "ck": return "#FF000000";
         case "None": return "#FFFFFFFF";
         default: return "#FF000000";
      }
   }

   public static HighlightColorValues BrushToHighlightColorValue(IBrush br)
   {
      var hcv = new HighlightColorValues();
      switch (((SolidColorBrush)br).Color)
      {
         case AvColor col when col == Colors.Yellow | col == Colors.Wheat | col.ToString() == "#FFFFE0C0": { hcv = HighlightColorValues.Yellow; break; }
         case AvColor col when col == Colors.Red | col.ToString() == "#FFFF4500": { hcv = HighlightColorValues.Red; break; }
         case AvColor col when col == Colors.Cyan | col.ToString() == "#FFE8EBF9": { hcv = HighlightColorValues.Cyan; break; }
         case AvColor col when col == Colors.Green | col.ToString() == "#FF98FB98": { hcv = HighlightColorValues.Green; break; }
         case AvColor col when col == Colors.Blue | col.ToString() == "#FFE3BFFF": { hcv = HighlightColorValues.Blue; break; }
         case AvColor col when col == Colors.YellowGreen | col.ToString() == "#FF9ACD32": { hcv = HighlightColorValues.DarkYellow; break; }
         case AvColor col when col == Colors.White: { hcv = HighlightColorValues.White; break; }
         case AvColor col when col == Colors.Magenta: { hcv = HighlightColorValues.Magenta; break; }
         case AvColor col when col == Colors.LightGray: { hcv = HighlightColorValues.LightGray; break; }
         case AvColor col when col == Colors.DarkRed: { hcv = HighlightColorValues.DarkRed; break; }
         case AvColor col when col == Colors.DarkMagenta: { hcv = HighlightColorValues.DarkMagenta; break; }
         case AvColor col when col == Colors.DarkGreen: { hcv = HighlightColorValues.DarkGreen; break; }
         case AvColor col when col == Colors.DarkGray: { hcv = HighlightColorValues.DarkGray; break; }
         case AvColor col when col == Colors.DarkCyan: { hcv = HighlightColorValues.DarkCyan; break; }
         case AvColor col when col == Colors.DarkBlue: { hcv = HighlightColorValues.DarkBlue; break; }
         case AvColor col when col == Colors.BlueViolet: { hcv = HighlightColorValues.Blue; break; }
         case AvColor col when col == Colors.Black: { hcv = HighlightColorValues.Black; break; }
         case AvColor col when col == Colors.Transparent:  { hcv = HighlightColorValues.None; break; }
         default: hcv = HighlightColorValues.LightGray; break;
      }

      return hcv;
   }


}
