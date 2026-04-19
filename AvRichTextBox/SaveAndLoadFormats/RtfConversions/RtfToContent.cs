using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DynamicData;
using RtfDomParserAv;
using System.Text.RegularExpressions;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

internal static partial class RtfConversions
{
   internal static string DefaultEastAsiaFont = "";
   internal static string DefaultAsciiFont = "";

  
   internal static void GetFlowDocumentFromRtf(RTFDomDocument rtfdoc, FlowDocument fdoc)
   {
      double leftMargin = Math.Round(TwipToPix(rtfdoc.LeftMargin));
      double topMargin = Math.Round(TwipToPix(rtfdoc.TopMargin));
      double rightMargin = Math.Round(TwipToPix(rtfdoc.RightMargin));
      double bottomMargin = Math.Round(TwipToPix(rtfdoc.BottomMargin));

      fdoc.PagePadding = new Thickness(leftMargin, topMargin, rightMargin, bottomMargin);

      foreach (RTFDomElement rtfelm in rtfdoc.Elements)
      {
         switch (rtfelm)
         {
            case RTFDomParagraph rtfpar:

               Paragraph newpar =  GetParagraphFromRtfDom(rtfpar, fdoc);
               fdoc.Blocks.Add(newpar);
               break;

            case RTFDomTable rtftable:
               Table newtable = GetTableFromRtfDom(rtftable, fdoc, rtfdoc.ColorTable);
               fdoc.Blocks.Add(newtable);
               break;
         }
        
      }

      fdoc.PagePadding = new Thickness(TwipToPix(rtfdoc.LeftMargin), TwipToPix(rtfdoc.TopMargin), TwipToPix(rtfdoc.RightMargin), TwipToPix(rtfdoc.BottomMargin));

   }

   private static Table GetTableFromRtfDom(RTFDomTable rtftable, FlowDocument fdoc, RTFColorTable cTable)
   {
      Table newtable = new(fdoc);
      for (int colno = 0; colno < rtftable.Columns.Count; colno++)
      {
         RTFDomElement thisCol = rtftable.Columns[colno];
         newtable.ColDefs.Add(new());
      }

      int tablewidth = 20;
      int rowno = 0;
      foreach (RTFDomTableRow row in rtftable.Elements.OfType<RTFDomTableRow>())
      {
         newtable.RowDefs.Add(new());


         foreach (RTFAttribute att in row.Attributes)
         {
            switch (att.Name)
            {
               case "trql":
                  newtable.TableAlignment = HorizontalAlignment.Left;
                  break;
               case "trqc":
                  newtable.TableAlignment = HorizontalAlignment.Center;
                  break;
               case "trqr":
                  newtable.TableAlignment = HorizontalAlignment.Right;
                  break;
            }
         }

         int colno = 0;

         BorderType borderType = BorderType.Left;

         foreach (RTFDomTableCell celm in row.Elements.OfType<RTFDomTableCell>())
         {

            Cell newCell = new(newtable) { RowNo = rowno, ColNo = colno, ColSpan = celm.ColSpan, RowSpan = celm.RowSpan };
                        
            double cellBorderLeft = 1;
            double cellBorderTop = 1;
            double cellBorderRight = 1;
            double cellBorderBottom = 1;
            
            double cellPaddingLeft = 1;
            double cellPaddingTop = 1;
            double cellPaddingRight = 1;
            double cellPaddingBottom = 1;
            
            bool isVMerged = false;

            foreach (RTFAttribute att in celm.Attributes)
            {
               switch (att.Name)
               {
                  case "clvertalc":
                     newCell.CellVerticalAlignment = VerticalAlignment.Center;
                     break;
                  case "clvertalt":
                     newCell.CellVerticalAlignment = VerticalAlignment.Top;
                     break;
                  case "clvertalb":
                     newCell.CellVerticalAlignment = VerticalAlignment.Bottom;
                     break;

                  case "clvmrg":
                     isVMerged = true;
                     break;

                  case "clvmgf":
                     break;

                  case "brdrs":
                     Debug.WriteLine("border single");
                     break;

                  case "brdrw":
                     switch (borderType)
                     {
                        case BorderType.Left:
                           cellBorderLeft = Math.Round(TwipToPix(att.Value));
                           break;
                        case BorderType.Right:
                           cellBorderRight = Math.Round(TwipToPix(att.Value));
                           break;
                        case BorderType.Top:
                           cellBorderTop = Math.Round(TwipToPix(att.Value));
                           break;
                        case BorderType.Bottom:
                           cellBorderBottom = Math.Round(TwipToPix(att.Value));
                           break;
                     }
                     break;

                  case "clbrdrb":
                     borderType = BorderType.Bottom;
                     break;
                  case "clbrdrt":
                     borderType = BorderType.Top;
                     break;
                  case "clbrdrr":
                     borderType = BorderType.Right;
                     break;
                  case "clbrdrl":
                     borderType = BorderType.Left;
                     break;

                  case "brdrcf":
                     newCell.BorderBrush = new SolidColorBrush(cTable.GetColor(att.Value, Colors.Black));
                     break;

                  case "clcbpat":
                     newCell.CellBackground = new SolidColorBrush(cTable.GetColor(att.Value, Colors.Black));
                     break;

                  case "cellx":
                     tablewidth = Math.Max(tablewidth, att.Value);
                     break;

                  case "clpadl":
                     cellPaddingLeft = Math.Round(TwipToPix(att.Value));
                     break;

                  case "clpadt":
                     cellPaddingTop = Math.Round(TwipToPix(att.Value));
                     break;

                  case "clpadr":
                     cellPaddingRight = Math.Round(TwipToPix(att.Value));
                     break;
                  
                  case "clpadb":
                     cellPaddingBottom = Math.Round(TwipToPix(att.Value));
                     break;

                  default:
                     Debug.WriteLine("unknown attr = " + att.Name);
                     break;
               }
            }
            
            if (!isVMerged)
            {
               foreach (RTFDomParagraph rtfpardom in celm.Elements.OfType<RTFDomParagraph>())
                  newCell.CellContent = GetParagraphFromRtfDom(rtfpardom, fdoc);

               newCell.BorderThickness = new(cellBorderLeft, cellBorderTop, cellBorderRight, cellBorderBottom);
               newCell.Padding = new(cellPaddingLeft, cellPaddingTop, cellPaddingRight, cellPaddingBottom);

               if (newCell.CellContent != null)
                  newtable.Cells.Add(newCell);
            }
      
            colno++;
         }

         rowno++;
      }

      newtable.Width = TwipToPix(tablewidth);
      int cols = newtable.ColDefs.Count;
      newtable.ColDefs.ToList().ForEach(cd => cd.Width = new(newtable.Width / cols, Avalonia.Controls.GridUnitType.Pixel));

      return newtable;
   }

   private static Paragraph GetParagraphFromRtfDom(RTFDomParagraph rtfpar, FlowDocument fdoc)
   {
      Paragraph newpar = new(fdoc);

      switch (rtfpar.Format.Align)
      {
         case RTFAlignment.Left: newpar.TextAlignment = TextAlignment.Left; break;
         case RTFAlignment.Center: newpar.TextAlignment = TextAlignment.Center; break;
         case RTFAlignment.Right: newpar.TextAlignment = TextAlignment.Right; break;
         case RTFAlignment.Justify: newpar.TextAlignment = TextAlignment.Justify; break;
      }

      newpar.Background = new SolidColorBrush(rtfpar.Format.BackColor);
      newpar.BorderBrush = new SolidColorBrush(rtfpar.Format.BorderColor);
      newpar.BorderThickness = new Avalonia.Thickness(TwipToPix(rtfpar.Format.BorderWidth));

      newpar.FontFamily = new FontFamily(rtfpar.Format.FontName);
      //newpar.Margin = new Thickness(rtfpar.Format.xxx);
      
      List<IEditable> addInlines = GetRtfTextElementsAsInlines(rtfpar.Elements);

      newpar.Inlines.AddRange(addInlines);


      if (newpar.Inlines.Count == 0) newpar.Inlines.Add(new EditableRun(""));

      //if (newpar.Inlines.Count > 0)
      //{
      //double rtfLineHeight = TwipToPix(rtfpar.Format.LineSpacing);
      double rtfLineHeightUnits = (double)rtfpar.Format.LineSpacing / 240;

      //Debug.WriteLine("rtfparlinespacing = " + rtfpar.Format.LineSpacing);
      double ilineH = newpar.Inlines.First().InlineHeight;
      //double spacing = rtfLineHeight - ilineH;

      double lheight = ilineH * rtfLineHeightUnits * 1.25;
      newpar.LineHeight = lheight;
      //newpar.LineSpacing = spacing;
      //}
      // else
      //    newpar.LineHeight = TwipToPix(rtfpar.Format.LineSpacing);

      return newpar;
   }

   internal static List<IEditable> GetRtfTextElementsAsInlines(RTFDomElementList elements)
   {
      List<IEditable> returnList = [];

      foreach (RTFDomElement domelm in elements)
      {

         if (domelm is RTFDomField rtfField)
         {
            RTFDomElementContainer rcont = rtfField.Result;

            //Debug.WriteLine("innert tex=" + rcont.InnerText);


            foreach (RTFDomElement rtfelm in rcont.Elements)
            {
               if (rtfelm is RTFDomText rtftext1)
               {
                  EditableRun erun = new(rtftext1.Text);
                  {
                     erun.FontSize = rtftext1.Format.FontSize;
                  };
                  returnList.Add(erun);
               }
               else
               {
                  Debug.WriteLine("other=" + rtfelm.GetType().ToString());
               }
            }

         }

         else if (domelm is RTFDomLineBreak rtflineBreak)
         {
            EditableLineBreak elinebreak = new();
            returnList.Add(elinebreak);
         }
         
         else if (domelm is RTFDomImage rtfImage)
         {
            EditableInlineUIContainer eIUC = new(null!)
            {
               FontFamily = "Image" //???
            };

            Avalonia.Controls.Image img = new()
            {
               Width = TwipToPix(rtfImage.Width),
               Height = TwipToPix(rtfImage.Height),
               Stretch = Stretch.Fill
            };

            MemoryStream memStream = new(rtfImage.Data) { Position = 0 };
            img.Source = new Bitmap(memStream);
            eIUC.Child = img;
            returnList.Add(eIUC);
         }

         else if (domelm is RTFDomText rtftext2)
         {            
            //EditableRun erun = new(DecodeRtfUnicode(rtftext2.GetText))
            EditableRun erun = new(rtftext2.Text)
            {
               FontSize = rtftext2.Format.FontSize
            };

            if (rtftext2.Format.Bold)
               erun.FontWeight = FontWeight.Bold;

            if (rtftext2.Format.Italic)
               erun.FontStyle = FontStyle.Italic;

            if (rtftext2.Format.Underline)
               erun.TextDecorations = TextDecorations.Underline;

            if (rtftext2.Format.Strikeout)
               erun.TextDecorations = TextDecorations.Strikethrough;

            if (rtftext2.Format.Subscript)
               erun.BaselineAlignment = BaselineAlignment.Subscript;

            if (rtftext2.Format.Superscript)
               erun.BaselineAlignment = BaselineAlignment.Superscript;

            erun.Foreground = new SolidColorBrush(rtftext2.Format.TextColor);
            erun.Background = new SolidColorBrush(rtftext2.Format.BackColor);
            erun.FontFamily = new FontFamily(rtftext2.Format.FontName);
            //erun.FontFamily = new FontFamily("Meiryo");
            //Debug.WriteLine("erun: " + erun.FontFamily + "  (" + erun.GetText + ")");

            returnList.Add(erun);

         }
         else
         {
            Debug.WriteLine("unknown: " + domelm.GetType().ToString());
         }
      }

      return returnList;
   }

   private static string DecodeRtfUnicode(string rtfText)
   {
      return RtfUnicodeRegex().Replace(rtfText, match =>
      {
         int unicodeValue = int.Parse(match.Groups[1].Value);
         return char.ConvertFromUtf32(unicodeValue);
      });
   }

   [GeneratedRegex(@"\\u(-?\d+)\?")]
   private static partial Regex RtfUnicodeRegex();

   private enum BorderType
   {
      Left, Top, Right, Bottom   
   }
}


