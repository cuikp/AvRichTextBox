using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media.Imaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Diagnostics;
using System.IO;
using DOW = DocumentFormat.OpenXml.Wordprocessing;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

internal static partial class WordConversions
{
   internal static DOW.Paragraph CreateWordDocParagraph(Block b)
   {

      var parg = new DOW.Paragraph();
      var pPr = new ParagraphProperties();
      Paragraph? p = b as Paragraph;

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
      
      if (p.Background!= null && p.Background.Color != Avalonia.Media.Colors.Transparent)
         pPr.Shading = new() { Val = ShadingPatternValues.Clear, Color = "auto", Fill = ToOpenXmlColor(p.Background.Color) };
      
      if (p.BorderBrush != null && p.BorderBrush.Color != Avalonia.Media.Colors.Transparent)
      {
         pPr.ParagraphBorders = new()
         {
            LeftBorder = new() { Val = BorderValues.Single, Color = ToOpenXmlColor(p.BorderBrush.Color), Size = (uint)(p.BorderThickness.Left * 6), Space = 0 },
            TopBorder = new() { Val = BorderValues.Single, Color = ToOpenXmlColor(p.BorderBrush.Color), Size = (uint)(p.BorderThickness.Top * 6), Space = 0 },
            RightBorder = new() { Val = BorderValues.Single, Color = ToOpenXmlColor(p.BorderBrush.Color), Size = (uint)(p.BorderThickness.Right * 6), Space = 0 },
            BottomBorder = new() { Val = BorderValues.Single, Color = ToOpenXmlColor(p.BorderBrush.Color), Size = (uint)(p.BorderThickness.Bottom * 6), Space = 0 },
         };
      }

      pPr.SpacingBetweenLines = new SpacingBetweenLines() { Before = "0", After = "0", LineRule = LineSpacingRuleValues.Auto, Line = "240" };
      pPr.SnapToGrid = new SnapToGrid() { Val = new OnOffValue(false) };
      parg.AppendChild(pPr);

      foreach (IEditable iline in p.Inlines)
      {
         switch (iline)
         {
            //case var @case when @case == typeof(EditableLineBreak):
            case EditableLineBreak elbreak:
               parg.AppendChild(new Break());
               break;

            case EditableInlineUIContainer edUIC:

               if (edUIC.Child.GetType() == typeof(Image))
               {
                  //string fontFamily = edUIC.FontFamily.ToString();

                  Image img = (Image)edUIC.Child;
                  img.Width = img.Bounds.Width;
                  img.Height = img.Bounds.Height;
                  Bitmap? imgbitmap = (Bitmap)img.Source!;

                  string extension = "";
                  ImagePart imagePart = mainPart!.AddImagePart(ImagePartType.Jpeg);
                                                      
                  //Debug.WriteLine("Imagesource is null ? : " + (thisImg.Source == null));
                  if (imgbitmap != null)
                  {
                     using (var memStream = new MemoryStream())
                     {
                        ResizeAndSaveBitmap(imgbitmap, (int)imgbitmap.Size.Width, (int)imgbitmap.Size.Height, memStream);
                        memStream.Position = 0;
                        imagePart.FeedData(memStream);
                        extension = ".jpg";
                     }

                     parg.AppendChild(new DOW.Run(CreateWordDocDrawing(mainPart!.GetIdOfPart(imagePart), img.Width, img.Height, extension)));

                  }
               }

               break;

            case EditableRun erun:
               DOW.Run dRun = GetWordDocRun(erun);
               //if (dRun.InnerText == "\n")
               //   parg.AppendChild(new Break());
               //else
               parg.AppendChild(dRun);
               break;
         }
      }

      return parg;
   }


}
