using Avalonia.Media;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Diagnostics;
using DOW = DocumentFormat.OpenXml.Wordprocessing;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

public static partial class WordConversions
{
   private static bool toClientFormat = false;
   private static MainDocumentPart mainPart;

   internal static void SaveWordDoc(string saveWordFileName, FlowDocument fdoc)
   {
      using (var WPDoc = WordprocessingDocument.Create(saveWordFileName, WordprocessingDocumentType.Document))
      {

         mainPart = WPDoc.AddMainDocumentPart();
         mainPart.Document = new Document();


         //Set page and margin size according to Client
         int pmarLeft = default(int), pmarTop = default(int), pmarRight = default(int), pmarBottom = default(int);
         pmarLeft = Convert.ToInt32(PixToTwip(fdoc.PagePadding.Left));
         pmarTop = Convert.ToInt32(PixToTwip(fdoc.PagePadding.Top));
         pmarRight = Convert.ToInt32(PixToTwip(fdoc.PagePadding.Right));
         pmarBottom = Convert.ToInt32(PixToTwip(fdoc.PagePadding.Bottom));
         var newpageMargin = new PageMargin() { Left = Convert.ToUInt32(pmarLeft), Top = pmarTop, Bottom = pmarBottom, Right = Convert.ToUInt32(pmarRight), Header = 720, Footer = 720 };

         var newcol = new Columns() { Space = new StringValue("720") };      // (StringValue)720 };
         var newpageSize = new PageSize() { Width = 21 * 567, Height = Convert.ToUInt32(29.7 * 567) };  // A4 size


         //Start creating word body
         var body = new Body();
         var sectp = new SectionProperties();
         sectp.Append(newpageMargin);
         sectp.Append(newpageSize);
         body.Append(sectp);


         mainPart.Document.AppendChild(body);

         foreach (Block b in fdoc.Blocks)
         {
            switch (b.GetType())
            {
               case Type t when t == typeof(Paragraph):
                                 
                  Paragraph p = (Paragraph)b;
                  DOW.Paragraph dowP = CreateWordDocParagraph(p);
                  body.AppendChild(dowP);

                  //Debug.WriteLine("\npPR.spacing=" + dowP.ParagraphProperties.SpacingBetweenLines.Line.ToString() + " (" + dowP.ParagraphProperties.SpacingBetweenLines.LineRule.ToString() + ")");
            
                  break;

                  //case Type t when t == typeof(Table):
                  //   body.AppendChild(CreateWordDocTable((Table)b));
                  //   break;
            }
         }

         WPDoc.Save();

      }

   }


   public static HighlightColorValues BrushToHighlightColorValue(IBrush br)
   {

      var hcv = new HighlightColorValues();
      switch (true)
      {
         case object _ when br == Brushes.Yellow | br.ToString() == "#FFFFE0C0": { hcv = HighlightColorValues.Yellow; break; }
         case object _ when br == Brushes.Red | br.ToString() == "#FFFF4500": { hcv = HighlightColorValues.Red; break; }
         case object _ when br == Brushes.Cyan | br.ToString() == "#FFE8EBF9": { hcv = HighlightColorValues.Cyan; break; }
         case object _ when br == Brushes.Green | br.ToString() == "#FF98FB98": { hcv = HighlightColorValues.Green; break; }
         case object _ when br == Brushes.Blue | br.ToString() == "#FFE3BFFF": { hcv = HighlightColorValues.Blue; break; }
         case object _ when br == Brushes.YellowGreen | br.ToString() == "#FF9ACD32": { hcv = HighlightColorValues.DarkYellow; break; }
         case object _ when br == Brushes.White: { hcv = HighlightColorValues.White; break; }
         case object _ when br == Brushes.Magenta: { hcv = HighlightColorValues.Magenta; break; }
         case object _ when br == Brushes.LightGray: { hcv = HighlightColorValues.LightGray; break; }
         case object _ when br == Brushes.DarkRed: { hcv = HighlightColorValues.DarkRed; break; }
         case object _ when br == Brushes.DarkMagenta: { hcv = HighlightColorValues.DarkMagenta; break; }
         case object _ when br == Brushes.DarkGreen: { hcv = HighlightColorValues.DarkGreen; break; }
         case object _ when br == Brushes.DarkGray: { hcv = HighlightColorValues.DarkGray; break; }
         case object _ when br == Brushes.DarkCyan: { hcv = HighlightColorValues.DarkCyan; break; }
         case object _ when br == Brushes.DarkBlue: { hcv = HighlightColorValues.DarkBlue; break; }
         case object _ when br == Brushes.Blue: { hcv = HighlightColorValues.Blue; break; }
         case object _ when br == Brushes.Black: { hcv = HighlightColorValues.Black; break; }
         case object _ when br == null: { hcv = HighlightColorValues.None; break; }

         default: break;
      }

      return hcv;
   }



}
