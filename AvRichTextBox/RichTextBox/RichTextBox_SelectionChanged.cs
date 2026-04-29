using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;

namespace AvRichTextBox;

public partial class RichTextBox : UserControl
{
   readonly double EmptyLineWidth = 14;

   private void FlowDoc_Selection_Changed(TextRange selection) { UpdateSelectionIndicators(); }
   private void FlowDoc_PagePadding_Changed() { Dispatcher.UIThread.Post(() => { UpdateSelectionIndicators(); }); }

   private void UpdateSelectionIndicators()
   {
      this.UpdateLayout();
       
      //caret & highlighting visibility
      _geometry.Figures = [];
      bool caretOnly = FlowDoc.Selection.Length == 0;
      SelectionPath.IsVisible = !caretOnly;
      RtbVm.CaretVisible = caretOnly;

      //Get rect measurement for each line in each selected paragraph 
      for (int parno = 0; parno < FlowDoc.SelectionParagraphs.Count; parno++)
      {
         Paragraph p = FlowDoc.SelectionParagraphs[parno];
         
         if (p.TextLayout == null) continue;
         bool parIsEmpty = p.Inlines.Count == 1 && p.Inlines.First().IsEmpty;

         p.CallRequestTextLayoutInfoStart();
         p.CallRequestTextLayoutInfoEnd();
         p.SelectionStartInBlock = Math.Max(0, FlowDoc.Selection.Start - p.StartInDoc);
         p.SelectionEndInBlock = Math.Min(p.BlockLength, FlowDoc.Selection.End - p.StartInDoc);

         CalculateParagraphLayoutProperties(p, p.TextLayout, parno == 0, parno == FlowDoc.SelectionParagraphs.Count - 1, parIsEmpty);

         //for full cell selection visibility
         if (p.IsTableCellBlock)
            p.OwningCell.Selected = (p.SelectionStartInBlock == 0 && p.SelectionEndInBlock == p.BlockLength);

         if (caretOnly)
         {  //only get first selected paragraph 

            int lineIndex = p.TextLayout.GetLineIndexFromCharacterIndex(p.SelectionStartInBlock, false);
            TextLine thisTextLine = p.TextLayout.TextLines[lineIndex];

            Rect selStartRect = p.TextLayout.HitTestTextPosition(p.SelectionStartInBlock);

            double glyphHeight = thisTextLine.Height;
            int IdxInThisLine = FlowDoc.Selection.Start - p.StartInDoc;   // GetTextBounds counts charindex from start of paragraph
            BaselineAlignment balign = BaselineAlignment.Baseline;

            bool endOfPar = p.EndInDoc - FlowDoc.Selection.Start == 1 && !(FlowDoc.Selection.Start == p.StartInDoc);

            if (endOfPar)
               IdxInThisLine -= 1;

            if (thisTextLine.GetTextBounds(IdxInThisLine, 1).FirstOrDefault() is TextBounds tbounds && tbounds.TextRunBounds.Count > 0 && tbounds.TextRunBounds[0].TextRun is ShapedTextRun strun)
            {
               glyphHeight = strun.GlyphRun.Bounds.Height - 2;
               balign = strun.Properties.BaselineAlignment;
            }

            RtbVm.CalculateCaretHeightAndPosition(thisTextLine, p.DocICRelativeLeft + selStartRect.X, glyphHeight, balign);
         }
         else
         {
            if (parIsEmpty)
            {
               TextLine tline = p.TextLayout.TextLines[0];
               double tlineTop = p.DocICRelativeTop;

               try
               {  //sometimes throws index outside of bounds before update
                  double tlineLeft = p.TextLayout.HitTestTextPosition(p.SelectionStartInBlock).Left;
                  Rect lineRect = new(p.DocICRelativeLeft + tlineLeft, tlineTop, EmptyLineWidth, tline.Height);
                  _geometry.Figures?.Add(GetLineRectanglePath(lineRect));
               }
               catch { Debug.WriteLine($"HitTestTextPosition at paragraph Start: {p.SelectionStartInBlock} failed"); }
               continue;
            }

            // multiple selection lines highlighting
            double lineTop = 0;
            for (int tlineno = 0; tlineno < p.TextLayout.TextLines.Count; tlineno++)
            {
               TextLine tline = p.TextLayout.TextLines[tlineno];

               int lineStartIndex = tline.FirstTextSourceIndex;
               int lineEndIndex = lineStartIndex + tline.Length;

               bool isSelectedLine = lineEndIndex >= p.SelectionStartInBlock && lineStartIndex < p.SelectionEndInBlock;
               bool isFirstSelLine = isSelectedLine && lineStartIndex <= p.SelectionStartInBlock;
               bool isLastSelLine =  isSelectedLine &&  lineEndIndex > p.SelectionEndInBlock;

               if (isSelectedLine)
               {
                  int idxInLine = p.SelectionStartInBlock - lineStartIndex;

                  Rect leftRect = new(0, 0, 0, 0);
                  //try { leftRect = p.TextLayout.HitTestTextPosition(0); }
                  try { leftRect = p.TextLayout.HitTestTextPosition(lineStartIndex); }
                  catch { Debug.WriteLine($"HitTestTextPosition at lineStartIndex: {lineStartIndex} failed"); continue; }

                  double tlineLeft = leftRect.Left;
                  double tlineWidth = tline.Width;

                  if (isFirstSelLine)
                  {
                     tlineLeft = p.TextLayout.HitTestTextPosition(p.SelectionStartInBlock).Left;
                     double widthCut = tlineLeft - leftRect.Left;
                     tlineWidth -= widthCut;
                  }

                  if (isLastSelLine)
                     tlineWidth = p.TextLayout.HitTestTextPosition(p.SelectionEndInBlock).Left - tlineLeft;

                  double tlineAdjustedLeft = p.DocICRelativeLeft + tlineLeft;
                  double tlineTop = p.DocICRelativeTop + lineTop;
                  double tlineHeight = tline.Height;

                  //Give visual width to rectangle for empty lines
                  if (tline.TextRuns.Count == 1 && tline.FirstTextSourceIndex < p.Text.Length && p.Text[tline.FirstTextSourceIndex] == '\\')
                     tlineWidth = EmptyLineWidth;

                  Rect lineRect = new
                  (
                     tlineAdjustedLeft,
                     tlineTop,
                     tlineWidth,
                     tlineHeight
                  );

                  _geometry.Figures?.Add(GetLineRectanglePath(lineRect));
               }

               lineTop += tline.Height;

            }
         }
      }

   }

   private void CalculateParagraphLayoutProperties(Paragraph thisPar, TextLayout tlayout, bool isStartPar, bool isEndPar, bool parIsEmpty)
   {
      IReadOnlyList<TextLine> textLines = tlayout.TextLines;
      
      //************ calculate start line-relative properties *****************
      if (isStartPar)
      {
         try
         {
            int startLineIndex = tlayout.GetLineIndexFromCharacterIndex(thisPar.SelectionStartInBlock, false);
            TextLine thisTextLine = textLines[startLineIndex];

            Rect selStartRect = new();
            try
            {
               selStartRect = tlayout.HitTestTextPosition(thisPar.SelectionStartInBlock);
            }
            catch { Debug.WriteLine("error getting selStartRect (selStartInBlock: " + thisPar.SelectionStartInBlock + ", startlineidx = " + startLineIndex); }
            
            Point selStartPoint = selStartRect.Position + new Vector(thisPar.DocICRelativeLeft, thisPar.DocICRelativeTop);
            FlowDoc.Selection.StartRect = new Rect(selStartPoint, selStartRect.Size);

            try
            {
               if (thisPar.SelectionStartInBlock > 0)
               {
                  Rect prevCharRect = tlayout.HitTestTextPosition(thisPar.SelectionStartInBlock - 1);
                  Point prevCharPoint = prevCharRect.Position + new Vector(thisPar.DocICRelativeLeft, thisPar.DocICRelativeTop);
                  FlowDoc.Selection.PrevCharRect = new Rect(prevCharPoint, prevCharRect.Size);
               }
            }
            catch (Exception ex) { Debug.WriteLine("Error getting prevChar properties : " + ex.Message); }
            
            thisPar.DistanceSelectionStartFromLeft = selStartPoint.X;
            thisPar.IsStartAtFirstLine = startLineIndex == 0;
            thisPar.IsStartAtLastLine = (startLineIndex == textLines.Count - 1);

            // get index of first char on previous line
            if (thisPar.IsStartAtFirstLine)
               thisPar.CharPrevLineStart = thisPar.SelectionStartInBlock;
            else
               thisPar.CharPrevLineStart = GetClosestIndex(tlayout, startLineIndex, thisPar.DistanceSelectionStartFromLeft, -1);

            // get index of first char on next line
            if (thisPar.IsStartAtLastLine)
               thisPar.CharNextLineStart = thisPar.SelectionEndInBlock - thisTextLine.FirstTextSourceIndex;
            else
               thisPar.CharNextLineStart = GetClosestIndex(tlayout, startLineIndex, thisPar.DistanceSelectionStartFromLeft, 1);

            thisPar.FirstIndexStartLine = FlowDoc.Selection.IsAtEndOfLineSpace ?
               textLines[Math.Max(0, startLineIndex - 1)].FirstTextSourceIndex :
               thisTextLine.FirstTextSourceIndex;
            thisPar.FirstIndexLastLine = textLines[^1].FirstTextSourceIndex;

         }
         catch (Exception ex) { Debug.WriteLine("Error calculating start properties: " + ex.Message); }
      }


      //************ calculate end line-relative properties *****************
      if (isEndPar)
      {
         try
         {
            Rect selEndRect = tlayout.HitTestTextPosition(thisPar.SelectionEndInBlock);
            Point selEndPoint = selEndRect.Position + new Vector(thisPar.DocICRelativeLeft, thisPar.DocICRelativeTop);
            FlowDoc.Selection.EndRect = new Rect(selEndPoint, selEndRect.Size);

            thisPar.DistanceSelectionEndFromLeft = selEndRect.Left;
            int endLineIndex = tlayout.GetLineIndexFromCharacterIndex(thisPar.SelectionEndInBlock, false);

            thisPar.IsEndAtLastLine = endLineIndex == textLines.Count - 1;
            thisPar.IsEndAtFirstLine = (endLineIndex == 0);

            if (thisPar.IsEndAtLastLine)
            {
               thisPar.LastIndexEndLine = thisPar.BlockLength;
               thisPar.CharNextLineEnd = thisPar.TextLength + 1 + thisPar.SelectionEndInBlock - textLines[endLineIndex].FirstTextSourceIndex; //$$$$$$$$$$$$$$$$$$$
            }
            else
            {
               TextLine tline = textLines[endLineIndex];
               int goBackNo = 1;
               if (tline.TextRuns.Count > 0)
               {
                  if (tline.TextRuns[tline.TextRuns.Count - 1].Text.ToString() == Environment.NewLine)
                     goBackNo++;
               }
               thisPar.LastIndexEndLine = textLines[endLineIndex + 1].FirstTextSourceIndex - goBackNo;
               thisPar.CharNextLineEnd = GetClosestIndex(tlayout, endLineIndex, thisPar.DistanceSelectionEndFromLeft, 1);
            }

            if (!thisPar.IsEndAtFirstLine)
               thisPar.CharPrevLineEnd = GetClosestIndex(tlayout, endLineIndex, thisPar.DistanceSelectionEndFromLeft, -1);

            thisPar.FirstIndexLastLine = textLines[^1].FirstTextSourceIndex;
         }
         catch (Exception ex) { Debug.WriteLine("Error calculating end properties: " + ex.Message); }

      }


#if DEBUG
      // Scroll Debugger panel to selection end inline
      debuggerPanel?.ParagraphsLB.ScrollIntoView(thisPar);
      if (debuggerPanel?.ParagraphsLB.ContainerFromItem(thisPar) is ListBoxItem lbi)
         if (lbi.GetVisualChildren().OfType<ItemsControl>().FirstOrDefault(c => c.Name == "InlinesIC") is ItemsControl inlinesIC)
            if (FlowDoc.Selection.GetEndInline() is IEditable ied)
               inlinesIC.ScrollIntoView(ied);
#endif

   }

   private static int GetClosestIndex(TextLayout tLayout, int lineNo, double distanceFromLeft, int direction)
   {
      CharacterHit chit = tLayout.TextLines[lineNo + direction].GetCharacterHitFromDistance(distanceFromLeft);

      double CharDistanceDiffThis = Math.Abs(distanceFromLeft - tLayout.HitTestTextPosition(chit.FirstCharacterIndex).Left);
      double CharDistanceDiffNext = Math.Abs(distanceFromLeft - tLayout.HitTestTextPosition(chit.FirstCharacterIndex + 1).Left);

      if (CharDistanceDiffThis > CharDistanceDiffNext)
         return chit.FirstCharacterIndex + 1;
      else
         return chit.FirstCharacterIndex;

   }

   
}


