using Avalonia.Controls;
using System;
using System.Diagnostics;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

public partial class FlowDocument
{
   internal void MoveSelectionRight(bool isTextInsertion)
   {

      if (Selection.Length > 0)
         ResetSelectionLengthZero(Selection.EndParagraph);

      Selection!.BiasForwardStart = isTextInsertion ? false : true;

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:

            Paragraph endPar = GetContainingParagraph(Selection.End);
            if (endPar == Blocks[^1] && endPar.SelectionEndInBlock == endPar.BlockLength - 1)
               return;  // End of document

            if (!isTextInsertion && Selection.IsAtLineBreak)
            {
               Selection.End += 1;
               Selection.CollapseToEnd();
               Selection!.BiasForwardStart = isTextInsertion ? false : true;
               Selection!.BiasForwardEnd = Selection.BiasForwardStart;
            }

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

      Selection!.BiasForwardStart = isTextInsertion ? false : true;
      Selection!.BiasForwardEnd = Selection.BiasForwardStart;


   }

   internal void MoveSelectionLeft(bool biasForward)
   {

      //Selection!.BiasForwardStart = biasForward;
      Selection!.BiasForwardStart = true;

      if (Selection!.Length > 0)
         ResetSelectionLengthZero(Selection.StartParagraph);

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:

            if (Selection.Start == 0)
               return;  // Start of document

            Selection.Start -= 1;

            if (Selection.IsAtLineBreak)
            {
               Selection.Start -= 1;
               Selection.CollapseToStart();
            }

            break;

         case ExtendMode.ExtendModeRight:
         case ExtendMode.ExtendModeLeft:
            Selection.CollapseToStart();
            break;
      }

      Selection!.BiasForwardEnd = Selection!.BiasForwardStart;
      Selection.CollapseToStart();
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      ScrollInDirection!(-1);

   }


   internal void MoveRightWord()
   {
      if (Selection.Start >= Selection.StartParagraph.StartInDoc + Selection.StartParagraph.BlockLength)
         return;

      Selection!.BiasForwardStart = true;
      Selection!.BiasForwardEnd = true;

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

      Selection!.BiasForwardStart = false;
      Selection!.BiasForwardEnd = false;

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
   
 
   internal void MoveSelectionDown(bool biasForward)
   {

      Selection.BiasForwardStart = biasForward;

      if (Selection.Length > 0)
      {
         ResetSelectionLengthZero(Selection.EndParagraph);
         Selection.CollapseToEnd();
      }


      int nextEnd = Selection.EndParagraph.StartInDoc + Selection.EndParagraph.CharNextLineEnd;
      if (Selection.EndParagraph.IsEndAtLastLine)
      {
         if (Selection.EndParagraph != Blocks[^1])
         {
            int nextParIndex = Blocks.IndexOf(Selection.EndParagraph) + 1;
            Paragraph nextPar = (Paragraph)Blocks[nextParIndex];
            int oldSE = Selection.End;
            Selection.End = Math.Min(nextPar.StartInDoc + nextPar.BlockLength - 1, nextEnd);
         }
      }
      else
         Selection.End = nextEnd;
         

      Selection.CollapseToEnd();
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      
      ScrollInDirection!(1);

   }

   internal void MoveSelectionUp(bool biasForward)
   {

      Selection!.BiasForwardStart = biasForward;

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
      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;
      Selection!.Start = 0;
      Selection.CollapseToStart();
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      ScrollInDirection!(-1);
      
      

      foreach (Paragraph p in Blocks)
         p.ClearSelection();

      ((Paragraph)Blocks[0]).CallRequestTextLayoutInfoStart();
      ((Paragraph)Blocks[0]).CallRequestTextLayoutInfoEnd();

   }

   internal void MoveToDocEnd()
   {
      Selection.BiasForwardStart = false;
      Selection.BiasForwardEnd = false;
      Selection!.End = Blocks[^1].StartInDoc + Blocks[^1].BlockLength - 1;
      Selection.CollapseToEnd();
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      ScrollInDirection!(1);

      foreach (Paragraph p in Blocks)
         p.ClearSelection();

      Blocks[^1].SelectionStartInBlock = Blocks[^1].BlockLength - 1;
      Blocks[^1].SelectionEndInBlock = Blocks[^1].BlockLength - 1;

      //Necessary for cursor movement
      Selection.Start = 0;
      Selection.CollapseToStart();
      ///////////////////////////////
      
      Select(DocEndPoint - 1, 0);
      
      UpdateRTBCursor?.Invoke();

   }

   internal void MoveToStartOfLine(bool selExtend)
   {

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;
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

      Selection!.BiasForwardStart = false;
      Selection!.BiasForwardEnd = false;

      if (Selection!.StartParagraph.Text == "") return;

      Paragraph thisEndPar = Selection.EndParagraph;

      if (thisEndPar.IsEndAtLastLine)
         //Selection!.End = Selection.EndParagraph.StartInDoc + thisEndPar.BlockLength - (Blocks.IndexOf(thisEndPar) == Blocks.Count - 1 ? 2 : 1);
         Selection!.End = Selection.EndParagraph.StartInDoc + thisEndPar.BlockLength - 1;
      else
         Selection!.End = Selection.EndParagraph.StartInDoc + thisEndPar.LastIndexEndLine;

      string parText = thisEndPar.Text;
      if (thisEndPar.LastIndexEndLine <= parText.Length && (parText[thisEndPar.LastIndexEndLine] == ' ' || IsCJKChar(parText[thisEndPar.LastIndexEndLine])))
      {
         Selection.IsAtEndOfLineSpace = true;
         Selection.End += 1;
      }

      //////***********
      
      if (!selExtend)
      {
         if (Selection!.Length > 0)
            ResetSelectionLengthZero(Selection.EndParagraph);
         Selection.CollapseToEnd();
      }
      else
         SelectionExtendMode = ExtendMode.ExtendModeRight;

      ScrollInDirection!(1);

      Selection.BiasForwardStart = false;
      Selection.BiasForwardEnd = Selection.BiasForwardStart;

      Selection.IsAtEndOfLineSpace = false;

      IEditable startInline = Selection.GetStartInline();
      IEditable? nextInline = GetNextInline(startInline);
      Selection.IsAtLineBreak = nextInline != null && nextInline.IsLineBreak;

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

  
}

