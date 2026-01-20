using Avalonia.Media;
using Avalonia.VisualTree;
using System.Text.RegularExpressions;

namespace AvRichTextBox;

public partial class RichTextBox
{

   EditableParagraph? currentMouseOverEP = null;

   internal void EditableParagraph_MouseMove(EditableParagraph edPar, int charIndex)
   {
      if (!PointerDownOverRTB)
         currentMouseOverEP = edPar;

   }


   private void EditableParagraph_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      this.Focus();
   }


   internal int SelectionOrigin = 0;
   bool PointerDownOverRTB = false;

   private void FlowDocSV_PointerPressed(object? sender, PointerPressedEventArgs e)
   {
      if (currentMouseOverEP == null) return;

      PointerDownOverRTB = true;

      TextHitTestResult hitCarIndex = currentMouseOverEP.TextLayout.HitTestPoint(e.GetPosition(currentMouseOverEP));
      Paragraph thisPar = (Paragraph)currentMouseOverEP.DataContext!;
      if (thisPar == null) return;
      SelectionOrigin = thisPar.StartInDoc + hitCarIndex.TextPosition;

      //Clear all selections in all paragraphs      
      foreach (Paragraph p in FlowDoc.Blocks.Where(pp => pp.SelectionLength != 0)) { p.ClearSelection();  }

      int sel_start_idx = SelectionOrigin;
      int sel_end_idx = SelectionOrigin;

      if(e.ClickCount > 1 &&
         e.Source is Visual source_visual &&
         e.GetCurrentPoint(source_visual).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed) 
      { // word/paragraph selection added by tkefauver
         if(e.ClickCount == 2) 
         {
            // dbl click, select word
            var word_matches = WordMatchesRegex().Matches(thisPar.Text);
            foreach(Match wm in word_matches) 
            {
               int wm_start_idx = thisPar.StartInDoc + wm.Index;
               int wm_end_idx = wm_start_idx + wm.Length;
               if(SelectionOrigin >= wm_start_idx && SelectionOrigin <= wm_end_idx) 
               {
                  sel_start_idx = wm_start_idx;
                  sel_end_idx = wm_end_idx;
                  break;
               }
            }
         } 
         else if(e.ClickCount == 3) 
         {
            // triple click select block
            sel_start_idx = thisPar.StartInDoc;
            sel_end_idx = sel_start_idx + thisPar.TextLength;
         } 
      }

      FlowDoc.Selection.Start = sel_start_idx;
      FlowDoc.Selection.End = sel_end_idx;

      //e.Pointer.Capture(null);
      //e.Pointer.Capture(this);

   }

   private void FlowDocSV_PointerMoved(object? sender, PointerEventArgs e)
   {      

      if (PointerDownOverRTB)
      {
         EditableParagraph overEP = null!;

         double RTBTransformedY = this.GetTransformedBounds()!.Value.Clip.Y;

         foreach (KeyValuePair<EditableParagraph, Rect> kvp in VisualHelper.GetVisibleEditableParagraphs(FlowDocSV))
         {  //Debug.WriteLine("visiPar = " + kvp.Key.Text);

            Point ePoint = e.GetCurrentPoint(FlowDocSV).Position;
            Rect thisEPRect = new(kvp.Value.X - DocIC.Margin.Left, kvp.Value.Y, kvp.Value.Width, kvp.Value.Height);

            double adjustedMouseY = ePoint.Y + RTBTransformedY;
            bool epContainsPoint = thisEPRect.Top <= adjustedMouseY && thisEPRect.Bottom >= adjustedMouseY;
            
            if (epContainsPoint)
               { overEP = kvp.Key; break; }
         }

         if (overEP != null)
         {

            TextHitTestResult hitCharIndex = overEP.TextLayout.HitTestPoint(e.GetPosition(overEP));
            int charIndex = hitCharIndex.TextPosition;

            Paragraph thisPar = (Paragraph)overEP.DataContext!;
         
            if (thisPar.StartInDoc + charIndex < SelectionOrigin)
            {  //Debug.WriteLine("startindoc = " + ThisPar.StartInDoc + " :::charindex = " +  charIndex + " :::selectionorigin= " + SelectionOrigin);
               FlowDoc.SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeLeft;
               FlowDoc.Selection.End = SelectionOrigin;
               FlowDoc.Selection.Start = thisPar.StartInDoc + charIndex;
            }
            else
            {
               FlowDoc.SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeRight;
               FlowDoc.Selection.Start = SelectionOrigin;
               FlowDoc.Selection.End = thisPar.StartInDoc + charIndex;
            }

            FlowDoc.EnsureSelectionContinuity();
         }
      }

   }

   private void RichTextBox_PointerReleased(object? sender, PointerReleasedEventArgs e)
   {
      PointerDownOverRTB = false;
      
   }

   private void FlowDocSV_PointerReleased(object? sender, PointerReleasedEventArgs e)
   {
      //e.Pointer.Capture(null);
      PointerDownOverRTB = false;

   }

   private void RichTextBox_PointerExited(object? sender, PointerEventArgs e)
   {
      //PointerDownOverRTB = false;

   }

   [GeneratedRegex("\\w+")]
   private static partial Regex WordMatchesRegex();
}


