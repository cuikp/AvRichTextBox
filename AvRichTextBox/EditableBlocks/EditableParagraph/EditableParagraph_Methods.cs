using Avalonia.Controls;
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
                     returnInlines.Add(CreateContainerFromERun(erun, RichTextBox.SubscriptTG));
                     continue;

                  case BaselineAlignment.Superscript:
                     returnInlines.Add(CreateContainerFromERun(erun, RichTextBox.SuperscriptTG));
                     continue;
               }
            }

            returnInlines.Add((Inline)ied);
            
         }
      }
      
      return returnInlines;
   }

   private static InlineUIContainer CreateContainerFromERun(EditableRun erun, TransformGroup rTransform)
   {
      TextBlock subBlock = new()
      {
         Inlines = [new Run(erun.Text)
                        {
                           FontSize = erun.FontSize,
                           FontWeight = erun.FontWeight,
                           FontStyle = erun.FontStyle,
                           FontFamily = erun.FontFamily,
                           Foreground = erun.Foreground,
                           Background = erun.Background,
                           TextDecorations = erun.TextDecorations,
                        }],
         RenderTransform = rTransform
      };
      return new InlineUIContainer (subBlock);

   }

   internal void UpdateInlines()
   {
      if (((Paragraph)this.DataContext!).Inlines != null)
         this.Inlines = GetFormattedInlines();

      //foreach (Inline thisIL in this.Inlines!)
      //   Debug.WriteLine("1:\n" + ((Run)thisIL).GetText + " ::: " + thisIL.FontWeight);

      //this.Height = this.Inlines[0].get

      this.InvalidateMeasure();
      this.InvalidateVisual();
   }

   public void UpdateVMFromEPStart()
   {
      SelectionStartRect_Changed?.Invoke(this);
      this.SetValue(TextLayoutInfoStartRequestedProperty, false);

   }

   public void UpdateVMFromEPEnd()
   {
      SelectionEndRect_Changed?.Invoke(this);
      this.SetValue(TextLayoutInfoEndRequestedProperty, false);
   }

   //private int GetClosestIndex(int lineNo, double distanceFromLeft, int direction)
   //{
   //   CharacterHit chit = this.TextLayout.TextLines[lineNo + direction].GetCharacterHitFromDistance(distanceFromLeft);

   //   double CharDistanceDiffThis = Math.Abs(distanceFromLeft - this.TextLayout.HitTestTextPosition(chit.FirstCharacterIndex).Left);
   //   double CharDistanceDiffNext = Math.Abs(distanceFromLeft - this.TextLayout.HitTestTextPosition(chit.FirstCharacterIndex + 1).Left);

   //   if (CharDistanceDiffThis > CharDistanceDiffNext)
   //      return chit.FirstCharacterIndex + 1;
   //   else
   //      return chit.FirstCharacterIndex;


   //}


}

