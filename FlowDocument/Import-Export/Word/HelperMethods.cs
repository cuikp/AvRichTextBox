using System;

namespace AvRichTextBox;

internal static class HelperMethods
{
   //internal static Color ColorFromHex(string hex) { return (Color)ColorConverter.ConvertFromString(hex); }
   internal static double TwipToPix(double unitTwip) { return Convert.ToInt32(96.0 / 1440 * unitTwip); }
   internal static double PixToTwip(double unitPix) { return Convert.ToInt32(15 * unitPix); }
   internal static double EMUToPix(double unitTwip) { return Convert.ToInt32(96 / (double)914400 * unitTwip); }
   internal static double PixToEMU(double unitPix) { return Convert.ToInt32(914400 / (double)96 * unitPix); }
   internal static double PointsToPixels(double pt) { return Convert.ToDouble(pt * 96 / 72); }
   internal static double InchesToPixels(double inch) { return Convert.ToDouble(inch * 96); }
   internal static double PixelsToPoints(double px) { return px * 72 / 96; }

}
