using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Linq;
using static AvRichTextBox.HelperMethods;
using RtfDomParser;
using System.Collections.Generic;
using Avalonia.Controls.Documents;
using DynamicData;
using DocumentFormat.OpenXml.Office2016.Excel;
using System.Text.RegularExpressions;
using System.Xml;
using Avalonia.Media.Imaging;
using System.IO;

namespace AvRichTextBox;

internal static partial class RtfConversions
{
   internal static string DefaultEastAsiaFont = "";
   internal static string DefaultAsciiFont = "";

   internal static List<IEditable> GetInlinesFromRtf(RTFDomDocument rtfdoc)
   {

      List<IEditable> returnList = [];
      
      int domParCount = rtfdoc.Elements.OfType<RTFDomParagraph>().Count();
      int parno = 0;
      foreach (RTFDomElement rtfelm in rtfdoc.Elements)
      {
         if (rtfelm.GetType() == typeof(RTFDomParagraph))
         {
            parno++;

            RTFDomParagraph rtfpar = (RTFDomParagraph)rtfelm;

            //set 
            //Paragraph newpar = new();

            //switch (rtfpar.Format.Align)
            //{
            //   case RTFAlignment.Left: newpar.TextAlignment = TextAlignment.Left; break;
            //   case RTFAlignment.Center: newpar.TextAlignment = TextAlignment.Center; break;
            //   case RTFAlignment.Right: newpar.TextAlignment = TextAlignment.Right; break;
            //   case RTFAlignment.Justify: newpar.TextAlignment = TextAlignment.Justify; break;
            //}
            //newpar.LineHeight = TwipToPix(PixelsToPoints(rtfpar.Format.LineSpacing)) * 2D;
            //newpar.FontFamily = new FontFamily(rtfpar.Format.FontName);
            //newpar.Margin = new Thickness(rtfpar.Format.xxx);

            
            if (rtfpar.Elements.Count > 0)
            {
               List<IEditable> addInlines = GetRtfTextElementsAsInlines(rtfpar.Elements);
               //if (parno != domParCount)  // Don't add \r at the last run as it's unnecessary???
                addInlines.Last().InlineText += "\r";
               returnList.AddRange(addInlines);
            }
            
         }
      }

      return returnList;

   }

   internal static void GetFlowDocumentFromRtf(RTFDomDocument rtfdoc, FlowDocument fdoc)
   {
      double leftMargin = Math.Round(TwipToPix(rtfdoc.LeftMargin));
      double topMargin = Math.Round(TwipToPix(rtfdoc.TopMargin));
      double rightMargin = Math.Round(TwipToPix(rtfdoc.RightMargin));
      double bottomMargin = Math.Round(TwipToPix(rtfdoc.BottomMargin));

      fdoc.PagePadding = new Avalonia.Thickness(leftMargin, topMargin, rightMargin, bottomMargin);

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

            newpar.LineSpacing = TwipToPix(PixelsToPoints(rtfpar.Format.LineSpacing)) * 2D;
            //newpar.LineHeight = TwipToPix(PixelsToPoints(rtfpar.Format.LineSpacing)) * 2D;

            newpar.FontFamily = new FontFamily(rtfpar.Format.FontName);
            //newpar.Margin = new Thickness(rtfpar.Format.xxx);

            fdoc.Blocks.Add(newpar);
                        
            List<IEditable> addInlines = GetRtfTextElementsAsInlines(rtfpar.Elements);

            newpar.Inlines.AddRange(addInlines);

         }
         else
         {
            Debug.WriteLine("unknown rtfelm=" + rtfelm.GetType().ToString());
         }
      }

      //fdoc.PagePadding = PageMargin? pMarg = mainDocPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.PageMargin>().FirstOrDefault()!;
      //Page size
      //   fdoc.PageWidth = TwipToPix(Convert.ToDouble((uint)pSize.Width));
      //   fdoc.PageHeight = TwipToPix(Convert.ToDouble((uint)pSize.Height));
      
          
   }

   private static List<IEditable> GetRtfTextElementsAsInlines(RTFDomElementList elements)
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
            EditableInlineUIContainer eIUC = new(null!);
            eIUC.FontFamily = "Image"; //???

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
            //EditableRun erun = new(DecodeRtfUnicode(rtftext2.Text))
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
            {
               erun.BaselineAlignment = BaselineAlignment.TextBottom;
               erun.FontSize /= 1.5;
            }

            if (rtftext2.Format.Superscript)
            {
               erun.BaselineAlignment = BaselineAlignment.Top;
               erun.FontSize /= 1.5;
            }

            erun.Foreground = new SolidColorBrush(rtftext2.Format.TextColor);
            erun.Background = new SolidColorBrush(rtftext2.Format.BackColor);
            erun.FontFamily = new FontFamily(rtftext2.Format.FontName);
            //erun.FontFamily = new FontFamily("Meiryo");
            //Debug.WriteLine("erun: " + erun.FontFamily + "  (" + erun.Text + ")");

            returnList.Add(erun);

         }
         else
         {
            Debug.WriteLine("unkjnown: " + domelm.GetType().ToString());
         }
      }

      return returnList;
   }

   private static string DecodeRtfUnicode(string rtfText)
   {
      return Regex.Replace(rtfText, @"\\u(-?\d+)\?", match =>
      {
         int unicodeValue = int.Parse(match.Groups[1].Value);
         return char.ConvertFromUtf32(unicodeValue);
      });
   }

}


