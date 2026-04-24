using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;

namespace AvRichTextBox;

public partial class RichTextBox : UserControl
{

   internal void SelectionStart_RectChanged(EditableParagraph edPar)
   {
      edPar.UpdateLayout();

      if (edPar.DataContext is not Paragraph thisPar) return;

      TextLayout tlayout = edPar.TextLayout;

      Rect selStartRect = tlayout.HitTestTextPosition(edPar.SelectionStart);
      Rect prevCharRect = tlayout.HitTestTextPosition(edPar.SelectionStart - 1);

      if (edPar.TranslatePoint(selStartRect.Position, DocIC) is not Point selStartPoint) return;
      if (edPar.TranslatePoint(prevCharRect.Position, DocIC) is not Point prevCharPoint) return;

      FlowDoc.Selection.StartRect = new Rect(selStartPoint, selStartRect.Size);
      FlowDoc.Selection.PrevCharRect = new Rect(prevCharPoint, prevCharRect.Size);

      IReadOnlyList<TextLine> textLines = tlayout.TextLines;

      //Calculate caret height and position
      int lineIndex = tlayout.GetLineIndexFromCharacterIndex(edPar.SelectionStart, false);
      TextLine thisTextLine = textLines[lineIndex];

      ////check for subscript
      //if (FlowDoc.Selection.GetStartInline() is EditableRun erun)
      //   if (erun.BaselineAlignment == BaselineAlignment.Subscript)

      double glyphHeight = thisTextLine.Height;
      int IdxInThisLine = FlowDoc.Selection.Start - thisPar.StartInDoc;   // GetTextBounds counts charindex from start of paragraph
      bool offsetTopFromHeight = false;
      BaselineAlignment balign = BaselineAlignment.Baseline;

      bool endOfPar = thisPar.EndInDoc - FlowDoc.Selection.Start == 1 && !(FlowDoc.Selection.Start == thisPar.StartInDoc);

      if (endOfPar)
         IdxInThisLine -= 1;

      if (thisTextLine.GetTextBounds(IdxInThisLine, 1).FirstOrDefault() is TextBounds tbounds && tbounds.TextRunBounds.Count > 0 && tbounds.TextRunBounds[0].TextRun is ShapedTextRun strun)
      {
         glyphHeight = strun.GlyphRun.Bounds.Height - 2;
         //Trace.WriteLine("strun.Properties.BaselineAlignment = " + strun.Properties.BaselineAlignment);
         offsetTopFromHeight = (strun.Properties.BaselineAlignment == BaselineAlignment.TextBottom);
         balign = strun.Properties.BaselineAlignment;
      }

      RtbVm.CalculateCaretHeightAndPosition(thisTextLine, selStartPoint.X, glyphHeight, offsetTopFromHeight, balign);
      RtbVm.UpdateCaretVisible();


      //************ calculate start line-relative properties *****************
      thisPar.DistanceSelectionStartFromLeft = selStartRect.Left;
      thisPar.IsStartAtFirstLine = lineIndex == 0;
      thisPar.IsStartAtLastLine = (lineIndex == textLines.Count - 1);

      // get index of first char on previous line
      if (thisPar.IsStartAtFirstLine)
         thisPar.CharPrevLineStart = edPar.SelectionStart;
      else
         thisPar.CharPrevLineStart = GetClosestIndex(edPar, lineIndex, thisPar.DistanceSelectionStartFromLeft, -1);

      // get index of first char on next line
      if (thisPar.IsStartAtLastLine)
         thisPar.CharNextLineStart = edPar.SelectionEnd - thisTextLine.FirstTextSourceIndex;
      else
         thisPar.CharNextLineStart = GetClosestIndex(edPar, lineIndex, thisPar.DistanceSelectionStartFromLeft, 1);

      thisPar.FirstIndexStartLine = FlowDoc.Selection.IsAtEndOfLineSpace ?
         textLines[Math.Max(0, lineIndex - 1)].FirstTextSourceIndex :
         thisTextLine.FirstTextSourceIndex;
      thisPar.FirstIndexLastLine = textLines[^1].FirstTextSourceIndex;
      //****************************************************************


   }


   //internal void SelectionStart_RectChanged(EditableParagraph edPar)
   //{

   //   edPar.UpdateLayout();

   //   if (edPar.DataContext is not Paragraph thisPar) return;
   //   TextLayout tlayout = edPar.TextLayout;

   //   Rect selStartRect = tlayout.HitTestTextPosition(edPar.SelectionStart);
   //   Rect prevCharRect = tlayout.HitTestTextPosition(edPar.SelectionStart - 1);

   //   //Trace.WriteLine("selstartrectX = " + selStartRect.X);

   //   if (edPar.TranslatePoint(selStartRect.Position, DocIC) is not Point selStartPoint) return;
   //   if (edPar.TranslatePoint(prevCharRect.Position, DocIC) is not Point prevCharPoint) return;

   //   FlowDoc.Selection.StartRect = new Rect(selStartPoint, selStartRect.Size);
   //   FlowDoc.Selection.PrevCharRect = new Rect(prevCharPoint, prevCharRect.Size);

   //   IReadOnlyList<TextLine> textLines = tlayout.TextLines;

   //   //Calculate caret height and position
   //   int lineIndex = tlayout.GetLineIndexFromCharacterIndex(edPar.SelectionStart, false);
   //   TextLine thisTextLine = textLines[lineIndex];

   //   ////test for subscript
   //   //if (FlowDoc.Selection.GetStartInline() is EditableRun erun)
   //   //   if (erun.BaselineAlignment == BaselineAlignment.Subscript)
   //   //      offsetMe = true;

   //   //double glyphHeight = thisTextLine.Height;
   //   //int IdxInThisLine = FlowDoc.Selection.Start - (thisPar.StartInDoc - thisTextLine.FirstTextSourceIndex);
   //   //bool offsetTopFromHeight = false;
   //   //BaselineAlignment balign = BaselineAlignment.Baseline;

   //   //if (thisTextLine.GetTextBounds(IdxInThisLine, 1)[0].TextRunBounds[0].TextRun is ShapedTextRun strun)
   //   //{
   //   //   //Trace.WriteLine(strun.Text + ", baselineAign = " + strun.Properties.BaselineAlignment);
   //   //   glyphHeight = strun.GlyphRun.Bounds.Height - 2;
   //   //   offsetTopFromHeight = (strun.Properties.BaselineAlignment != BaselineAlignment.Superscript);
   //   //   balign = strun.Properties.BaselineAlignment;
   //   //}

   //   //RtbVm.CalculateCaretHeightAndPosition(thisTextLine, selStartPoint.X, glyphHeight, offsetTopFromHeight, balign);
   //   RtbVm.CalculateCaretHeightAndPosition(thisTextLine, selStartPoint.X);
   //   RtbVm.UpdateCaretVisible();

   //   //************ calculate start line-relative properties *****************
   //   thisPar.DistanceSelectionStartFromLeft = selStartRect.Left;
   //   thisPar.IsStartAtFirstLine = lineIndex == 0;
   //   thisPar.IsStartAtLastLine = (lineIndex == textLines.Count - 1);

   //   // get index of first char on previous line
   //   if (thisPar.IsStartAtFirstLine)
   //      thisPar.CharPrevLineStart = edPar.SelectionStart;
   //   else
   //      thisPar.CharPrevLineStart = GetClosestIndex(edPar, lineIndex, thisPar.DistanceSelectionStartFromLeft, -1);

   //   // get index of first char on next line
   //   if (thisPar.IsStartAtLastLine)
   //      thisPar.CharNextLineStart = edPar.SelectionEnd - thisTextLine.FirstTextSourceIndex;
   //   else
   //      thisPar.CharNextLineStart = GetClosestIndex(edPar, lineIndex, thisPar.DistanceSelectionStartFromLeft, 1);

   //   thisPar.FirstIndexStartLine = FlowDoc.Selection.IsAtEndOfLineSpace ?
   //      textLines[Math.Max(0, lineIndex - 1)].FirstTextSourceIndex :
   //      thisTextLine.FirstTextSourceIndex;
   //   thisPar.FirstIndexLastLine = textLines[^1].FirstTextSourceIndex;
   //   //****************************************************************


   //}

   internal void SelectionEnd_RectChanged(EditableParagraph edPar)
   {
      edPar.UpdateLayout();

      TextLayout tlayout = edPar.TextLayout;

      Rect selEndRect = tlayout.HitTestTextPosition(edPar.SelectionEnd);

      Point? selEndPoint = edPar.TranslatePoint(selEndRect.Position, DocIC);
      if (selEndPoint != null)
         FlowDoc.Selection.EndRect = new Rect((Point)selEndPoint!, selEndRect.Size);

      RtbVm.UpdateCaretVisible();

      //************ calculate end line-relative properties *****************
      if (edPar.DataContext is not Paragraph thisPar) return;

      thisPar.DistanceSelectionEndFromLeft = tlayout.HitTestTextPosition(edPar.SelectionEnd).Left;
      int lineNo = tlayout.GetLineIndexFromCharacterIndex(edPar.SelectionEnd, false);

      IReadOnlyList<TextLine> textLines = tlayout.TextLines;
            
      thisPar.IsEndAtLastLine = lineNo == textLines.Count - 1;
      thisPar.IsEndAtFirstLine = (lineNo == 0);

      if (thisPar.IsEndAtLastLine)
      {
         thisPar.LastIndexEndLine = thisPar.BlockLength;
         thisPar.CharNextLineEnd = edPar.TextLength + 1 + edPar.SelectionEnd - textLines[lineNo].FirstTextSourceIndex;
      }
      else
      {
         TextLine tline = textLines[lineNo];
         int goBackNo = 1;
         if (tline.TextRuns.Count > 0)
         {
            if (tline.TextRuns[tline.TextRuns.Count - 1].Text.ToString() == Environment.NewLine)
               goBackNo++;
         }
         thisPar.LastIndexEndLine = textLines[lineNo + 1].FirstTextSourceIndex - goBackNo;
         thisPar.CharNextLineEnd = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionEndFromLeft, 1);
      }

      if (!thisPar.IsEndAtFirstLine)
         thisPar.CharPrevLineEnd = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionEndFromLeft, -1);

      thisPar.FirstIndexLastLine = textLines[^1].FirstTextSourceIndex;
      //****************************************************************
            

#if DEBUG
      // Scroll Debugger panel to selection end inline
      debuggerPanel?.ParagraphsLB.ScrollIntoView(thisPar);
      if (debuggerPanel?.ParagraphsLB.ContainerFromItem(thisPar) is ListBoxItem lbi)
         if (lbi.GetVisualChildren().OfType<ItemsControl>().FirstOrDefault(c => c.Name == "InlinesIC") is ItemsControl inlinesIC)
            if (FlowDoc.Selection.GetEndInline() is IEditable ied)
               inlinesIC.ScrollIntoView(ied);
#endif


   }


   private static int GetClosestIndex(EditableParagraph edPar, int lineNo, double distanceFromLeft, int direction)
   {
      CharacterHit chit = edPar.TextLayout.TextLines[lineNo + direction].GetCharacterHitFromDistance(distanceFromLeft);

      double CharDistanceDiffThis = Math.Abs(distanceFromLeft - edPar.TextLayout.HitTestTextPosition(chit.FirstCharacterIndex).Left);
      double CharDistanceDiffNext = Math.Abs(distanceFromLeft - edPar.TextLayout.HitTestTextPosition(chit.FirstCharacterIndex + 1).Left);

      if (CharDistanceDiffThis > CharDistanceDiffNext)
         return chit.FirstCharacterIndex + 1;
      else
         return chit.FirstCharacterIndex;


   }


}


