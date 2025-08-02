using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AvRichTextBox;

public partial class RichTextBox : UserControl
{   

   internal void SelectionStart_RectChanged(EditableParagraph edPar)
   {
      edPar.UpdateLayout();
      
      TextLayout tlayout = edPar.TextLayout;

      Rect selStartRect = tlayout.HitTestTextPosition(edPar.SelectionStart);
      Rect prevCharRect = tlayout.HitTestTextPosition(edPar.SelectionStart - 1);

      Point? selStartPoint = edPar.TranslatePoint(selStartRect.Position, DocIC);
      Point? prevCharPoint = edPar.TranslatePoint(prevCharRect.Position, DocIC);

      if (selStartPoint == null || prevCharPoint == null) return;

      //Debug.WriteLine("selstartpoint= " + selStartPoint!.Value.Y);

      if (selStartPoint != null)
         FlowDoc.Selection.StartRect = new Rect((Point)selStartPoint, selStartRect.Size);

      if (prevCharPoint != null)
         FlowDoc.Selection.PrevCharRect = new Rect((Point)prevCharPoint, prevCharRect.Size);

      if (edPar.DataContext is not Paragraph thisPar) return;

      thisPar.DistanceSelectionStartFromLeft = selStartRect.Left;

      List<TextLine> textLines = tlayout.TextLines.ToList();

      int lineNo = tlayout.GetLineIndexFromCharacterIndex(edPar.SelectionStart, false);
      thisPar.IsStartAtFirstLine = lineNo == 0;
      thisPar.IsStartAtLastLine = (lineNo == textLines.Count - 1);

      if (thisPar.IsStartAtFirstLine)
         thisPar.CharPrevLineStart = edPar.SelectionStart;
      else
      {  // get index of first char on previous line
         thisPar.CharPrevLineStart = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionStartFromLeft, -1);
      }

      if (thisPar.IsStartAtLastLine)
         thisPar.CharNextLineStart = edPar.SelectionEnd - textLines[lineNo].FirstTextSourceIndex;
      else
         thisPar.CharNextLineStart = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionStartFromLeft, 1);

      //thisPar.FirstIndexStartLine = tlayout.TextLines[lineNo].FirstTextSourceIndex;
      thisPar.FirstIndexStartLine = FlowDoc.Selection.IsAtEndOfLineSpace ? 
         textLines[Math.Max(0, lineNo -1)].FirstTextSourceIndex : 
         textLines[lineNo].FirstTextSourceIndex;
      thisPar.FirstIndexLastLine = textLines[^1].FirstTextSourceIndex;


      //**********Fix caret height and position*********:
      int lineIndex = tlayout.GetLineIndexFromCharacterIndex(edPar.SelectionStart, false);

      rtbVM.CaretHeight = textLines[lineIndex].Extent;
      if (rtbVM.CaretHeight == 0)
         rtbVM.CaretHeight = textLines[lineIndex].Height;
      rtbVM.CaretHeight += 4; // give it an extra bit


      double caretMLeft = selStartPoint!.Value.X;
      double caretMTop = textLines[lineIndex].Start;

      double textTopY = FlowDoc.Selection.StartRect.Top + (textLines[lineIndex].Extent == 0 ? 0 : Math.Max(0, textLines[lineIndex].Baseline - textLines[lineIndex].Extent));
      //Debug.WriteLine("baseline = " + textLines[lineIndex].Baseline + "\nextent = " + textLines[lineIndex].Extent + "\ntextopY = " + textTopY);

      if (FlowDoc.Selection.IsAtEndOfLineSpace)
      {
         caretMLeft = FlowDoc.Selection!.PrevCharRect!.Right;
         caretMTop = FlowDoc.Selection!.PrevCharRect.Top + 1;
      }
      else
         //caretMTop = selStartPoint.Value.Y;
         caretMTop = textTopY;

      rtbVM.CaretMargin = new Thickness(caretMLeft, caretMTop, 0, 0);
      rtbVM.UpdateCaretVisible();

      // Visualization rectangles:
      //rtbVM.LineHeightRectMargin = new Thickness(caretMLeft + 3, FlowDoc.Selection.StartRect.Top, 0, 0);
      //rtbVM.LineHeightRectHeight = textLines[lineIndex].Height; //selStartRect.Size.Height
      //rtbVM.BaseLineRectMargin = new Thickness(caretMLeft + 5, FlowDoc.Selection.StartRect.Top + textLines[lineIndex].Baseline - textLines[lineIndex].Extent, 0, 0);
      //rtbVM.BaseLineRectHeight = textLines[lineIndex].Baseline; //selStartRect.Size.Height

   }

   internal void SelectionEnd_RectChanged(EditableParagraph edPar)
   {
      edPar.UpdateLayout();

      TextLayout tlayout = edPar.TextLayout;

      Rect selEndRect = tlayout.HitTestTextPosition(edPar.SelectionEnd);

      Point? selEndPoint = edPar.TranslatePoint(selEndRect.Position, DocIC);
      if (selEndPoint != null)
         FlowDoc.Selection.EndRect = new Rect((Point)selEndPoint!, selEndRect.Size);

      //Paragraph thisPar = (Paragraph)edPar.DataContext!;
      if (edPar.DataContext is not Paragraph thisPar) return;

      thisPar.DistanceSelectionEndFromLeft = tlayout.HitTestTextPosition(edPar.SelectionEnd).Left;
      int lineNo = tlayout.GetLineIndexFromCharacterIndex(edPar.SelectionEnd, false);
      
      List<TextLine> textLines = tlayout.TextLines.ToList();

      thisPar.IsEndAtLastLine = lineNo == textLines.Count - 1;

      thisPar.IsEndAtFirstLine = (lineNo == 0);
      if (thisPar.IsEndAtLastLine)
      {
         thisPar.LastIndexEndLine = thisPar.BlockLength;
         thisPar.CharNextLineEnd = edPar.Text!.Length + 1 + edPar.SelectionEnd - textLines[lineNo].FirstTextSourceIndex;
      }
      else
      {
         TextLine tline = textLines[lineNo];
         //Debug.WriteLine("textstring ="  + string.Join("\r", (tline.TextRuns.Select(tr=>tr.Text))));
         //Debug.WriteLine("lastis rn? "  + (tline.TextRuns.Last().Text.ToString() == "\r\n"));
         
         int goBackNo = 1;
         if (tline.TextRuns.Count > 0)
         {
            if (tline.TextRuns[tline.TextRuns.Count - 1].Text.ToString() == "\r\n")
               goBackNo++;
         }
         
         thisPar.LastIndexEndLine = textLines[lineNo + 1].FirstTextSourceIndex - goBackNo;
         thisPar.CharNextLineEnd = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionEndFromLeft, 1);
      }

      if (!thisPar.IsEndAtFirstLine)
         thisPar.CharPrevLineEnd = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionEndFromLeft, -1);


      thisPar.FirstIndexLastLine = textLines[^1].FirstTextSourceIndex;

      rtbVM.UpdateCaretVisible();

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


