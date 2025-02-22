using Avalonia;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Linq;
using static AvRichTextBox.HelperMethods;
using RtfDomParser;

namespace AvRichTextBox;

internal static partial class RtfConversions
{
   internal static RTFDomDocument GetRtfFromFlowDocument(FlowDocument fdoc)
   {
      RTFDomDocument rtfdoc = new();
      
      foreach (Block b in fdoc.Blocks)
      {
         if (b.IsParagraph)
         {
            Paragraph? p = b as Paragraph;

            RTFDomParagraph rtfpar = new ();

            switch (p!.TextAlignment)
            {
               case TextAlignment.Left: rtfpar.Format.Align = RTFAlignment.Left; break;
               case TextAlignment.Center: rtfpar.Format.Align = RTFAlignment.Center; break;
               case TextAlignment.Right: rtfpar.Format.Align = RTFAlignment.Right; break;
               case TextAlignment.Justify: rtfpar.Format.Align = RTFAlignment.Justify; break;
            }

            rtfpar.Format.LineSpacing = (int)(PointsToPixels(PixToTwip(p.LineHeight)) / 2D);
            rtfpar.Format.FontName = p.FontFamily.Name;

            rtfdoc.AppendChild(rtfpar);

            foreach (IEditable ied in p.Inlines)
            {
               if (ied.GetType() == typeof(EditableRun))
               {
                  EditableRun erun = (EditableRun) ied;
                  RTFDomText rtftext = new() { Text = erun.Text };
                  rtftext.Format.FontSize = (float)(erun.FontSize / 2D);
                  rtftext.Format.FontName = erun.FontFamily.Name;
                  rtftext.Format.TextColor = erun.Foreground == null ? Colors.Black : ((SolidColorBrush)erun.Foreground).Color;
                  rtftext.Format.BackColor = erun.Background == null ? Colors.Transparent : ((SolidColorBrush)erun.Background).Color;

                  switch (erun.FontWeight)
                  {
                     case FontWeight.Bold:
                        rtftext.Format.Bold = true;
                        break;
                     case FontWeight.Normal:
                        rtftext.Format.Bold = false;
                        break;
                  }

                  switch (erun.FontStyle)
                  {
                     case FontStyle.Italic:
                        rtftext.Format.Italic = true;
                        break;
                     case FontStyle.Normal:
                        rtftext.Format.Italic = false;
                        break;
                  }


                  if (erun.TextDecorations != null)
                  {
                     foreach (TextDecoration td in erun.TextDecorations!)
                     {
                        switch (td.Location)
                        {
                           case TextDecorationLocation.Underline: rtftext.Format.Underline = true; break;
                           case TextDecorationLocation.Strikethrough: rtftext.Format.Strikeout = true; break;
                           default: 
                              rtftext.Format.Underline = false;
                              rtftext.Format.Strikeout = false;
                              break;
                        }
                     }
                  }

                  switch (erun.BaselineAlignment)
                  {
                     case BaselineAlignment.TextBottom:
                        rtftext.Format.Subscript = true;
                        break;
                     case BaselineAlignment.Top:
                        rtftext.Format.Superscript = true;
                        break;
                  }

                  rtfpar.AppendChild(rtftext);

               }
            }


         }
      }


      return rtfdoc;
          
   }
 

}


