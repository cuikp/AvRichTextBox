using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace AvRichTextBox;

internal partial class EditableParagraph
{
 
   private InlineCollection GetFormattedInlines()
   {
   
      InlineCollection returnInlines = [];
      if (this.DataContext is Paragraph p)
      {
         foreach (IEditable ied in p.Inlines)
         {
            if (ied is EditableRun erun)
            {               
               switch (erun.BaselineAlignment)
               {
                  case BaselineAlignment.Subscript:
                  case BaselineAlignment.Superscript:
                     returnInlines.Add(CreateScriptRun(erun));
                     continue;
               }
            }

            returnInlines.Add((Inline)ied);
                       
         }
      }
      
      return returnInlines;
   }

   private static Run CreateScriptRun(EditableRun erun)
   {
      Run scriptRun = new(erun.Text)
      {
         FontSize = erun.FontSize * 0.75,
         FontWeight = erun.FontWeight,
         FontStyle = erun.FontStyle,
         FontFamily = erun.FontFamily,
         Foreground = erun.Foreground,
         Background = erun.Background,
         TextDecorations = erun.TextDecorations,
         //BaselineAlignment = erun.BaselineAlignment == BaselineAlignment.Superscript ? BaselineAlignment.Superscript : BaselineAlignment.TextBottom
         BaselineAlignment = erun.BaselineAlignment == BaselineAlignment.Superscript ? BaselineAlignment.Superscript : BaselineAlignment.Subscript
      };

      return scriptRun;

   }


   internal void UpdateInlines()
   {
      if (this.DataContext is not Paragraph par) return;
      
      if (par.Inlines != null)
         this.Inlines = GetFormattedInlines();

      //foreach (Inline thisIL in this.Inlines!)
      //   Debug.WriteLine("1:\n" + ((Run)thisIL).GetText + " ::: " + thisIL.FontWeight);

      this.InvalidateMeasure();
      this.InvalidateVisual();
   }

   private void UpdateParRelativePos()
   {
      if (ThisPar != null)
      {
         ThisPar.TextLayout = this.TextLayout;
         
         if (myDocIC != null)
         {
            if (this.TranslatePoint(new Point(0, 0), myDocIC) is Point p)
            {
               ThisPar.DocICRelativeTop = p.Y + ThisPar.Margin.Top;
               ThisPar.DocICRelativeLeft = p.X + ThisPar.Margin.Left;
            }
         }
      }
   }

   public void UpdateVMFromEPStart()
   {
      this.SetValue(TextLayoutInfoStartRequestedProperty, false);
      this.UpdateLayout();
      UpdateParRelativePos();

   }

   public void UpdateVMFromEPEnd()
   {
      this.SetValue(TextLayoutInfoEndRequestedProperty, false);
      this.UpdateLayout();
      UpdateParRelativePos();
      
   }



}

