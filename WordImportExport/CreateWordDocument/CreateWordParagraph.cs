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
      var nj = new Justification();
      Paragraph? p = b as Paragraph;
      
      switch (p!.TextAlignment)
      {
         case Avalonia.Media.TextAlignment.Left: { nj.Val = JustificationValues.Left; break; }
         case Avalonia.Media.TextAlignment.Center: { nj.Val = JustificationValues.Center; break; }
         case Avalonia.Media.TextAlignment.Right: { nj.Val = JustificationValues.Right; break; }
         case Avalonia.Media.TextAlignment.Justify: { nj.Val = JustificationValues.Both; break; }
      }

      pPr.Justification = nj;

      pPr.SpacingBetweenLines = new SpacingBetweenLines() { Before = "0", After = "0", LineRule = LineSpacingRuleValues.Auto, Line = "240" };


      pPr.SnapToGrid = new SnapToGrid() { Val = new OnOffValue(false) };
      parg.AppendChild(pPr);

      foreach (IEditable iline in p.Inlines)
      {
         switch (iline.GetType())
         {
            case var @case when @case == typeof(EditableLineBreak):
               parg.AppendChild(new Break());
               break;

            case var @case when @case == typeof(EditableInlineUIContainer):

               EditableInlineUIContainer edUIC = (EditableInlineUIContainer)iline;
        
               if (edUIC.Child.GetType() == typeof(Image))
               {
                  string fontFamily = edUIC.FontFamily.ToString();

                  Image img = (Image)edUIC.Child;
                  img.Width = img.Bounds.Width;
                  img.Height = img.Bounds.Height;
                  Bitmap? imgbitmap = (Bitmap)img.Source!;

                  string extension = "";
                  ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);
                                                      
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

                     parg.AppendChild(new DOW.Run(CreateWordDocDrawing(mainPart.GetIdOfPart(imagePart), img.Width, img.Height, extension)));

                  }
                  break;
               }

               break;

            case var @case when @case == typeof(EditableRun):
               DOW.Run dRun = GetWordDocRun((EditableRun)iline!);
               if (dRun.InnerText == "\v")
                  parg.AppendChild(new Break());
               else
                  parg.AppendChild(dRun);
               break;
         }
      }

      return parg;
   }


}
