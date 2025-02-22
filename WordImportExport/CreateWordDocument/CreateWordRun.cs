using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DOW = DocumentFormat.OpenXml.Wordprocessing;
using Avalonia.Media;
using System;
using System.Diagnostics;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

internal static partial class WordConversions
{
   internal static DOW.Run GetWordDocRun(EditableRun edRun)
   {
      string? thisRunText = "";
      thisRunText = edRun.Text;
      //Debug.WriteLine("thisRuntext= " + thisRunText);
      
      var newrun = new DOW.Run();

      try
      {

         var runtext = new Text(thisRunText!) // convert text to "wordprocessing.text" form
         {
            Space = SpaceProcessingModeValues.Preserve
         };  

         var RunProp = new RunProperties();

         if (edRun.TextDecorations != null)
         {
            foreach (TextDecoration td in edRun.TextDecorations!)
            {
               switch (td.Location)
               {
                  case TextDecorationLocation.Underline:
                     RunProp.AppendChild(new DOW.Underline() { Val = UnderlineValues.Single, Color = "Black" });
                     break;
                  case TextDecorationLocation.Overline: { break; }
                  case TextDecorationLocation.Baseline: { break; }
                  case TextDecorationLocation.Strikethrough:
                     RunProp.AppendChild(new DOW.Strike());
                     break; 
               }
            }
         }

         if (edRun.FontWeight == FontWeight.Bold)
            RunProp.AppendChild(new DOW.Bold());

         if (edRun.FontStyle == FontStyle.Italic)
            RunProp.AppendChild(new DOW.Italic());

         if (edRun.Background != null)
         {
            var Hlight = new  Highlight() { Val = BrushToHighlightColorValue(edRun.Background) };
            RunProp.AppendChild(Hlight);
         }

         if (edRun.Foreground != null)
         {
            var foreColor = new DocumentFormat.OpenXml.Wordprocessing.Color() { Val  = edRun.Foreground.ToString() };
            RunProp.AppendChild(foreColor);
         }

         switch (edRun.BaselineAlignment)
         {
            case BaselineAlignment.Baseline: RunProp.AppendChild(new VerticalTextAlignment() { Val = VerticalPositionValues.Baseline }); break;
            case BaselineAlignment.TextTop: RunProp.AppendChild(new VerticalTextAlignment() { Val = VerticalPositionValues.Superscript }); break;
            case BaselineAlignment.Bottom: RunProp.AppendChild(new VerticalTextAlignment() { Val = VerticalPositionValues.Subscript }); break;
         }

         // Get font
         var rFont = new RunFonts();
         var fntSize = default(double);
         rFont.Ascii = edRun.FontFamily.ToString();
         fntSize = edRun.FontSize * 0.75 * 2;  // converts pixels to points

         RunProp.AppendChild(rFont);
         RunProp.AppendChild(new FontSize() { Val = fntSize.ToString() });

         //Attach run properties
         newrun.AppendChild(RunProp);

         // Must parse line breaks
         if (!string.IsNullOrEmpty(runtext.Text))
         {
            //if (runtext.Text.Contains(Constants.vbLf))
            if (runtext.Text.Contains("\n"))
               ParseRunText(ref newrun, runtext.Text);
            else

               newrun.AppendChild(runtext);
         }

      }
      catch (Exception ex) { Debug.WriteLine("Failed to create run: " + edRun.Text + "\nexception: " + ex.Message); }

      return newrun;
   }

   public static void ParseRunText(ref DOW.Run r, string tData)
   {
      //var newLineArray = new[] { Constants.vbLf };
      //var textArray = tData.Split(newLineArray, StringSplitOptions.None);
      var textArray = tData.Split("\n".ToCharArray(), StringSplitOptions.None);

      foreach (string line in textArray)
      {
         if (string.IsNullOrEmpty(line))
         {
            if (!(r.LastChild is Break))
               r.AppendChild(new Break());
         }
         else
         {
            var txt = new Text
            {
               Text = line
            };
            r.AppendChild(txt);
         }
      }
   }

}
