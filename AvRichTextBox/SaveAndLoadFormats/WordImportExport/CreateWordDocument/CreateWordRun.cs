using Avalonia.Media;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using static AvRichTextBox.HelperMethods;
using DOW = DocumentFormat.OpenXml.Wordprocessing;

namespace AvRichTextBox;

internal static partial class WordConversions
{
   internal static DOW.Run GetWordDocRun(EditableRun edRun)
   {
      string? thisRunText = "";
      thisRunText = edRun.Text;
      
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
            foreach (TextDecoration td in edRun.TextDecorations)
            {
               switch (td.Location)
               {
                  case TextDecorationLocation.Underline: RunProp.AppendChild(new DOW.Underline() { Val = UnderlineValues.Single, Color = "Black" }); break;
                  case TextDecorationLocation.Strikethrough: RunProp.AppendChild(new DOW.Strike()); break;
                  case TextDecorationLocation.Overline: break; // Word doesn't inherently support overline, so ignore
               }
            }
         }

         if (edRun.FontWeight == FontWeight.Bold)
            RunProp.AppendChild(new DOW.Bold());

         if (edRun.FontStyle == FontStyle.Italic)
            RunProp.AppendChild(new DOW.Italic());

         if (edRun.Background != null && edRun.Background != Brushes.Transparent)
         {
            var Hlight = new Highlight() { Val = BrushToHighlightColorValue(edRun.Background) };
            RunProp.AppendChild(Hlight);
         }

         if (edRun.Foreground != null)
         {
            var foreColor = new DOW.Color() { Val  = edRun.Foreground.ToString() };
            RunProp.AppendChild(foreColor);
         }

         switch (edRun.BaselineAlignment)
         {
            case BaselineAlignment.Baseline: RunProp.AppendChild(new VerticalTextAlignment() { Val = VerticalPositionValues.Baseline }); break;
            case BaselineAlignment.Superscript: RunProp.AppendChild(new VerticalTextAlignment() { Val = VerticalPositionValues.Superscript }); break;
            case BaselineAlignment.Subscript: RunProp.AppendChild(new VerticalTextAlignment() { Val = VerticalPositionValues.Subscript }); break;
         }

         // Get font
         var rFont = new RunFonts();
         

         var fntSize = default(double);
         rFont.Ascii = edRun.FontFamily.ToString();
         rFont.HighAnsi = edRun.FontFamily.ToString();
         //rFont.EastAsia = edRun.FontFamily.ToString();
         rFont.EastAsia = "Meiryo UI";  // temporary default
         
         fntSize = PixelsToPoints(edRun.FontSize * 2);

         RunProp.AppendChild(rFont);
         RunProp.AppendChild(new FontSize() { Val = fntSize.ToString() });

         //Attach run properties
         newrun.AppendChild(RunProp);

         // Must parse line breaks
         if (!string.IsNullOrEmpty(runtext.Text))
         {
            //if (runtext.GetText.Contains(Constants.vbLf))
            if (runtext.Text.Contains('\n'))
               ParseRunText(ref newrun, runtext.Text);
            else

               newrun.AppendChild(runtext);
         }

      }
      catch (Exception ex) { Debug.WriteLine($"Failed to create run: {edRun.Text}\nexception: {ex.Message}"); }

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
            if (r.LastChild is not Break)
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
