//using Avalonia.Controls;
//using Avalonia.Media;
//using Avalonia.Media.TextFormatting;
//using Avalonia.VisualTree;

//namespace AvRichTextBox;

//public partial class RichTextBox : UserControl
//{

//   private void FlowDoc_SelectionStartChanged(TextRange selection)
//   {
//      CalculateParagraphStartLayoutProperties(selection.StartParagraph, selection.StartParagraph.TextLayout);
//      UpdateSelectionIndicators();
//   }

//   private void FlowDoc_SelectionEndChanged(TextRange selection)
//   {
//      CalculateParagraphEndLayoutProperties(selection.EndParagraph, selection.EndParagraph.TextLayout);
//      UpdateSelectionIndicators();
//   }

//   private void UpdateSelectionIndicators()
//   {

//      _geometry.Figures = [];

//      //caret & highlighting visibility
//      SelectionPath.IsVisible = FlowDoc.Selection.Length > 0;
//      RtbVm.CaretVisible = !SelectionPath.IsVisible;

//      for (int parno = 0; parno < FlowDoc.SelectionParagraphs.Count; parno++)
//      {
//         Paragraph p = FlowDoc.SelectionParagraphs[parno];
//         if (p.TextLayout == null) continue;

//         //CalculateParagraphLayoutProperties(p, p.TextLayout, parno == 0, parno == FlowDoc.SelectionParagraphs.Count - 1);

//         bool parIsEmpty = p.Inlines.Count == 1 && p.Inlines.First().IsEmpty;

//         if (parIsEmpty)
//         {
//            TextLine tline = p.TextLayout.TextLines[0];
//            double tlineTop = p.DocICRelativeTop;

//            try
//            {  //sometimes throws index outside of bounds before update
//               double tlineLeft = p.TextLayout.HitTestTextPosition(p.SelectionStartInBlock).Left;
//               Rect lineRect = new(p.FlowDocRelativeLeft + tlineLeft, tlineTop, 22, tline.Height);
//               _geometry.Figures?.Add(GetLineRectanglePath(lineRect));
//            }
//            catch { }
//         }

//         if (RtbVm.CaretVisible)
//         {  //Debug.WriteLine("start/end = " + FlowDoc.Selection.Start + ", " + FlowDoc.Selection.End);

//            int lineIndex = p.TextLayout.GetLineIndexFromCharacterIndex(p.SelectionStartInBlock, false);
//            TextLine thisTextLine = p.TextLayout.TextLines[lineIndex];

//            Rect selStartRect = p.TextLayout.HitTestTextPosition(p.SelectionStartInBlock);

//            double glyphHeight = thisTextLine.Height;
//            int IdxInThisLine = FlowDoc.Selection.Start - p.StartInDoc;   // GetTextBounds counts charindex from start of paragraph
//            BaselineAlignment balign = BaselineAlignment.Baseline;

//            bool endOfPar = p.EndInDoc - FlowDoc.Selection.Start == 1 && !(FlowDoc.Selection.Start == p.StartInDoc);

//            if (endOfPar)
//               IdxInThisLine -= 1;

//            if (thisTextLine.GetTextBounds(IdxInThisLine, 1).FirstOrDefault() is TextBounds tbounds && tbounds.TextRunBounds.Count > 0 && tbounds.TextRunBounds[0].TextRun is ShapedTextRun strun)
//            {
//               glyphHeight = strun.GlyphRun.Bounds.Height - 2;
//               balign = strun.Properties.BaselineAlignment;
//            }

//            RtbVm.CalculateCaretHeightAndPosition(thisTextLine, p.FlowDocRelativeLeft + selStartRect.X, glyphHeight, balign);
//            continue;

//         }

//         // multiple selection lines highlighting
//         double lineTop = 0;
//         for (int tlineno = 0; tlineno < p.TextLayout.TextLines.Count; tlineno++)
//         {
//            TextLine tline = p.TextLayout.TextLines[tlineno];

//            int lineStartIndex = tline.FirstTextSourceIndex;
//            int lineEndIndex = lineStartIndex + tline.Length;
//            bool isSelectedLine = lineEndIndex >= p.SelectionStartInBlock && lineStartIndex < p.SelectionEndInBlock;
//            bool isFirstSelLine = lineEndIndex >= p.SelectionStartInBlock && lineStartIndex <= p.SelectionStartInBlock;
//            bool isLastSelLine = lineEndIndex > p.SelectionEndInBlock && lineStartIndex < p.SelectionEndInBlock;

//            if (isSelectedLine)
//            {
//               int idxInLine = p.SelectionStartInBlock - lineStartIndex;

//               Rect leftRect = new(0, 0, 0, 0);
//               try { leftRect = p.TextLayout.HitTestTextPosition(0); }
//               //try { leftRect = p.TextLayout.HitTestTextPosition(lineStartIndex); }
//               catch { Debug.WriteLine("hittesttextpos failed"); continue; }

//               double tlineLeft = leftRect.Left;
//               double tlineWidth = tline.Width;

//               if (isFirstSelLine)
//               {
//                  tlineLeft = p.TextLayout.HitTestTextPosition(p.SelectionStartInBlock).Left;
//                  double widthCut = tlineLeft - leftRect.Left;
//                  tlineWidth -= widthCut;
//                  //Debug.WriteLine("isfirstselline = " + tline.TextRuns[0].Text);
//               }

//               if (isLastSelLine)
//               {
//                  tlineWidth = p.TextLayout.HitTestTextPosition(p.SelectionEndInBlock).Left - tlineLeft;
//                  //Debug.WriteLine("islastselline = " + tline.TextRuns[0].Text);
//               }

//               double tlineAdjustedLeft = p.FlowDocRelativeLeft + tlineLeft;
//               double tlineTop = p.DocICRelativeTop + lineTop;

//               Rect lineRect = new
//               (
//                  tlineAdjustedLeft,
//                  tlineTop,
//                  tlineWidth,
//                  tline.Height
//               );

//               _geometry.Figures?.Add(GetLineRectanglePath(lineRect));
//            }

//            lineTop += tline.Height;

//         }
//      }

//   }

//   private void CalculateParagraphStartLayoutProperties(Paragraph thisPar, TextLayout tlayout)
//   {

//      thisPar.CallRequestTextLayoutInfoStart();
//      thisPar.SelectionStartInBlock = Math.Max(0, FlowDoc.Selection.Start - thisPar.StartInDoc);

//      IReadOnlyList<TextLine> textLines = tlayout.TextLines;

//      //************ calculate start line-relative properties *****************
//      try
//      {
//         int startLineIndex = tlayout.GetLineIndexFromCharacterIndex(thisPar.SelectionStartInBlock, false);
//         TextLine thisTextLine = textLines[startLineIndex];

//         Rect selStartRect = tlayout.HitTestTextPosition(thisPar.SelectionStartInBlock);
//         Point selStartPoint = selStartRect.Position + new Vector(thisPar.FlowDocRelativeLeft, thisPar.FlowDocRelativeTop);
//         FlowDoc.Selection.StartRect = new Rect(selStartPoint, selStartRect.Size);

//         //Rect prevCharRect = tlayout.HitTestTextPosition(Math.Max(0, thisPar.SelectionStartInBlock - 1));
//         Rect prevCharRect = tlayout.HitTestTextPosition(thisPar.SelectionStartInBlock - 1);
//         Point prevCharPoint = prevCharRect.Position + new Vector(thisPar.FlowDocRelativeLeft, thisPar.FlowDocRelativeTop);
//         FlowDoc.Selection.PrevCharRect = new Rect(prevCharPoint, prevCharRect.Size);

//         thisPar.DistanceSelectionStartFromLeft = selStartPoint.X;
//         thisPar.IsStartAtFirstLine = startLineIndex == 0;
//         thisPar.IsStartAtLastLine = (startLineIndex == textLines.Count - 1);

//         // get index of first char on previous line
//         if (thisPar.IsStartAtFirstLine)
//            thisPar.CharPrevLineStart = thisPar.SelectionStartInBlock;
//         else
//            thisPar.CharPrevLineStart = GetClosestIndex(tlayout, startLineIndex, thisPar.DistanceSelectionStartFromLeft, -1);

//         // get index of first char on next line
//         if (thisPar.IsStartAtLastLine)
//            thisPar.CharNextLineStart = thisPar.SelectionEndInBlock - thisTextLine.FirstTextSourceIndex;
//         else
//            thisPar.CharNextLineStart = GetClosestIndex(tlayout, startLineIndex, thisPar.DistanceSelectionStartFromLeft, 1);

//         thisPar.FirstIndexStartLine = FlowDoc.Selection.IsAtEndOfLineSpace ?
//            textLines[Math.Max(0, startLineIndex - 1)].FirstTextSourceIndex :
//            thisTextLine.FirstTextSourceIndex;
//         thisPar.FirstIndexLastLine = textLines[^1].FirstTextSourceIndex;

//      }
//      catch (Exception ex) { Debug.WriteLine("Error calculating: " + ex.Message); }


//   }

//   private void CalculateParagraphEndLayoutProperties(Paragraph thisPar, TextLayout tlayout)
//   {

//      thisPar.CallRequestTextLayoutInfoEnd();
//      thisPar.SelectionEndInBlock = Math.Max(0, FlowDoc.Selection.End - thisPar.StartInDoc);

//      IReadOnlyList<TextLine> textLines = tlayout.TextLines;

//      //************ calculate end line-relative properties *****************
//      try
//      {
//         Rect selEndRect = tlayout.HitTestTextPosition(thisPar.SelectionEndInBlock);
//         Point selEndPoint = selEndRect.Position + new Vector(thisPar.FlowDocRelativeLeft, thisPar.FlowDocRelativeTop);
//         FlowDoc.Selection.EndRect = new Rect(selEndPoint, selEndRect.Size);

//         thisPar.DistanceSelectionEndFromLeft = selEndRect.Left;
//         int endLineIndex = tlayout.GetLineIndexFromCharacterIndex(thisPar.SelectionEndInBlock, false);

//         thisPar.IsEndAtLastLine = endLineIndex == textLines.Count - 1;
//         thisPar.IsEndAtFirstLine = (endLineIndex == 0);

//         if (thisPar.IsEndAtLastLine)
//         {
//            thisPar.LastIndexEndLine = thisPar.BlockLength;
//            thisPar.CharNextLineEnd = thisPar.TextLength + 1 + thisPar.SelectionEndInBlock - textLines[endLineIndex].FirstTextSourceIndex; //$$$$$$$$$$$$$$$$$$$
//         }
//         else
//         {
//            TextLine tline = textLines[endLineIndex];
//            int goBackNo = 1;
//            if (tline.TextRuns.Count > 0)
//            {
//               if (tline.TextRuns[tline.TextRuns.Count - 1].Text.ToString() == Environment.NewLine)
//                  goBackNo++;
//            }
//            thisPar.LastIndexEndLine = textLines[endLineIndex + 1].FirstTextSourceIndex - goBackNo;
//            thisPar.CharNextLineEnd = GetClosestIndex(tlayout, endLineIndex, thisPar.DistanceSelectionEndFromLeft, 1);
//         }

//         if (!thisPar.IsEndAtFirstLine)
//            thisPar.CharPrevLineEnd = GetClosestIndex(tlayout, endLineIndex, thisPar.DistanceSelectionEndFromLeft, -1);

//         thisPar.FirstIndexLastLine = textLines[^1].FirstTextSourceIndex;
//      }
//      catch (Exception ex) { Debug.WriteLine("Error calculating: " + ex.Message); }


//#if DEBUG
//      // Scroll Debugger panel to selection end inline
//      debuggerPanel?.ParagraphsLB.ScrollIntoView(thisPar);
//      if (debuggerPanel?.ParagraphsLB.ContainerFromItem(thisPar) is ListBoxItem lbi)
//         if (lbi.GetVisualChildren().OfType<ItemsControl>().FirstOrDefault(c => c.Name == "InlinesIC") is ItemsControl inlinesIC)
//            if (FlowDoc.Selection.GetEndInline() is IEditable ied)
//               inlinesIC.ScrollIntoView(ied);
//#endif

//   }

//   private static int GetClosestIndex(TextLayout tLayout, int lineNo, double distanceFromLeft, int direction)
//   {
//      CharacterHit chit = tLayout.TextLines[lineNo + direction].GetCharacterHitFromDistance(distanceFromLeft);

//      double CharDistanceDiffThis = Math.Abs(distanceFromLeft - tLayout.HitTestTextPosition(chit.FirstCharacterIndex).Left);
//      double CharDistanceDiffNext = Math.Abs(distanceFromLeft - tLayout.HitTestTextPosition(chit.FirstCharacterIndex + 1).Left);

//      if (CharDistanceDiffThis > CharDistanceDiffNext)
//         return chit.FirstCharacterIndex + 1;
//      else
//         return chit.FirstCharacterIndex;

//   }

   
//}


