using Avalonia.Controls;
using System;
using System.Diagnostics;

namespace AvRichTextBox;

public partial class FlowDocument
{
   internal void MoveSelectionRight(bool biasForward)
   {

      Selection!.BiasForward = biasForward;

      if (Selection.Length > 0)
         ResetSelectionLengthZero(Selection.EndParagraph);

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:

            Paragraph endPar = GetContainingParagraph(Selection.End);
            if (endPar == Blocks[^1] && endPar.SelectionEndInBlock == endPar.BlockLength - 1)
               return;  // End of document

            Selection.End += 1;

            break;
         case ExtendMode.ExtendModeRight:
            Selection.End = Math.Min(Selection.End, this.DocEndPoint - 1);
            break;
         case ExtendMode.ExtendModeLeft:
            //Do nothing just collapse selection
            break;
      }

      Selection.CollapseToEnd();
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      ScrollInDirection!(1);


   }

   internal void MoveRightWord()
   {
      if (Selection.Start >= Selection.StartParagraph.StartInDoc + Selection.StartParagraph.BlockLength)
         return;

      Selection!.BiasForward = true;

      Paragraph startP = (Paragraph)Selection.StartParagraph;

      int IndexNext = -1; 
      if (startP.SelectionStartInBlock == startP.Text.Length)
         Selection.End += 1;
      else
      {
         IEditable startInline = Selection.GetStartInline();
         if (startInline.IsUIContainer || startInline.IsLineBreak)
            Selection.End += 1;
         else
         {
            IndexNext = startP.Text.IndexOfAny(" \v".ToCharArray(), startP.SelectionStartInBlock);
            if (IndexNext == -1)
               IndexNext = startP.Text.Length;
            else
               IndexNext += 1;  // go beyond the space

            int NextWordEndPoint = Selection.StartParagraph.StartInDoc + IndexNext;
            Selection.End = NextWordEndPoint;
         }
      }

      Selection.CollapseToEnd();
      ScrollInDirection!(1);

   }
    
   internal void MoveLeftWord()
   {
      if (Selection.Start <= 0)
         return;

      Selection!.BiasForward = false;

      int IndexNext = -1;
      Paragraph startP = (Paragraph)Selection.StartParagraph;
      
      if (startP.SelectionStartInBlock == 0)
         Selection.Start -= 1; 
      else
      {
         Selection.Start -= 1;
         Selection.CollapseToStart();
         
         startP = (Paragraph)Selection.StartParagraph;
         IEditable startInline = Selection.GetStartInline();

         if (!startInline.IsUIContainer)
         {
           IndexNext = startP.Text.LastIndexOfAny(" \v".ToCharArray(), startP.SelectionStartInBlock - 1);
            if (IndexNext == -1)
               IndexNext = 0;
            else
               IndexNext += 1;  // go to right of space

            int NextWordEndPoint = Selection.StartParagraph.StartInDoc + IndexNext;
            Selection.Start = NextWordEndPoint;
         }

      }

      Selection.CollapseToStart();
      ScrollInDirection!(-1);

   }
   
   internal void MoveSelectionLeft(bool biasForward)
   {

      Selection!.BiasForward = biasForward;

      if (Selection!.Length > 0)
         ResetSelectionLengthZero(Selection.StartParagraph);

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:

            if (Selection.Start == 0)
               return;  // Start of document

            Selection.Start -= 1;
            Selection.End = Selection.Start;
            break;

         case ExtendMode.ExtendModeRight:
         case ExtendMode.ExtendModeLeft:
            Selection.CollapseToStart();
            break;
      }

      SelectionExtendMode = ExtendMode.ExtendModeNone;
      ScrollInDirection!(-1);

   }

   internal void MoveSelectionDown(bool biasForward)
   {

      Selection!.BiasForward = biasForward;

      if (Selection!.Length > 0)
      {
         ResetSelectionLengthZero(Selection.EndParagraph);
         Selection.CollapseToEnd();
      }

      if (Selection.EndParagraph.IsEndAtLastLine)
      {
         if (Selection!.EndParagraph != Blocks[^1])
         {
            int nextParIndex = Blocks.IndexOf(Selection.EndParagraph) + 1;
            Paragraph nextPar = (Paragraph)Blocks[nextParIndex];

            int oldSE = Selection.End;
            Selection.End = Math.Min(nextPar.StartInDoc + nextPar.BlockLength - 1, Selection.EndParagraph.StartInDoc + Selection.EndParagraph.CharNextLineEnd);
            //Debug.WriteLine("Old selectionEnd = " + oldSE + " ::: New Selection end: " + Selection.End);
         }
      }
      else
         Selection.End = Selection.EndParagraph.StartInDoc + Selection.EndParagraph.CharNextLineEnd;
         

      Selection.CollapseToEnd();
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      ScrollInDirection!(1);

   }

   internal void MoveSelectionUp(bool biasForward)
   {

      Selection!.BiasForward = biasForward;


      if (Selection!.Length > 0)
      {
         ResetSelectionLengthZero(Selection.StartParagraph);
         Selection.CollapseToStart();
      }

      if (Selection.StartParagraph.IsStartAtFirstLine)
      {
         if (Selection!.StartParagraph != Blocks[0])
         {
            int prevParIndex = Blocks.IndexOf(Selection.StartParagraph) - 1;
            Paragraph prevPar = (Paragraph)Blocks[prevParIndex];
            int oldSS = Selection.Start;
            Selection.Start = Math.Min(prevPar.StartInDoc + prevPar.BlockLength - 1, prevPar.StartInDoc + prevPar.FirstIndexLastLine + Selection.StartParagraph.CharPrevLineStart);
         }
      }
      else
      {
         Selection.Start = Selection.StartParagraph.StartInDoc + Selection.StartParagraph.CharPrevLineStart;
      }
         
      Selection.CollapseToStart();
      
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      ScrollInDirection!(-1);
      
   }

  
   internal void MoveToDocStart()
   {
      Selection.BiasForward = true;
      Selection!.Start = 0;
      Selection.CollapseToStart();
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      ScrollInDirection!(-1);
      
      foreach (Paragraph p in Blocks)
         p.ClearSelection();

      ((Paragraph)Blocks[0]).CallRequestTextLayoutInfoStart();
      ((Paragraph)Blocks[0]).CallRequestTextLayoutInfoEnd();

   }

   internal void SelectAll()
   {
      Selection.Start = 0;
      Selection.End = 0;
      SelectionParagraphs.Clear();
      Selection.End = this.DocEndPoint - 1;
      EnsureSelectionContinuity();
      this.SelectionExtendMode = ExtendMode.ExtendModeRight;
   }
   
   internal void Select(int Start, int Length)
   {
      Selection.Start = 0;
      Selection.CollapseToStart();
      SelectionParagraphs.Clear();

      Selection.Start = Start;
      Selection.End = Start + Length;
      
      EnsureSelectionContinuity();

      UpdateSelection();

   }


   internal void MoveToDocEnd()
   {
      Selection.BiasForward = false;
      Selection!.End = Blocks[^1].StartInDoc + Blocks[^1].BlockLength - 1;
      Selection.CollapseToEnd();
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      ScrollInDirection!(1);
      
      foreach (Paragraph p in Blocks)
         p.ClearSelection();

      Blocks[^1].SelectionStartInBlock = Blocks[^1].BlockLength - 1;
      Blocks[^1].SelectionEndInBlock = Blocks[^1].BlockLength - 1;

   }

   internal void MoveToStartOfLine(bool selExtend)
   {

      Selection.BiasForward = true;
      Selection!.Start = Selection.StartParagraph.StartInDoc + Selection.StartParagraph.FirstIndexStartLine;

      if (!selExtend)
      {
         if (Selection!.Length > 0)
            ResetSelectionLengthZero(Selection.StartParagraph);
         Selection.CollapseToStart();
      }
      else
         SelectionExtendMode = ExtendMode.ExtendModeLeft;

      ScrollInDirection!(-1);

   }

   internal void MoveToEndOfLine(bool selExtend)
   {

      Selection!.BiasForward = false;

      if (Selection!.StartParagraph.Text == "") return;

      Paragraph thisEndPar = Selection.EndParagraph;

      if (thisEndPar.IsEndAtLastLine)
         Selection!.End = Selection.EndParagraph.StartInDoc + thisEndPar.BlockLength - 1;
      else
         Selection!.End = Selection.EndParagraph.StartInDoc + thisEndPar.LastIndexEndLine;

      string parText = thisEndPar.Text;
      if (thisEndPar.LastIndexEndLine <= parText.Length && parText[thisEndPar.LastIndexEndLine] == ' ')
      {
         Selection.IsAtEndOfLineSpace = true;
         Selection.End += 1;
      }
 
      if (!selExtend)
      {
         if (Selection!.Length > 0)
            ResetSelectionLengthZero(Selection.EndParagraph);
         Selection.CollapseToEnd();
      }
      else
         SelectionExtendMode = ExtendMode.ExtendModeRight;

      ScrollInDirection!(1);

      Selection.BiasForward = true;

      Selection.IsAtEndOfLineSpace = false;


   }

   internal void MovePageSelection(int direction, bool extend, int newIndexInDoc)
   {

      newIndexInDoc = Math.Min(newIndexInDoc, this.DocEndPoint - 1);

      switch (direction)
      {
         case 1:

            if (extend)
            {
               switch (SelectionExtendMode)
               {

                  case ExtendMode.ExtendModeRight:
                  case ExtendMode.ExtendModeNone:
                     Selection.End = newIndexInDoc;
                     SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeRight;
                     break;
                  case ExtendMode.ExtendModeLeft:
                     if (newIndexInDoc > Selection.End)
                        SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeRight;
                     Selection.Start = newIndexInDoc;
                     break;
               }
               EnsureSelectionContinuity();
            }
            else
            {
               Selection.End = newIndexInDoc;
               Selection.CollapseToEnd();
            }
               
            break;

         case -1:
            if (extend)
            {
               switch (SelectionExtendMode)
               {
                  case ExtendMode.ExtendModeLeft:
                  case ExtendMode.ExtendModeNone:
                     Selection.Start = newIndexInDoc;
                     SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeLeft;
                     break;
                  case ExtendMode.ExtendModeRight:

                     if (newIndexInDoc < Selection.Start)
                        SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeLeft;
                     Selection.End = newIndexInDoc;
                     //Debug.WriteLine("Extending back, page up, extend mode right : selection.end = " + Selection.End + " (" + newIndexInDoc);
                     break;
               }
               EnsureSelectionContinuity();
            }

            else
            {
               Selection.Start = newIndexInDoc;
               Selection.CollapseToStart();
            }
               

            break;

      }

      
   }

   internal void UpdateCursor()
   {
      Selection.StartParagraph.CallRequestTextLayoutInfoStart();
      Selection.StartParagraph.CallRequestTextLayoutInfoEnd();
      Selection.EndParagraph.CallRequestTextLayoutInfoStart();
      Selection.EndParagraph.CallRequestTextLayoutInfoEnd();
   }

   internal void UpdateSelection()
   {
      UpdateBlockAndInlineStarts(Selection.StartParagraph);
      Selection.StartParagraph.CallRequestInlinesUpdate();
      Selection.EndParagraph.CallRequestInlinesUpdate();
      Selection.GetStartInline();
      Selection.GetEndInline();
      Selection.StartParagraph.CallRequestTextLayoutInfoStart();
      Selection.EndParagraph.CallRequestTextLayoutInfoEnd();
      Selection.StartParagraph.CallRequestTextBoxFocus();
   }


}

