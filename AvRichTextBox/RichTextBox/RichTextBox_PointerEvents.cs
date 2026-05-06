using Avalonia.Controls;
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


   private void EditableParagraph_LostFocus(object? sender, FocusChangedEventArgs e)
   {
      this.Focus();
   }


   internal int SelectionOrigin = 0;
   bool PointerDownOverRTB = false;

   private void FlowDocSV_PointerPressed(object? sender, PointerPressedEventArgs e)
   {
      if (currentMouseOverEP == null) return;
      if (currentMouseOverEP.DataContext is not Paragraph thisPar) return;

      // Hyperlink mouse down processing
      if (HyperlinkClickable)
      {
         EditableHyperlink thisHyperlink = currentMouseOverEP.CurrentOverHyperlink;
         var psi = new ProcessStartInfo { FileName = currentMouseOverEP.CurrentOverHyperlink.NavigateUri, UseShellExecute = true };
         Process.Start(psi);
         return;
      }

      // Normal mouse down processing
      TextHitTestResult hitCarIndex = currentMouseOverEP.TextLayout.HitTestPoint(e.GetPosition(currentMouseOverEP));

      int clickPosition = thisPar.StartInDoc + hitCarIndex.TextPosition;

      // Shift+Click: extend selection from current anchor
      if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
      {
         if (e.Properties.IsRightButtonPressed)
         {
            if (FlowDoc.Selection.Length == 0)
               FlowDoc.Select(SelectionOrigin, 0);
            return;
         }

         PointerDownOverRTB = true;
         SelectionOrigin = GetSelectionAnchor();

         // Set selection from anchor to click point
         SetSelectionFromAnchorTo(clickPosition);

         return;
      }

      SelectionOrigin = clickPosition;


      if (e.Properties.IsRightButtonPressed)
      {
         if (FlowDoc.Selection.Length == 0)
            FlowDoc.Select(SelectionOrigin, 0);
         return;
      }

      PointerDownOverRTB = true;

      int sel_start_idx = SelectionOrigin;
      int sel_end_idx = SelectionOrigin;

      if (e.ClickCount > 1 &&
         e.Source is Visual source_visual &&
         e.GetCurrentPoint(source_visual).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
      { // word/paragraph selection added by tkefauver
         if (e.ClickCount == 2)
         {
            // dbl click, select word
            var word_matches = WordMatchesRegex().Matches(thisPar.Text);
            foreach (Match wm in word_matches)
            {
               int wm_start_idx = thisPar.StartInDoc + wm.Index;
               int wm_end_idx = wm_start_idx + wm.Length;
               if (SelectionOrigin >= wm_start_idx && SelectionOrigin <= wm_end_idx)
               {
                  sel_start_idx = wm_start_idx;
                  sel_end_idx = wm_end_idx;
                  break;
               }
            }
         }
         else if (e.ClickCount == 3)
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

   private int GetSelectionAnchor()
   {
      // Determine anchor point based on current extend mode
      if (FlowDoc.Selection.Length > 0)
      {
         return FlowDoc.SelectionExtendMode == FlowDocument.ExtendMode.ExtendModeLeft
             ? FlowDoc.Selection.End
             : FlowDoc.Selection.Start;
      }

      return FlowDoc.Selection.Start;
   }

   private void SetSelectionFromAnchorTo(int position)
   {
      if (position < SelectionOrigin)
      {
         FlowDoc.Selection.BiasForwardStart = true;
         FlowDoc.Selection.UpdateContextStart();
         FlowDoc.SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeLeft;
         FlowDoc.Selection.End = SelectionOrigin;
         FlowDoc.Selection.Start = position;
      }
      else
      {
         FlowDoc.Selection.BiasForwardEnd = false;
         FlowDoc.Selection.UpdateContextEnd();
         FlowDoc.SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeRight;
         FlowDoc.Selection.Start = SelectionOrigin;
         FlowDoc.Selection.End = position;
      }
   }

   private void FlowDocSV_PointerMoved(object? sender, PointerEventArgs e)
   {
      if (PointerDownOverRTB)
      {
         EditableParagraph overEP = null!;

         double scaleXTransform = 1;
         double scaleYTransform = 1;
         if (FlowDocSV.Parent is LayoutTransformControl ltcont)
         {
            if (ltcont.LayoutTransform is ScaleTransform scTrans)
            {
               scaleXTransform = scTrans.ScaleX;
               scaleYTransform = scTrans.ScaleY;
            }
         }

         double RTBTransformedY = this.GetTransformedBounds()!.Value.Clip.Y;

         foreach (KeyValuePair<EditableParagraph, Rect> kvp in VisualHelper.GetVisibleEditableParagraphs(FlowDocSV))
         {
            Point ePoint = e.GetCurrentPoint(FlowDocSV).Position;
            ePoint = ePoint.Transform(Matrix.CreateScale(scaleXTransform, scaleYTransform));

            Rect thisEPRect = new(kvp.Value.X - this.Padding.Left, kvp.Value.Y - this.Padding.Top, kvp.Value.Width, kvp.Value.Height);

            double adjustedMouseY = ePoint.Y + RTBTransformedY;
            bool epContainsPoint = thisEPRect.Top <= adjustedMouseY && thisEPRect.Bottom >= adjustedMouseY;

            if (epContainsPoint)
            { overEP = kvp.Key; break; }
         }

         if (overEP != null)
         {
            TextHitTestResult hitCharIndex = overEP.TextLayout.HitTestPoint(e.GetPosition(overEP));
            int charIndex = hitCharIndex.TextPosition;

            if (overEP.DataContext is not Paragraph thisPar) return;

            if (thisPar.StartInDoc + charIndex < SelectionOrigin)
            {
               FlowDoc.Selection.BiasForwardStart = true;
               FlowDoc.Selection.UpdateContextStart();
               FlowDoc.SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeLeft;
               FlowDoc.Selection.End = SelectionOrigin;
               FlowDoc.Selection.Start = thisPar.StartInDoc + charIndex;

               //leap over hyperlink
               if (FlowDoc.GetStartInline(FlowDoc.Selection.Start) is EditableHyperlink hyperlink)
                  FlowDoc.Selection.Start = thisPar.StartInDoc + hyperlink.TextPositionOfInlineInParagraph;

            }
            else
            {
               FlowDoc.Selection.BiasForwardEnd = false;
               FlowDoc.Selection.UpdateContextEnd();
               FlowDoc.SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeRight;
               FlowDoc.Selection.Start = SelectionOrigin;
               FlowDoc.Selection.End = thisPar.StartInDoc + charIndex;

               //leap over hyperlink
               if (FlowDoc.GetStartInline(FlowDoc.Selection.End - 1) is EditableHyperlink hyperlink)
                  FlowDoc.Selection.End = thisPar.StartInDoc + hyperlink.TextPositionOfInlineInParagraph + hyperlink.InlineLength;

            }
         }
      }
      else
      {
         if (currentMouseOverEP == null)
         {
            this.Cursor = Cursor.Default;
            return;
         }

         // Hyperlink mouse down processing
         if (currentMouseOverEP.IsOverHyperlink)
         {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || !CtrlKeyOpensHyperlink)
               HyperlinkClickable = true;
            else
               HyperlinkClickable = false;
         }
         else
            HyperlinkClickable = false;
      }
   }

   private readonly Cursor HyperlinkCursor = new(StandardCursorType.Hand);
   private bool HyperlinkClickable { get; set { field = value; this.Cursor = value ? HyperlinkCursor : Cursor.Default; } }

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


