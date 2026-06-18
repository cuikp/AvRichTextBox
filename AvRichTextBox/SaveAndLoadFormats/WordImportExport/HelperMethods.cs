using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;
using AvColor = Avalonia.Media.Color;

namespace AvRichTextBox;

internal static partial class HelperMethods
{
   internal static AvColor ColorFromHex(string hex) => AvColor.Parse(hex);
   internal static double TwipToPix(double unitTwip) => 96.0 / 1440D * unitTwip;
   internal static double PixToTwip(double unitPix) => 15D * unitPix;
   internal static double EMUToPix(double unitTwip) => 96 / (double)914400 * unitTwip;
   internal static double PixToEMU(double unitPix) => 914400 / (double)96 * unitPix;
   internal static double PointsToPixels(double pt) => pt * 96D / 72D;
   internal static double InchesToPixels(double inch) => inch * 96D;  
   internal static double PixelsToPoints(double px) => px * 72D / 96D;
   internal static double TwipToDip(double twips) => twips * (96.0 / 1440.0);
   internal static double DipToTwip(double dips) => dips * (1440.0 / 96.0);

   internal static char GetWindows1252Char(byte ansiByte)
   {
      switch (ansiByte)
      {
         case 0x80: return '\u20ac';
         case 0x82: return '\u201a';
         case 0x83: return '\u0192';
         case 0x84: return '\u201e';
         case 0x85: return '\u2026';
         case 0x86: return '\u2020';
         case 0x87: return '\u2021';
         case 0x88: return '\u02c6';
         case 0x89: return '\u2030';
         case 0x8a: return '\u0160';
         case 0x8b: return '\u2039';
         case 0x8c: return '\u0152';
         case 0x8e: return '\u017d';
         case 0x91: return '\u2018';
         case 0x92: return '\u2019';
         case 0x93: return '\u201c';
         case 0x94: return '\u201d';
         case 0x95: return '\u2022';
         case 0x96: return '\u2013';
         case 0x97: return '\u2014';
         case 0x98: return '\u02dc';
         case 0x99: return '\u2122';
         case 0x9a: return '\u0161';
         case 0x9b: return '\u203a';
         case 0x9c: return '\u0153';
         case 0x9e: return '\u017e';
         case 0x9f: return '\u0178';
      }

      return (char)ansiByte;
   }

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
         "none" => string.Empty,
         _ => string.Empty
      };

      if (returnString != string.Empty) return returnString;
      //return hexColorRegex().Replace(hex, "") == "" ? ("#" + hex) : (isBackground ? "#FFFFFFFF" : "#FF000000"); // default white or black
      //return hexColorRegex().Replace(hex, "") == "" ? (hex) : (isBackground ? "#FFFFFFFF" : "#FF000000"); // default white or black
      return hexColorRegex().Replace(hex, "") == "" ? (hex) : (isBackground ? "" : "#FF000000"); // default empty or black

   }

   internal static HighlightColorValues BrushToHighlightColorValue(IBrush br)
   {
      if (br is not SolidColorBrush solidColorBrush || br == Brushes.Transparent)
         return HighlightColorValues.None;

      var inputColor = solidColorBrush.Color;

      // Check for direct matches first
      var predefinedMatch = HighlightColors.FirstOrDefault(kv => kv.Value == inputColor);
      if (HighlightColors.ContainsKey(predefinedMatch.Key))
      {
         //Debug.WriteLine("key = " + predefinedMatch.Key.ToString());
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

   internal static string ToOpenXmlColor(AvColor color) => $"{color.R:X2}{color.G:X2}{color.B:X2}";

   internal static ISolidColorBrush FromOpenXmlColor(string openxmlhex)
   {
      BrushConverter BConverter = new();
      ImmutableSolidColorBrush? iSCB = (ImmutableSolidColorBrush)BConverter.ConvertFromString("#" + openxmlhex)!;
      return new SolidColorBrush(iSCB.Color);
      
   }

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
