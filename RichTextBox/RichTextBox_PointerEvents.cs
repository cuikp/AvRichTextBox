using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Input;
using Avalonia.Media;
using DynamicData;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AvRichTextBox;

public partial class RichTextBox
{

   EditableParagraph? currentMouseOverEP = null;

   private void EditableParagraph_MouseMove(EditableParagraph edPar, int charIndex)
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
      SelectionOrigin = thisPar.StartInDoc + hitCarIndex.TextPosition;
      FlowDoc.Selection.Start = SelectionOrigin;

      FlowDoc.Selection.CollapseToStart();


      //Clear all selections in all paragraphs      
      foreach (Paragraph p in FlowDoc.Blocks.Where(pp => pp.SelectionLength != 0)) { p.ClearSelection(); }
      
     
      //e.Pointer.Capture(null);
      //e.Pointer.Capture(this);
   }

   private void FlowDocSV_PointerMoved(object? sender, PointerEventArgs e)
   {
      if (PointerDownOverRTB)
      {

         EditableParagraph overEP = null!;

         foreach (KeyValuePair<EditableParagraph, Rect> kvp in VisualHelper.GetVisibleEditableParagraphs(this))
         {
            double parTopMargin = ((Paragraph)kvp.Key.DataContext!).Margin.Top;
            Rect thisEPRect = new (kvp.Value.X - DocIC.Margin.Left, kvp.Value.Y - (DocIC.Margin.Top * 2 + parTopMargin * 2), kvp.Value.Width, kvp.Value.Height);
            Point ePoint = e.GetCurrentPoint(this).Position;
            bool epContainsPoint = thisEPRect.Top <= ePoint.Y + parTopMargin * 2 && thisEPRect.Bottom >= ePoint.Y;
            if (epContainsPoint)
            { overEP = kvp.Key; break; }
         }

         if (overEP != null)
         {
            TextHitTestResult hitCharIndex = overEP.TextLayout.HitTestPoint(e.GetPosition(overEP));
            int charIndex = hitCharIndex.TextPosition;

            Paragraph thisPar = (Paragraph)overEP.DataContext!;

            if (thisPar.StartInDoc + charIndex < SelectionOrigin)
            {
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

}


