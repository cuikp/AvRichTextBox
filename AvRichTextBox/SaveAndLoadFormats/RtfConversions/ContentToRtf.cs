using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Text;
using static AvRichTextBox.HelperMethods;

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

      foreach (Block block in fdoc.Blocks)
      {
         switch (block)
         {
            case Paragraph p:

               string parRtf = GetParagraphRtf(p, fontMap, colorMap);
               sb.Append(parRtf);
               break;

            case Table t:
               string tableRtf = GetTableRtf(t, fontMap, colorMap);
               sb.Append(tableRtf);
               break;
         }

      }
      sb.Remove(sb.Length - 5, 5);  // remove final \par
      sb.Append('}');
      return sb.ToString();
   }

   internal static string GetTableRtf(Table table, Dictionary<string, int> fontMap, Dictionary<Color, int> colorMap)
   {

      int[] colRights = new int[table.ColDefs.Count];
      int x = 0;
      for (int i = 0; i < table.ColDefs.Count; i++)
      {
         x += (int)PixToTwip(table.ColDefs[i].Width.Value);
         colRights[i] = x;
      }

      (int VMergeStart, int colspan, int cellBorderColorIdx, int cellBackColorIdx, Thickness cellPadding)[] nextVMergeStartswColSpans = new (int VMergeStart, int colspan, int cellBorderColorIdx, int cellBackColorIdx, Thickness cellPadding)[table.ColDefs.Count];

      StringBuilder tableRtf = new();
      for (int rowno = 0; rowno < table.RowDefs.Count; rowno++)
      {
         tableRtf.Append(@"\trowd ");

         switch (table.TableAlignment)
         {
            case Avalonia.Layout.HorizontalAlignment.Left:
            default:
               tableRtf.Append(@"\trql");
               break;
            case Avalonia.Layout.HorizontalAlignment.Center:
               tableRtf.Append(@"\trqc");
               break;
            case Avalonia.Layout.HorizontalAlignment.Right:
               tableRtf.Append(@"\trqr");
               break;
         }

         for (int colno = 0; colno < table.ColDefs.Count; colno++)
         {

            if (rowno < nextVMergeStartswColSpans[colno].VMergeStart)
               tableRtf.Append(@"\clvmrg");

            string appendParString = "";
            int advanceColNo = 0;

            if (table.Cells.FirstOrDefault(c => c.RowNo == rowno && c.ColNo == colno) is Cell thisCell)
            {
               switch (thisCell.CellVerticalAlignment)
               {
                  case Avalonia.Layout.VerticalAlignment.Top:
                     tableRtf.Append(@"\clvertalt");
                     break;
                  case Avalonia.Layout.VerticalAlignment.Center:
                     tableRtf.Append(@"\clvertalc");
                     break;
                  case Avalonia.Layout.VerticalAlignment.Bottom:
                     tableRtf.Append(@"\clvertalb");
                     break;
               }

               
               if (thisCell.RowSpan > 1)
                  tableRtf.Append(@"\clvmgf");

               advanceColNo = thisCell.ColSpan - 1;

               if (thisCell.CellContent is Paragraph p)
                  appendParString = GetParagraphRtf(p, fontMap, colorMap, true);

               nextVMergeStartswColSpans[colno].VMergeStart = rowno + thisCell.RowSpan;
               nextVMergeStartswColSpans[colno].colspan = thisCell.ColSpan;
               nextVMergeStartswColSpans[colno].cellPadding = thisCell.Padding;

               if (thisCell.BorderBrush is ISolidColorBrush borderBrush && colorMap.TryGetValue(borderBrush.Color, out int colorIndexBorderF))
                  nextVMergeStartswColSpans[colno].cellBorderColorIdx = colorIndexBorderF;
               
               if (thisCell.CellBackground is ISolidColorBrush backgroundBrush && colorMap.TryGetValue(backgroundBrush.Color, out int colorIndexBackF))
                  nextVMergeStartswColSpans[colno].cellBackColorIdx = colorIndexBackF;
               
            }
            else
            {
               advanceColNo= nextVMergeStartswColSpans[colno].colspan - 1;
            }


            int borderColorIdx = nextVMergeStartswColSpans[colno].cellBorderColorIdx;
            if (borderColorIdx != 0)
               tableRtf.Append(
                  $@"\clbrdrt\brdrs\brdrw20\brdrcf{borderColorIdx}" +
                  $@"\clbrdrl\brdrs\brdrw20\brdrcf{borderColorIdx}" +
                  $@"\clbrdrb\brdrs\brdrw20\brdrcf{borderColorIdx}" +
                  $@"\clbrdrr\brdrs\brdrw20\brdrcf{borderColorIdx}");
            
            int backColorIdx = nextVMergeStartswColSpans[colno].cellBackColorIdx;
            if (backColorIdx != 0)
               tableRtf.Append($@"\clcbpat{backColorIdx}");

            //cell padding
            Thickness cellPad = nextVMergeStartswColSpans[colno].cellPadding;
            int padL = (int)PixToTwip(cellPad.Left);
            int padT = (int)PixToTwip(cellPad.Top);
            int padR = (int)PixToTwip(cellPad.Right);
            int padB = (int)PixToTwip(cellPad.Bottom);
            tableRtf.Append($@"\clpadl{padL}\clpadfl3\clpadt{padT}\clpadft3\clpadr{padR}\clpadfr3\clpadb{padB}\clpadfb3");

            colno += advanceColNo;

            tableRtf.Append($@"\cellx{colRights[colno]} ");
            tableRtf.Append(appendParString);
            tableRtf.Append(@"\cell ");
           
         }

         tableRtf.Append(@"\row");
      }

      

      return tableRtf.ToString();

   }

   internal static string GetParagraphRtf(Paragraph par, Dictionary<string, int> fontMap, Dictionary<Color, int> colorMap, bool isTablePar = false)
   {
      bool BoldOn = false;
      bool ItalicOn = false;
      bool UnderlineOn = false;
      bool SuperscriptOn = false;
      bool SubscriptOn = false;
      int CurrentLang = 1033;

      StringBuilder parRtf = new ();

      parRtf.Append(@"\pard" + (isTablePar ? @"\intbl" : ""));
      parRtf.Append(par.TextAlignment switch
      {
         TextAlignment.Center => @"\qc",
         TextAlignment.Left => @"\ql",
         TextAlignment.Right => @"\qr",
         TextAlignment.Justify => @"\qj",
         _ => @"\ql"
      });


      double maxHeight = par.Inlines.Max(il => il.IsRun ? ((EditableRun)il).FontSize : par.LineHeight);
      double lineHeightPx = maxHeight == 0 ? 0 : (int)(par.LineHeight / maxHeight * 2 * 240D);
      //Debug.WriteLine("\nlineheightPx = " + lineHeightPx + "\nmaxHeight= " + maxHeight + "\nlineHeight = " + par.LineHeight);

      parRtf.Append(@$"\sl{lineHeightPx}\slmult0");

      if (par.BorderBrush is ISolidColorBrush borderBrush && borderBrush.Color != Colors.Transparent)
      {
         int brdrColIdx = 0;
         if (colorMap.TryGetValue(borderBrush.Color, out int colorIndexF))
            brdrColIdx = colorIndexF;

         string leftBorderWidth = PixToTwip(par.BorderThickness.Left).ToString();
         string rightBorderWidth = PixToTwip(par.BorderThickness.Right).ToString();
         string topBorderWidth = PixToTwip(par.BorderThickness.Top).ToString();
         string bottomBorderWidth = PixToTwip(par.BorderThickness.Bottom).ToString();
         parRtf.Append(@$"\brdrt\brdrs\brdrw{topBorderWidth}\brdrcf{brdrColIdx}");
         parRtf.Append(@$"\brdrl\brdrs\brdrw{leftBorderWidth}\brdrcf{brdrColIdx}");
         parRtf.Append(@$"\brdrb\brdrs\brdrw{bottomBorderWidth}\brdrcf{brdrColIdx}");
         parRtf.Append(@$"\brdrr\brdrs\brdrw{rightBorderWidth}\brdrcf{brdrColIdx}");
      }

      if (par.Background != null && par.Background.Color != Colors.Transparent)
      {
         int bkColIdx = 0;
         if (par.Background is ISolidColorBrush backgroundBrush && colorMap.TryGetValue(backgroundBrush.Color, out int colorIndexF))
            bkColIdx = colorIndexF;
         parRtf.Append(@$"\cbpat{bkColIdx}");
      }

      foreach (IEditable ied in par.Inlines)
         parRtf.Append(GetIEditableRtf(ied, ref BoldOn, ref ItalicOn, ref UnderlineOn, ref SuperscriptOn, ref SubscriptOn, ref CurrentLang, fontMap, colorMap));

      if (!isTablePar)
         parRtf.Append(@"\par ");

      return parRtf.ToString();

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
      bool SuperscriptOn = false;
      bool SubscriptOn = false;
      int CurrentLang = 1033;

      foreach (IEditable ied in inlines)
      {

         sb.Append(GetIEditableRtf(ied, ref BoldOn, ref ItalicOn, ref UnderlineOn, ref SuperscriptOn, ref SubscriptOn, ref CurrentLang, fontMap, colorMap));
         
         if (ied.InlineText.EndsWith("\r\n"))
            sb.Append(@"\par ");
      }
      
      sb.Append('}');

      return sb.ToString();
   }

   //private static string GetIEditableRtf(IEditable ied, ref bool BoldOn, ref bool ItalicOn, ref bool UnderlineOn, ref int currentLang, Dictionary<string, int> fontMap, Dictionary<Color, int> colorMap)
   private static string GetIEditableRtf(IEditable ied, ref bool BoldOn, ref bool ItalicOn, ref bool UnderlineOn, ref bool SuperscriptOn, ref bool SubscriptOn, ref int currentLang, Dictionary<string, int> fontMap, Dictionary<Color, int> colorMap)
   {
      StringBuilder iedSB = new();

      switch (ied)
      {
         case EditableLineBreak:
            return @"\line";

         case EditableInlineUIContainer eIUC:

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
            break;

         case EditableRun run:

            if (!BoldOn && run.FontWeight == FontWeight.Bold) { iedSB.Append(@"\b "); BoldOn = true; }
            if (!ItalicOn && run.FontStyle == FontStyle.Italic) { iedSB.Append(@"\i "); ; ItalicOn = true; }
            if (!UnderlineOn && run.TextDecorations == TextDecorations.Underline) { iedSB.Append(@"\ul "); ; UnderlineOn = true; }
            if (!SuperscriptOn && run.BaselineAlignment == BaselineAlignment.Superscript) { iedSB.Append(@"\super "); SuperscriptOn = true; }
            if (!SubscriptOn && run.BaselineAlignment == BaselineAlignment.Subscript) { iedSB.Append(@"\sub ");; SubscriptOn = true; }

            if (BoldOn && run.FontWeight == FontWeight.Normal) { iedSB.Append(@"\b0 "); BoldOn = false; }
            if (ItalicOn && run.FontStyle == FontStyle.Normal) { iedSB.Append(@"\i0 "); ItalicOn = false; }
            if (UnderlineOn && run.TextDecorations != TextDecorations.Underline) { iedSB.Append(@"\ul0 "); UnderlineOn = false; }
            if (SuperscriptOn && run.BaselineAlignment != BaselineAlignment.Superscript) { iedSB.Append(@"\nosupersub "); SuperscriptOn = false; }
            if (SubscriptOn && run.BaselineAlignment != BaselineAlignment.Subscript) { iedSB.Append(@"\nosupersub "); SubscriptOn = false; }

            if (run.FontSize > 0) iedSB.Append($@"\fs{(int)(run.FontSize * 2)} ");

            if (fontMap.TryGetValue(run.FontFamily.Name, out int fontIndex))
               iedSB.Append($@"\f{fontIndex} ");

            if (run.Foreground is ISolidColorBrush foregroundBrush && colorMap.TryGetValue(foregroundBrush.Color, out int colorIndexF))
               iedSB.Append($@"\cf{colorIndexF} ");
            else
               iedSB.Append(@"\cf0 "); // Reset to default

            if (run.Background is ISolidColorBrush backgroundBrush && backgroundBrush.Color != Colors.Transparent && colorMap.TryGetValue(backgroundBrush.Color, out int colorIndexB))
               iedSB.Append($@"\highlight{colorIndexB} ");
            else
               iedSB.Append(@"\highlight0 "); // Reset background to default

            if (!string.IsNullOrEmpty(run.Text))
               iedSB.Append(GetRtfRunText(run.Text!, ref currentLang));

            break;

      }

      return iedSB.ToString();
   }


   private static string GetFontAndColorTables(IEnumerable<Block> allBlocks, ref Dictionary<string, int> fontMap, ref Dictionary<Color, int> colorMap)
   {
      StringBuilder fontAndColorTableSB = new ();

      int fontIndex = 0;
      int colorIndex = 1;


      foreach (Block b in allBlocks)
      {
         switch (b)
         {
            case Paragraph par:

               GetParagraphColorFontMapping(par, ref fontMap, ref colorMap, ref fontIndex, ref colorIndex);

               break;

            case Table table:

               if (table.BorderBrush is ISolidColorBrush tableBorderBrush && tableBorderBrush.Color != Colors.Transparent)
                  if (!colorMap.ContainsKey(tableBorderBrush.Color))
                     colorMap[tableBorderBrush.Color] = colorIndex++;


               foreach (Cell c in table.Cells)
               {
                  if (c.BorderBrush is ISolidColorBrush cellBorderBrush && cellBorderBrush.Color != Colors.Transparent)
                     if (!colorMap.ContainsKey(cellBorderBrush.Color))
                        colorMap[cellBorderBrush.Color] = colorIndex++;

                  if (c.CellBackground is ISolidColorBrush cellBackground && cellBackground.Color != Colors.Transparent)
                     if (!colorMap.ContainsKey(cellBackground.Color))
                        colorMap[cellBackground.Color] = colorIndex++;

                  if (c.CellContent is Paragraph p)
                  {
                     GetParagraphColorFontMapping(p, ref fontMap, ref colorMap, ref fontIndex, ref colorIndex);
                  }
               }

               break;
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

   private static void GetParagraphColorFontMapping(Paragraph par, ref Dictionary<string, int> fontMap, ref Dictionary<Color, int> colorMap, ref int fontIndex, ref int colorIndex)
   {
      
      if (par.BorderBrush is ISolidColorBrush borderBrush && borderBrush.Color != Colors.Transparent)
         if (!colorMap.ContainsKey(borderBrush.Color))
            colorMap[borderBrush.Color] = colorIndex++;

      if (par.Background is ISolidColorBrush parBackground && parBackground.Color != Colors.Transparent)
         if (!colorMap.ContainsKey(parBackground.Color))
            colorMap[parBackground.Color] = colorIndex++;

      foreach (IEditable ied in par.Inlines)
      {
         if (ied is EditableRun run)
         {
            if (run.FontFamily != null && !fontMap.ContainsKey(run.FontFamily.Name))
               fontMap[run.FontFamily.Name] = fontIndex++;

            if (run.Foreground is ISolidColorBrush foregroundBrush)
               if (!colorMap.ContainsKey(foregroundBrush.Color))
                  colorMap[foregroundBrush.Color] = colorIndex++;

            if (run.Background is ISolidColorBrush backgroundBrush)
               if (!colorMap.ContainsKey(backgroundBrush.Color))
                  colorMap[backgroundBrush.Color] = colorIndex++;
         }
      }
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

            if (run.Foreground is ISolidColorBrush foregroundBrush)
               if (!colorMap.ContainsKey(foregroundBrush.Color))
                  colorMap[foregroundBrush.Color] = colorIndex++;

            if (run.Background is ISolidColorBrush backgroundBrush)
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
         else if (c == '"')
            sb.Append(@"\'22"); // Escape double quote
         else if (c > 127) // Non-ASCII (double-byte characters)
            sb.Append(@"\u" + (int)c + "?"); // Unicode escape
         else
            sb.Append(c);

      }
      return sb.ToString();
   }


}
