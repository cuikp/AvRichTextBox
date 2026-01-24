using Avalonia;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using static AvRichTextBox.HelperMethods;
using Avalonia.Media.Imaging;
using Avalonia.Controls;
using System.IO;

namespace AvRichTextBox;

internal static partial class RtfConversions
{
   internal static string GetRtfFromFlowDocument(FlowDocument fdoc)
   {
      var sb = new StringBuilder();

      //Build font map
      var fontMap = new Dictionary<string, int>();
      var colorMap = new Dictionary<Color, int>();
      sb.Append(GetFontAndColorTables(fdoc.Blocks, ref fontMap, ref colorMap));

      string margl = Math.Round(PixToTwip(fdoc.PagePadding.Left)).ToString();
      string margr = Math.Round(PixToTwip(fdoc.PagePadding.Right)).ToString();
      string margt = Math.Round(PixToTwip(fdoc.PagePadding.Top)).ToString();
      string margb = Math.Round(PixToTwip(fdoc.PagePadding.Bottom)).ToString();
      sb.Append(@$"\margl{margl}\margr{margr}\margt{margt}\margb{margb}");

      bool BoldOn = false;
      bool ItalicOn = false;
      bool UnderlineOn = false;
      int CurrentLang = 1033;

      foreach (Block block in fdoc.Blocks)
      {
         if (block.GetType() == typeof(Paragraph))
         {
            Paragraph p = (Paragraph)block;
            sb.Append(@"\pard");
            sb.Append(p.TextAlignment switch 
            { 
               TextAlignment.Center => @"\qc", 
               TextAlignment.Left => @"\ql", 
               TextAlignment.Right => @"\qr", 
               TextAlignment.Justify => @"\qj", 
               _=> @"\ql"
            });


            double maxHeight = p.Inlines.Max(il => il.IsRun ? ((EditableRun)il).FontSize : p.LineHeight);
            double lineHeightPx = maxHeight == 0 ? 0 : (int)(p.LineHeight / maxHeight * 2 * 240D);
            //Debug.WriteLine("\nlineheightPx = " + lineHeightPx + "\nmaxHeight= " + maxHeight + "\nlineHeight = " + p.LineHeight);

            sb.Append(@$"\sl{lineHeightPx}\slmult0");

            if (p.BorderBrush != null && p.BorderBrush.Color != Colors.Transparent)
            {
               int brdrColIdx = 0;
               if (p.BorderBrush is SolidColorBrush borderBrush && colorMap.TryGetValue(borderBrush.Color, out int colorIndexF))
                  brdrColIdx = colorIndexF;

               string leftBorderWidth = PixToTwip(p.BorderThickness.Left).ToString();
               string rightBorderWidth = PixToTwip(p.BorderThickness.Right).ToString();
               string topBorderWidth = PixToTwip(p.BorderThickness.Top).ToString();
               string bottomBorderWidth = PixToTwip(p.BorderThickness.Bottom).ToString();
               sb.Append(@$"\brdrt\brdrs\brdrw{topBorderWidth}\brdrcf{brdrColIdx}");
               sb.Append(@$"\brdrl\brdrs\brdrw{leftBorderWidth}\brdrcf{brdrColIdx}");
               sb.Append(@$"\brdrb\brdrs\brdrw{bottomBorderWidth}\brdrcf{brdrColIdx}");
               sb.Append(@$"\brdrr\brdrs\brdrw{rightBorderWidth}\brdrcf{brdrColIdx}");
            }

            if (p.Background != null && p.Background.Color != Colors.Transparent)
            {
               int bkColIdx = 0;
               if (p.Background is SolidColorBrush backgroundBrush && colorMap.TryGetValue(backgroundBrush.Color, out int colorIndexF))
                  bkColIdx = colorIndexF;
               sb.Append(@$"\cbpat{bkColIdx}");
            }

            foreach (IEditable ied in p.Inlines)
            sb.Append(GetIEditableRtf(ied, ref BoldOn, ref ItalicOn, ref UnderlineOn, ref CurrentLang, fontMap, colorMap));

            sb.Append(@"\par ");
         }
      }
      sb.Remove(sb.Length - 5, 5);  // remove final \par
      sb.Append('}');
      return sb.ToString();
   }

   internal static string GetRtfFromInlines(List<IEditable> inlines)
   {
      var sb = new StringBuilder();

      //Build font map
      var fontMap = new Dictionary<string, int>();
      var colorMap = new Dictionary<Color, int>();
      sb.Append(GetFontAndColorTables(inlines, ref fontMap, ref colorMap));

      bool BoldOn = false;
      bool ItalicOn = false;
      bool UnderlineOn = false;
      int CurrentLang = 1033;

      foreach (IEditable ied in inlines)
      {

         sb.Append(GetIEditableRtf(ied, ref BoldOn, ref ItalicOn, ref UnderlineOn, ref CurrentLang, fontMap, colorMap));
         
         if (ied.InlineText.EndsWith('\r'))
            sb.Append(@"\par ");
      }
      
      sb.Append('}');

      return sb.ToString();
   }

   private static string GetIEditableRtf(IEditable ied, ref bool BoldOn, ref bool ItalicOn, ref bool UnderlineOn, ref int currentLang, Dictionary<string, int> fontMap, Dictionary<Color, int> colorMap)
   {
      StringBuilder iedSB = new();

      if (ied.GetType() == typeof(EditableLineBreak)) return @"\line";


      if (ied.GetType() == typeof(EditableInlineUIContainer))
      {
         EditableInlineUIContainer eIUC = (EditableInlineUIContainer)ied;
         if (eIUC.Child != null)
         {
            if (eIUC.Child.GetType() == typeof(Image))
            {
               if (eIUC.Child is Image thisImg && thisImg.Source is Bitmap imgbitmap)
               {
                  int picw = imgbitmap.PixelSize.Width;
                  int pich = imgbitmap.PixelSize.Height;
                  int picwgoal = (int)PixToTwip(thisImg.Width);
                  int pichgoal = (int)PixToTwip(thisImg.Height);

                  using MemoryStream memoryStream = new();

                  var renderTarget = new RenderTargetBitmap(new PixelSize(picw, pich));
                  using (var context = renderTarget.CreateDrawingContext())
                     context.DrawImage(imgbitmap, new Rect(0, 0, picw, pich));

                  renderTarget.Save(memoryStream);  // png by default
                  memoryStream.Seek(0, SeekOrigin.Begin);

                  byte[] imgbytes = new byte[memoryStream.Length];
                  memoryStream.Read(imgbytes, 0, imgbytes.Length);

                  // add image to rtf code:
                  iedSB.AppendLine($@"{{\pict\pngblip\picw{picw}\pich{pich}\picwgoal{picwgoal}\pichgoal{pichgoal}");

                  foreach (byte b in imgbytes)
                     iedSB.Append(b.ToString("x2"));  // hex encoding

                  iedSB.AppendLine("}");

               }
            }
         }
      }

      if (ied.GetType() == typeof(EditableRun))
      {
         EditableRun run = (EditableRun)ied;
         
         if (!BoldOn && run.FontWeight == FontWeight.Bold) { iedSB.Append(@"\b "); BoldOn = true; }
         if (!ItalicOn && run.FontStyle == FontStyle.Italic) { iedSB.Append(@"\i "); ; ItalicOn = true; }
         if (!UnderlineOn && run.TextDecorations == TextDecorations.Underline) { iedSB.Append(@"\ul "); ; UnderlineOn = true; }
         
         if (BoldOn && run.FontWeight == FontWeight.Normal) { iedSB.Append(@"\b0 "); BoldOn = false; }
         if (ItalicOn && run.FontStyle == FontStyle.Normal) { iedSB.Append(@"\i0 "); ItalicOn = false; }
         if (UnderlineOn && run.TextDecorations != TextDecorations.Underline) { iedSB.Append(@"\ul0 "); UnderlineOn = false; }

         if (run.FontSize > 0) iedSB.Append($@"\fs{(int)(run.FontSize * 2)} ");

         if (fontMap.TryGetValue(run.FontFamily.Name, out int fontIndex))
            iedSB.Append($@"\f{fontIndex} ");

         if (run.Foreground is SolidColorBrush foregroundBrush && colorMap.TryGetValue(foregroundBrush.Color, out int colorIndexF))
            iedSB.Append($@"\cf{colorIndexF} ");
         else
            iedSB.Append(@"\cf0 "); // Reset to default

         if (run.Background is SolidColorBrush backgroundBrush && backgroundBrush.Color != Colors.Transparent && colorMap.TryGetValue(backgroundBrush.Color, out int colorIndexB))
            iedSB.Append($@"\highlight{colorIndexB} ");
         else
            iedSB.Append(@"\highlight0 "); // Reset background to default

         if (!string.IsNullOrEmpty(run.Text))
            iedSB.Append(GetRtfRunText(run.Text!, ref currentLang));
      }

      return iedSB.ToString();
   }


   private static string GetFontAndColorTables(IEnumerable<Block> allBlocks, ref Dictionary<string, int> fontMap, ref Dictionary<Color, int> colorMap)
   {
      StringBuilder fontAndColorTableSB = new ();

      int fontIndex = 0;
      int colorIndex = 1;

      foreach (Paragraph par in allBlocks.Where(b=>b.IsParagraph))
      {
         if (par.BorderBrush is SolidColorBrush borderBrush && par.BorderBrush.Color != Colors.Transparent)
            if (!colorMap.ContainsKey(borderBrush.Color))
               colorMap[borderBrush.Color] = colorIndex++;

         if (par.Background is SolidColorBrush parBackground && par.Background.Color != Colors.Transparent)
            if (!colorMap.ContainsKey(parBackground.Color))
               colorMap[parBackground.Color] = colorIndex++;

      }

      foreach (IEditable ied in allBlocks.SelectMany(b => ((Paragraph)b).Inlines))
      {
         if (ied is EditableRun run)
         {
            if (run.FontFamily != null && !fontMap.ContainsKey(run.FontFamily.Name))
               fontMap[run.FontFamily.Name] = fontIndex++;

            if (run.Foreground is SolidColorBrush foregroundBrush)
               if (!colorMap.ContainsKey(foregroundBrush.Color))
                  colorMap[foregroundBrush.Color] = colorIndex++;

            if (run.Background is SolidColorBrush backgroundBrush)
               if (!colorMap.ContainsKey(backgroundBrush.Color))
                  colorMap[backgroundBrush.Color] = colorIndex++;
         }
      }
         
      fontAndColorTableSB.Append(@"{\rtf1\ansi\deff0 {\fonttbl");
      foreach (var kvp in fontMap)
         fontAndColorTableSB.Append($@"{{\f{kvp.Value}\fnil {kvp.Key};}}");
      fontAndColorTableSB.Append('}');

      fontAndColorTableSB.Append(@"{\colortbl;");
      foreach (var kvp in colorMap)
         fontAndColorTableSB.Append($@"\red{kvp.Key.R}\green{kvp.Key.G}\blue{kvp.Key.B};");
      fontAndColorTableSB.Append('}');

      return fontAndColorTableSB.ToString();

   }

   private static string GetFontAndColorTables(IEnumerable<IEditable> inlinesToMap, ref Dictionary<string, int> fontMap, ref Dictionary<Color, int> colorMap)
   {
   
      int fontIndex = 0;
      int colorIndex = 1;

      foreach (IEditable ied in inlinesToMap)
      {
         if (ied is EditableRun run)
         {
            if (run.FontFamily != null && !fontMap.ContainsKey(run.FontFamily.Name))
               fontMap[run.FontFamily.Name] = fontIndex++;

            if (run.Foreground is SolidColorBrush foregroundBrush)
               if (!colorMap.ContainsKey(foregroundBrush.Color))
                  colorMap[foregroundBrush.Color] = colorIndex++;

            if (run.Background is SolidColorBrush backgroundBrush)
               if (!colorMap.ContainsKey(backgroundBrush.Color))
                  colorMap[backgroundBrush.Color] = colorIndex++;
         }
      }

      StringBuilder fontAndColorTableSB = new();

      fontAndColorTableSB.Append(@"{\rtf1\ansi\deff0 {\fonttbl");
      foreach (var kvp in fontMap)
         fontAndColorTableSB.Append($@"{{\f{kvp.Value}\fnil {kvp.Key};}}");
      fontAndColorTableSB.Append('}');

      fontAndColorTableSB.Append(@"{\colortbl;");
      foreach (var kvp in colorMap)
         fontAndColorTableSB.Append($@"\red{kvp.Key.R}\green{kvp.Key.G}\blue{kvp.Key.B};");
      fontAndColorTableSB.Append('}');

      return fontAndColorTableSB.ToString();

   }


   private static string GetRtfRunText(string text, ref int currentLang)
   {
  
      StringBuilder sb = new();

      foreach (char c in text)
      {
         int newLang = GetLanguageForChar(c);

         if (newLang != currentLang)
         {
            sb.Append($@"\lang{newLang} ");
            currentLang = newLang;
         }

         if (c is '\\' or '{' or '}')
            sb.Append(@"\" + c); // RTF control characters
         else if (c > 127) // Non-ASCII (double-byte characters)
            sb.Append(@"\u" + (int)c + "?"); // Unicode escape
         else
            sb.Append(c);

      }
      return sb.ToString();
   }


}