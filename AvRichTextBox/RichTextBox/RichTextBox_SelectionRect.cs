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

      if (selStartPoint != null)
         FlowDoc.Selection.StartRect = new Rect((Point)selStartPoint!, selStartRect.Size);

      if (prevCharPoint != null)
         FlowDoc.Selection.PrevCharRect = new Rect((Point)prevCharPoint!, prevCharRect.Size);

      Paragraph thisPar = (Paragraph)edPar.DataContext!;
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
         //IEditable? nextInline = FlowDoc.GetNextInline(FlowDoc.Selection.GetStartInline());
         //if (nextInline != null && nextInline.IsLineBreak)
         //{
         //   Debug.WriteLine("thisinlinetext=" + FlowDoc.Selection.GetStartInline().InlineText);
         //   Debug.WriteLine("nextinlinetext=" + nextInline.InlineText);
         //}
         //   //thisPar.CharPrevLineStart += 1;
      }


      if (thisPar.IsStartAtLastLine)
         thisPar.CharNextLineStart = edPar.SelectionEnd - tlayout.TextLines[lineNo].FirstTextSourceIndex;
      else
         thisPar.CharNextLineStart = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionStartFromLeft, 1);

      thisPar.FirstIndexStartLine = tlayout.TextLines[lineNo].FirstTextSourceIndex;
      thisPar.FirstIndexLastLine = tlayout.TextLines[^1].FirstTextSourceIndex;
           

      //rtbVM.CursorHeight = selStartRect.Height;
      rtbVM.CursorHeight = tlayout.Baseline + 5;  //temporary
      


      double cursorML = selStartPoint!.Value.X;
      double cursorMT = FlowDoc.Selection.StartRect.Top + 2;

      //Debug.WriteLine("edparLH= " + edPar.LineSpacing + "\nthisparlH= " + thisPar.LineSpacing);

      
      if (FlowDoc.Selection.IsAtEndOfLineSpace)
      {
         cursorML = FlowDoc.Selection!.PrevCharRect!.Right;
         cursorMT = FlowDoc.Selection!.PrevCharRect.Top + 2;
      }
      else if (FlowDoc.Selection.GetStartInline() is EditableRun startRun)
      {
         //rtbVM.CursorHeight = startRun.FontSize  * 1.25;
         rtbVM.CursorHeight = startRun.FontSize  * 1.25 + startRun.myParagraph!.LineSpacing;
         //cursorMT = FlowDoc.Selection.StartRect.Top + FlowDoc.Selection.StartRect.Height - rtbVM.CursorHeight;
         cursorMT = FlowDoc.Selection.StartRect.Bottom - rtbVM.CursorHeight - 4;
         //Debug.WriteLine("thisparlH= " + thisPar.LineSpacing);
      }
      
      //Take into account linespacing
      cursorMT -= thisPar.LineSpacing;

      rtbVM.CursorMargin = new Thickness(cursorML, cursorMT, 0, 0);
      rtbVM.UpdateCursorVisible();

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

      rtbVM.UpdateCursorVisible();

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


