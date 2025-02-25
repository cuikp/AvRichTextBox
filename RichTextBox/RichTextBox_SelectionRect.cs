using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Diagnostics;

namespace AvRichTextBox;

public partial class RichTextBox : UserControl
{
   internal void SelectionStart_RectChanged(EditableParagraph edPar)
   {
      edPar.UpdateLayout();

      Rect selStartRect = edPar.TextLayout.HitTestTextPosition(edPar.SelectionStart);
      Rect prevCharRect = edPar.TextLayout.HitTestTextPosition(edPar.SelectionStart - 1);

      Point? selStartPoint = edPar.TranslatePoint(selStartRect.Position, DocIC);
      Point? prevCharPoint = edPar.TranslatePoint(prevCharRect.Position, DocIC);

      if (selStartPoint != null)
         FlowDoc.Selection.StartRect = new Rect((Point)selStartPoint!, selStartRect.Size);
      
      if (prevCharPoint != null)
         FlowDoc.Selection.PrevCharRect = new Rect((Point)prevCharPoint!, prevCharRect.Size);

      Paragraph thisPar = (Paragraph)edPar.DataContext!;


      thisPar.DistanceSelectionStartFromLeft = edPar.TextLayout.HitTestTextPosition(edPar.SelectionStart).Left;

      int lineNo = edPar.TextLayout.GetLineIndexFromCharacterIndex(edPar.SelectionStart, false);
      thisPar.IsStartAtFirstLine = lineNo == 0;

      thisPar.IsStartAtLastLine = (lineNo == edPar.TextLayout.TextLines.Count - 1);
      
      if (thisPar.IsStartAtFirstLine)
         thisPar.CharPrevLineStart = edPar.SelectionStart;
      else
         thisPar.CharPrevLineStart = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionStartFromLeft, -1);

      if (thisPar.IsStartAtLastLine)
         thisPar.CharNextLineStart = edPar.SelectionEnd - edPar.TextLayout.TextLines[lineNo].FirstTextSourceIndex;
      else
         thisPar.CharNextLineStart = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionStartFromLeft, 1);

      thisPar.FirstIndexStartLine = edPar.TextLayout.TextLines[lineNo].FirstTextSourceIndex;
      thisPar.FirstIndexLastLine = edPar.TextLayout.TextLines[^1].FirstTextSourceIndex;

      //Debug.WriteLine("will update cursor: START rect changed, end-start = " + (FlowDoc.Selection.End - FlowDoc.Selection.Start));

      rtbVM.UpdateCursor();

   }

   internal void SelectionEnd_RectChanged(EditableParagraph edPar)
   {
      edPar.UpdateLayout();

      Rect selEndRect = edPar.TextLayout.HitTestTextPosition(edPar.SelectionEnd);

      Point? selEndPoint = edPar.TranslatePoint(selEndRect.Position, DocIC);
      if (selEndPoint != null)
         FlowDoc.Selection.EndRect = new Rect((Point)selEndPoint!, selEndRect.Size);

      Paragraph thisPar = (Paragraph)edPar.DataContext!;

      thisPar.DistanceSelectionEndFromLeft = edPar.TextLayout.HitTestTextPosition(edPar.SelectionEnd).Left;
      int lineNo = edPar.TextLayout.GetLineIndexFromCharacterIndex(edPar.SelectionEnd, false);
      thisPar.IsEndAtLastLine = lineNo == edPar.TextLayout.TextLines.Count - 1;

      thisPar.IsEndAtFirstLine = (lineNo == 0);
      if (thisPar.IsEndAtLastLine)
      {
         thisPar.LastIndexEndLine = thisPar.BlockLength; 
         thisPar.CharNextLineEnd = edPar.Text!.Length + 1 + edPar.SelectionEnd - edPar.TextLayout.TextLines[lineNo].FirstTextSourceIndex;
      }
      else
      {
         thisPar.LastIndexEndLine = edPar.TextLayout.TextLines[lineNo + 1].FirstTextSourceIndex - 1;
         thisPar.CharNextLineEnd = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionEndFromLeft, 1);
      }

      if (!thisPar.IsEndAtFirstLine)
         thisPar.CharPrevLineEnd = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionEndFromLeft, -1);


      thisPar.FirstIndexLastLine = edPar.TextLayout.TextLines[^1].FirstTextSourceIndex;

      rtbVM.UpdateCursor();

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

   private void EditableParagraph_CharIndexRect_Notified(EditableParagraph edPar, Rect selStartRect)
   {
      //Point? selStartPoint = edPar.TranslatePoint(selStartRect.Position, DocIC);
      //if (selStartPoint != null)
      //   FlowDoc.Selection.StartRect = new Rect((Point)selStartPoint!, selStartRect.Size);

   }


}


