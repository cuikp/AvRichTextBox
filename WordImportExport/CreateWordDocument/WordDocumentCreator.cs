using Avalonia.Media;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Diagnostics;
using DOW = DocumentFormat.OpenXml.Wordprocessing;
using AvColor = Avalonia.Media.Color;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

internal static partial class WordConversions
{
   private static MainDocumentPart? mainPart;

   internal static void SaveWordDoc(string saveWordFileName, FlowDocument fdoc)
   {
      using var WPDoc = WordprocessingDocument.Create(saveWordFileName, WordprocessingDocumentType.Document);

      mainPart = WPDoc.AddMainDocumentPart();
      mainPart.Document = new Document();


      //Set page and margin size according to Client
      int pmarLeft = default, pmarTop = default, pmarRight = default, pmarBottom = default;
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
