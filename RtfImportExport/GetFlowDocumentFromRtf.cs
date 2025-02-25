using Avalonia;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Linq;
using static AvRichTextBox.HelperMethods;
using RtfDomParser;
using DocumentFormat.OpenXml.Office2016.Excel;

namespace AvRichTextBox;

internal static partial class RtfConversions
{
   internal static string DefaultEastAsiaFont = "";
   internal static string DefaultAsciiFont = "";

   internal static void GetFlowDocumentFromRtf(RTFDomDocument rtfdoc, FlowDocument fdoc)
   {

      foreach (RTFDomElement rtfelm in rtfdoc.Elements)
      {
         if (rtfelm.GetType() == typeof(RTFDomParagraph))
         {
            RTFDomParagraph rtfpar = (RTFDomParagraph)rtfelm;
            Paragraph newpar = new();

            switch (rtfpar.Format.Align)
            {
               case RTFAlignment.Left: newpar.TextAlignment = TextAlignment.Left; break;
               case RTFAlignment.Center: newpar.TextAlignment = TextAlignment.Center; break;
               case RTFAlignment.Right: newpar.TextAlignment = TextAlignment.Right; break;
               case RTFAlignment.Justify: newpar.TextAlignment = TextAlignment.Justify; break;
            }

            newpar.LineHeight = TwipToPix(PixelsToPoints(rtfpar.Format.LineSpacing)) * 2D;
            newpar.FontFamily = new FontFamily(rtfpar.Format.FontName);
            //newpar.Margin = new Thickness(rtfpar.Format.xxx);


            fdoc.Blocks.Add(newpar);

            foreach (RTFDomElement domelm in rtfpar.Elements)
            {
               if (domelm.GetType() == typeof(RTFDomText))
               {
                  RTFDomText rtftext = (RTFDomText)domelm;

                  EditableRun erun = new (rtftext.Text)
                  {
                     FontSize = rtftext.Format.FontSize
                  };

                  if (rtftext.Format.Bold)
                     erun.FontWeight = FontWeight.Bold;
                  
                  if (rtftext.Format.Italic)
                     erun.FontStyle = FontStyle.Italic; 

                  if (rtftext.Format.Underline)
                     erun.TextDecorations = TextDecorations.Underline;

                  if (rtftext.Format.Strikeout)
                     erun.TextDecorations = TextDecorations.Strikethrough; 

                  if (rtftext.Format.Subscript)
                  {
                     erun.BaselineAlignment = BaselineAlignment.TextBottom; 
                     erun.FontSize /= 1.5;  
                  }

                  if (rtftext.Format.Superscript)
                  { 
                     erun.BaselineAlignment = BaselineAlignment.Top; 
                     erun.FontSize /= 1.5; 
                  }
                  
                  erun.Foreground = new SolidColorBrush(rtftext.Format.TextColor);
                  erun.Background = new SolidColorBrush(rtftext.Format.BackColor);
                  erun.FontFamily = new FontFamily(rtftext.Format.FontName);

                  newpar.Inlines.Add(erun);

               }
            }
         }
      }

      //fdoc.PagePadding = PageMargin? pMarg = mainDocPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.PageMargin>().FirstOrDefault()!;
      //Page size
      //   fdoc.PageWidth = TwipToPix(Convert.ToDouble((uint)pSize.Width));
      //   fdoc.PageHeight = TwipToPix(Convert.ToDouble((uint)pSize.Height));
      
          
   }
 

}


