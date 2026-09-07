using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using static AvRichTextBox.HelperMethods;
using DOW = DocumentFormat.OpenXml.Wordprocessing;

namespace AvRichTextBox;

internal static partial class WordConversions
{
   internal static DOW.Paragraph CreateWordDocParagraph(Block b, ref MainDocumentPart mainPart)
   {

      var parg = new DOW.Paragraph();
      var pPr = new ParagraphProperties();
      if (b is Paragraph p)
      {
         pPr.Justification = new()
         {
            Val = p!.TextAlignment switch
            {
               Avalonia.Media.TextAlignment.Left => JustificationValues.Left,
               Avalonia.Media.TextAlignment.Center => JustificationValues.Center,
               Avalonia.Media.TextAlignment.Right => JustificationValues.Right,
               Avalonia.Media.TextAlignment.Justify => JustificationValues.Both,
               _ => JustificationValues.Left
            }
         };

         if (p.Background != null && GetPrimaryColor(p.Background) != Colors.Transparent)
            pPr.Shading = new() { Val = ShadingPatternValues.Clear, Color = "auto", Fill = ToOpenXmlColor(GetPrimaryColor(p.Background) ?? Colors.Transparent) };

         if (p.BorderBrush != null &&  GetPrimaryColor(p.BorderBrush) != Colors.Transparent)
         {
            if (p.BorderThickness != default)
            {
               Avalonia.Media.Color? borderColor = GetPrimaryColor(p.BorderBrush);

               pPr.ParagraphBorders = new()
               {
                  LeftBorder = new() { Val = BorderValues.Single, Color = ToOpenXmlColor(borderColor), Size = (uint)(p.BorderThickness.Left * 6), Space = 0 },
                  TopBorder = new() { Val = BorderValues.Single, Color = ToOpenXmlColor(borderColor), Size = (uint)(p.BorderThickness.Top * 6), Space = 0 },
                  RightBorder = new() { Val = BorderValues.Single, Color = ToOpenXmlColor(borderColor), Size = (uint)(p.BorderThickness.Right * 6), Space = 0 },
                  BottomBorder = new() { Val = BorderValues.Single, Color = ToOpenXmlColor(borderColor), Size = (uint)(p.BorderThickness.Bottom * 6), Space = 0 },
               };
            }
               
         }

         pPr.SpacingBetweenLines = new SpacingBetweenLines() { Before = "0", After = "0", LineRule = LineSpacingRuleValues.Auto, Line = "240" };
         pPr.SnapToGrid = new SnapToGrid() { Val = new OnOffValue(false) };
         parg.AppendChild(pPr);

         foreach (IEditable iline in p.Inlines)
         {
            switch (iline)
            {
               case EditableHyperlink hlink:

                  if (Uri.TryCreate(hlink.NavigateUri, UriKind.Absolute, out var uri))
                  {
                     var relationship = mainPart.AddHyperlinkRelationship(uri, true);

                     var newHyperlink = new DOW.Hyperlink(
                        new DOW.Run(
                        new DOW.RunProperties(
                           new DOW.RunStyle { Val = "Hyperlink" }, 
                           new DOW.Color { Val = "0563C1" },
                           new DOW.Underline { Val = DOW.UnderlineValues.Single } 
                        ),
                        new DOW.Text(hlink.Text ?? hlink.NavigateUri)
                        )
                     )
                     { Id = relationship.Id };

                     parg.AppendChild(newHyperlink);

                     //Debug.WriteLine("appended hyperlink: " + newHyperlink.Id);
                  }

                  break;

               case EditableLineBreak elbreak:
                  parg.AppendChild(new Break());
                  break;

               case EditableInlineUIContainer edUIC:

                  if (edUIC.GetChild() is Image img)
                  {
                     //string fontFamily = edUIC.FontFamily.ToString();
                     img.Width = img.Bounds.Width;
                     img.Height = img.Bounds.Height;
                     if (img.Source is Bitmap imgbitmap)
                     {
                        string extension = "";
                        ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);
                        using (var memStream = new MemoryStream())
                        {
                           ResizeAndSaveBitmap(imgbitmap, (int)imgbitmap.Size.Width, (int)imgbitmap.Size.Height, memStream);
                           memStream.Position = 0;
                           imagePart.FeedData(memStream);
                           extension = ".jpg";
                        }
                        parg.AppendChild(new DOW.Run(CreateWordDocDrawing(mainPart.GetIdOfPart(imagePart), img.Width, img.Height, extension)));
                     }
                  }

                  break;

               case EditableRun erun:
                  DOW.Run dRun = GetWordDocRun(erun);
                  parg.AppendChild(dRun);
                  break;
            }
         }

      }

      return parg;
   }


}
