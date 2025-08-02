using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System;
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
      //if (thisPar == null) return;

      thisPar.DistanceSelectionStartFromLeft = selStartRect.Left;

      int lineNo = tlayout.GetLineIndexFromCharacterIndex(edPar.SelectionStart, false);
      thisPar.IsStartAtFirstLine = lineNo == 0;
      thisPar.IsStartAtLastLine = (lineNo == tlayout.TextLines.Count - 1);

      if (thisPar.IsStartAtFirstLine)
         thisPar.CharPrevLineStart = edPar.SelectionStart;
      else
      {  // get index of first char on previous line
         thisPar.CharPrevLineStart = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionStartFromLeft, -1);
      }

      if (thisPar.IsStartAtLastLine)
         thisPar.CharNextLineStart = edPar.SelectionEnd - tlayout.TextLines[lineNo].FirstTextSourceIndex;
      else
         thisPar.CharNextLineStart = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionStartFromLeft, 1);

      //thisPar.FirstIndexStartLine = tlayout.TextLines[lineNo].FirstTextSourceIndex;
      thisPar.FirstIndexStartLine = FlowDoc.Selection.IsAtEndOfLineSpace ? 
         tlayout.TextLines[Math.Max(0, lineNo -1)].FirstTextSourceIndex : 
         tlayout.TextLines[lineNo].FirstTextSourceIndex;
      thisPar.FirstIndexLastLine = tlayout.TextLines[^1].FirstTextSourceIndex;


      //**********Fix caret height and position*********:
      int lineIndex = tlayout.GetLineIndexFromCharacterIndex(edPar.SelectionStart, false);

      //Debug.WriteLine("Fixing caret height & position\nLineHeight = " + tlayout.TextLines[lineIndex].Height + ", Extent = " + tlayout.TextLines[lineIndex].Extent);

      rtbVM.CaretHeight = tlayout.TextLines[lineIndex].Extent;
      if (rtbVM.CaretHeight == 0)
         rtbVM.CaretHeight = tlayout.TextLines[lineIndex].Height;
      rtbVM.CaretHeight += 5; // give it an extra bit


      double caretML = selStartPoint!.Value.X;
      double caretMT = tlayout.TextLines[lineIndex].Start;

      if (FlowDoc.Selection.IsAtEndOfLineSpace)
      {
         caretML = FlowDoc.Selection!.PrevCharRect!.Right;
         caretMT = FlowDoc.Selection!.PrevCharRect.Top + 1;
      }
      else
         caretMT = selStartPoint.Value.Y;
         //caretMT = FlowDoc.Selection.StartRect.Top; 
         //caretMT = FlowDoc.Selection.StartRect.Top + (tlayout.TextLines[lineIndex].Height - FlowDoc.Selection.StartRect.Height);
         

      rtbVM.CaretMargin = new Thickness(caretML, caretMT, 0, 0);
      rtbVM.UpdateCaretVisible();

   }

   internal void SelectionEnd_RectChanged(EditableParagraph edPar)
   {
      edPar.UpdateLayout();

      Rect selEndRect = edPar.TextLayout.HitTestTextPosition(edPar.SelectionEnd);

      Point? selEndPoint = edPar.TranslatePoint(selEndRect.Position, DocIC);
      if (selEndPoint != null)
         FlowDoc.Selection.EndRect = new Rect((Point)selEndPoint!, selEndRect.Size);

      Paragraph thisPar = (Paragraph)edPar.DataContext!;
      //if (thisPar == null) return;

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
         //Debug.WriteLine("partext = " + thisPar.Text);
         //Debug.WriteLine("partext = " + thisPar.Inlines.Count);

         //IEditable ied = FlowDoc.Selection.GetStartInline();
         //Debug.WriteLine("ied.= " + ied.DisplayInlineText);

         //Debug.WriteLine("count = " + FlowDoc.GetRangeInlines(FlowDoc.Selection).Count);
         //if (FlowDoc.GetRangeInlines(FlowDoc.Selection).Count > 0)
         //   Debug.WriteLine("count = *" + FlowDoc.GetRangeInlines(FlowDoc.Selection)[0].InlineText + "*");

         TextLine tline = edPar.TextLayout.TextLines[lineNo];
         //Debug.WriteLine("textstring ="  + string.Join("\r", (tline.TextRuns.Select(tr=>tr.Text))));
         //Debug.WriteLine("lastis rn? "  + (tline.TextRuns.Last().Text.ToString() == "\r\n"));
         
         int goBackNo = 1;
         if (tline.TextRuns.Count > 0)
         {
            if (tline.TextRuns[tline.TextRuns.Count - 1].Text.ToString() == "\r\n")
               goBackNo++;
               
         }
         
         thisPar.LastIndexEndLine = edPar.TextLayout.TextLines[lineNo + 1].FirstTextSourceIndex - goBackNo;
         thisPar.CharNextLineEnd = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionEndFromLeft, 1);
      }

      if (!thisPar.IsEndAtFirstLine)
         thisPar.CharPrevLineEnd = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionEndFromLeft, -1);


      thisPar.FirstIndexLastLine = edPar.TextLayout.TextLines[^1].FirstTextSourceIndex;

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


