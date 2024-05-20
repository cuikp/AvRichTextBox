using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DOW = DocumentFormat.OpenXml.Wordprocessing;
using Avalonia.Media;
using System;
using System.Diagnostics;

namespace AvRichTextBox;

public partial class FlowDocument
{
   public static DOW.Run GetWordDocRun(EditableRun edRun)
   {
      string? thisRunText = "";

      thisRunText = edRun.Text;

      //Debug.WriteLine("thisRuntext= " + thisRunText);
      
      var newrun = new DOW.Run();

      try
      {

       var runtext = new Text(thisRunText!);  // convert text to "wordprocessing.text" form
         runtext.Space = SpaceProcessingModeValues.Preserve;


         var RunProp = new RunProperties();

         if (((EditableRun)edRun).TextDecorations != null)
         {
            foreach (TextDecoration td in ((EditableRun)edRun).TextDecorations!)
            {
               switch (td.Location)
               {
                  case TextDecorationLocation.Underline:
                     RunProp.AppendChild(new DOW.Underline() { Val = UnderlineValues.Single, Color = "Black" });
                     break;
                  case TextDecorationLocation.Overline: { break; }
                  case TextDecorationLocation.Baseline: { break; }
                  case TextDecorationLocation.Strikethrough: { break; }
               }
            }
         }

         if (((EditableRun)edRun).FontWeight == FontWeight.Bold)
            RunProp.AppendChild(new DOW.Bold());

         if (((EditableRun)edRun).FontStyle == FontStyle.Italic)
            RunProp.AppendChild(new DOW.Italic());

         if (!(((EditableRun)edRun).Background == null))
         {
            var Hlight = new Highlight() { Val = BrushToHighlightColorValue(((EditableRun)edRun).Background) };
            RunProp.AppendChild(Hlight);
         }

         // Debug.WriteLine(edRun.BaselineAlignment.ToString() & vbCr & runtext.Text)

         if (((EditableRun)edRun).BaselineAlignment == BaselineAlignment.Subscript)
            RunProp.AppendChild(new VerticalTextAlignment() { Val = VerticalPositionValues.Subscript });
         if (((EditableRun)edRun).BaselineAlignment == BaselineAlignment.Superscript | ((EditableRun)edRun).BaselineAlignment == BaselineAlignment.TextTop)
            RunProp.AppendChild(new VerticalTextAlignment() { Val = VerticalPositionValues.Superscript });

         // Get font
         var rFont = new RunFonts();
         var fntSize = default(double);
         rFont.Ascii = ((EditableRun)edRun).FontFamily.ToString();
         fntSize = ((EditableRun)edRun).FontSize * 0.75 * 2;  // converts pixels to points

         RunProp.AppendChild(rFont);
         RunProp.AppendChild(new FontSize() { Val = fntSize.ToString() });


         // Get font color
         Avalonia.Media.Color bgcolor = ((SolidColorBrush)((EditableRun)edRun).Foreground!).Color;
         RunProp.AppendChild(new DOW.Color() { Val = bgcolor.ToString().Substring(3) });  // Delete "#FF" portion

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
      catch { }

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
            var txt = new Text();
            txt.Text = line;
            r.AppendChild(txt);
         }
      }
   }

}
