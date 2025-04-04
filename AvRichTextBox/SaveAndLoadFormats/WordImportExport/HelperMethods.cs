using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AvColor = Avalonia.Media.Color;

namespace AvRichTextBox;

internal static partial class HelperMethods
{
   internal static AvColor ColorFromHex(string hex) => AvColor.Parse(hex);
   internal static double TwipToPix(double unitTwip) => Convert.ToInt32(96.0 / 1440 * unitTwip);
   internal static double PixToTwip(double unitPix) => Convert.ToInt32(15 * unitPix);
   internal static double EMUToPix(double unitTwip) => Convert.ToInt32(96 / (double)914400 * unitTwip);
   internal static double PixToEMU(double unitPix) => Convert.ToInt32(914400 / (double)96 * unitPix);
   internal static double PointsToPixels(double pt) => Convert.ToDouble(pt * 96 / 72);
   internal static double InchesToPixels(double inch) => Convert.ToDouble(inch * 96);  
   internal static double PixelsToPoints(double px) => px * 72 / 96;


   internal static void ResizeAndSaveBitmap(Bitmap originalBitmap, int newWidth, int newHeight, Stream memoryStream)
   {
      var renderTarget = new RenderTargetBitmap(new PixelSize(newWidth, newHeight));

      using (var context = renderTarget.CreateDrawingContext())
      {
         context.DrawImage(originalBitmap, new Rect(0, 0, newWidth, newHeight));
      }

      renderTarget.Save(memoryStream);  // png by default
   }

   internal static string WordColorValueToHexString(string hcv, bool isBackground)
   {
      string hex = hcv.ToLower();

      string returnString = hex switch
      {
         "darkyellow" => "#FF9ACD32",
         "darkred" => "#FF8B000000",
         "blueviolet" => "#FF931FDF",
         "cyan" => "#FFE8EBF9",
         "green" => "#FF98FB98",
         "yellow" => "#FFFFE0C0",
         "red" => "#FFFF4500",
         "blue" => "#FFE3BFFF",
         "black" or "ck" => "#FF000000",
         "lightgray" => "#FFCCCCCC",
         "magenta" => "#FFFF00FF",
         "white" => "#FFFFFFFF",
         "none" => "#00FFFFFF",
         _ => string.Empty
      };

      if (returnString != string.Empty) return returnString;
      return hexColorRegex().Replace(hex, "") == "" ? hex : (isBackground ? "#FFFFFFFF" : "#FF000000"); // default white or black

   }

   internal static HighlightColorValues BrushToHighlightColorValue(IBrush br)
   {
      if (br is not SolidColorBrush solidColorBrush)
         return HighlightColorValues.None;

      var inputColor = solidColorBrush.Color;

      // Check for direct matches first
      var predefinedMatch = HighlightColors.FirstOrDefault(kv => kv.Value == inputColor);
      if (HighlightColors.ContainsKey(predefinedMatch.Key))
      {
         Debug.WriteLine("key = " + predefinedMatch.Key.ToString());
         return predefinedMatch.Key;
      }
         

      return FindClosestHighlightColor(inputColor);

   }

   private static readonly Dictionary<HighlightColorValues, AvColor> HighlightColors = new()
    {
        { HighlightColorValues.Yellow, Colors.Yellow },
        { HighlightColorValues.Red, Colors.Red },
        { HighlightColorValues.Cyan, Colors.Cyan },
        { HighlightColorValues.Green, Colors.Green },
        { HighlightColorValues.Blue, Colors.Blue },
        { HighlightColorValues.DarkYellow, Colors.YellowGreen },
        { HighlightColorValues.White, Colors.White },
        { HighlightColorValues.Magenta, Colors.Magenta },
        { HighlightColorValues.LightGray, Colors.LightGray },
        { HighlightColorValues.DarkRed, Colors.DarkRed },
        { HighlightColorValues.DarkMagenta, Colors.DarkMagenta },
        { HighlightColorValues.DarkGreen, Colors.DarkGreen },
        { HighlightColorValues.DarkGray, Colors.DarkGray },
        { HighlightColorValues.DarkCyan, Colors.DarkCyan },
        { HighlightColorValues.DarkBlue, Colors.DarkBlue },
        { HighlightColorValues.Black, Colors.Black },
        { HighlightColorValues.None, Colors.Transparent }
    };


   private static HighlightColorValues FindClosestHighlightColor(AvColor color)
   {
      return HighlightColors.OrderBy(kv => ColorDistance(color, kv.Value)).First().Key;
   }

   private static double ColorDistance(AvColor c1, AvColor c2)
   {
      int rDiff = c1.R - c2.R;
      int gDiff = c1.G - c2.G;
      int bDiff = c1.B - c2.B;
      return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
   }

   internal static int GetLanguageForChar(char c)
   {
      if (c is >= (char)0x4E00 and <= (char)0x9FFF or // CJK Unified Ideographs (Common Chinese, Japanese, Korean)
          >= (char)0x3400 and <= (char)0x4DBF)  // CJK Extension A (Rare Chinese characters)
         return 2052; // Simplified Chinese (zh-CN)
      else if (c is >= (char)0x3040 and <= (char)0x30FF) // Hiragana & Katakana (Japanese)
         return 1041; // Japanese (ja-JP)
      else if (c is >= (char)0xAC00 and <= (char)0xD7AF) // Hangul Syllables (Korean)
         return 1042; // Korean (ko-KR)
      else if (c is >= (char)0x3100 and <= (char)0x312F) // Bopomofo (Traditional Chinese phonetic)
         return 1028; // Traditional Chinese (zh-TW)
      else
         return 1033; // Default to English (en-US)
   }

   internal static bool IsCJKChar(char c) => GetLanguageForChar(c) is 2052 or 1041 or 1042 or 1028;
   [GeneratedRegex("[#0-9a-f]")]
   private static partial Regex hexColorRegex();
}
