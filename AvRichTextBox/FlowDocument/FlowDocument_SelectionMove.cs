using HtmlAgilityPack;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

public partial class FlowDocument
{
   internal void MoveSelectionRight(bool isTextInsertion)
   {

      Selection.BiasForwardStart = !isTextInsertion;

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:

            Block endBlock = GetContainingParagraph(Selection.End);
            if (endBlock == AllParagraphs.ToList()[^1] && endBlock.SelectionEndInBlock == endBlock.BlockLength - 1)
               return;  // End of document

            if (!isTextInsertion && (Selection.IsAtLineBreak || Selection.IsAtCellBreak))
            {
               Selection.End += 1;
               Selection.CollapseToEnd();
               Selection.BiasForwardStart = !isTextInsertion;
               Selection.BiasForwardEnd = Selection.BiasForwardStart;
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
      ScrollInDirection?.Invoke(1);

      Selection.BiasForwardStart = !isTextInsertion;
      Selection.BiasForwardEnd = Selection.BiasForwardStart;

   }

   internal void MoveSelectionLeft(bool biasForward)
   {
      //Selection.BiasForwardStart = biasForward;
      Selection.BiasForwardStart = true;


      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:

            if (Selection.Start == 0)
               return;  // Start of document

            Selection.Start -= 1;

            if (Selection.IsAtLineBreak || Selection.IsAtCellBreak)
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

      Selection.BiasForwardEnd = Selection.BiasForwardStart;
      Selection.CollapseToStart();
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      ScrollInDirection?.Invoke(-1);

   }


   internal int GetRelativeTextPos(IEditable inline, int absTextPos)
   {
      //if (Blocks.FirstOrDefault(b => b.Id == inline.MyParagraphId) is not Block myBlock) return -1;
      if (AllParagraphs.FirstOrDefault(p => p.Id == inline.MyParagraphId) is not Block myBlock) return -1;
      return absTextPos - myBlock.StartInDoc - inline.TextPositionOfInlineInParagraph;
   }
     
   internal void MoveRightWord()
   {
      if (Selection.Start >= Selection.StartParagraph.StartInDoc + Selection.StartParagraph.BlockLength)
         return;

      //Select(GetNextWordPosition(), 0);
      //return;
      
      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

      Paragraph startP = Selection.StartParagraph;

      if (startP.SelectionStartInBlock == startP.TextLength)
         Selection.End += 1;
      else
      {
         if (Selection.GetStartInline() is IEditable startInline)
         {
            if (startInline.IsUIContainer || startInline.IsLineBreak)
               Selection.End += 1;
            else
            {
               int currentTextPos = Selection.Start;

               int foundNextIndex = GetRelativeTextPos(startInline, currentTextPos);

               bool breakWhile = false;
               while (startInline?.InlineText.IndexOf(' ', foundNextIndex) is int IndexNext)
               {
                  if (IndexNext == -1)
                  {
                     currentTextPos += (startInline.InlineLength - foundNextIndex);

                     startInline = GetNextInline(startInline) ?? null!;

                     if (startInline != null)
                     {
                        switch (startInline)
                        {
                           case EditableRun erun:
                              if (startP.Id != startInline.MyParagraphId)
                              {
                                 startP = GetNextParagraph(startP)!;
                                 startInline = startP.Inlines[0];
                                 currentTextPos = startP.StartInDoc + startInline.TextPositionOfInlineInParagraph;
                                 foundNextIndex = 0;
                                 breakWhile = true;
                              }
                              else
                                 foundNextIndex = GetRelativeTextPos(startInline, currentTextPos);
                              break;

                           case EditableLineBreak elb:
                              startInline = GetNextInline(startInline) ?? null!;
                              currentTextPos += 2;
                              foundNextIndex = 0;
                              breakWhile = true;
                              break;

                           default:
                              foundNextIndex = startInline!.InlineLength;
                              breakWhile = true;
                              break;
                        }
                     }

                     if (breakWhile) break;
                  }
                  else
                  {
                     currentTextPos += IndexNext;
                     foundNextIndex = IndexNext;
                     break;
                  }

               }

               if (!breakWhile)
                  foundNextIndex += 1;  // go beyond the space if found

               int NextWordEndPoint = 0;
               if (startInline != null)
                  NextWordEndPoint = startP.StartInDoc + startInline!.TextPositionOfInlineInParagraph + foundNextIndex;
               else
                  NextWordEndPoint = startP.StartInDoc + startP.BlockLength;

               Selection.Start = NextWordEndPoint;
               Selection.End = NextWordEndPoint;

            }
         }
      }

      Selection.CollapseToEnd();
      ScrollInDirection?.Invoke(1);

   }

   internal void MoveLeftWord()
   {
      if (Selection.Start <= 0)
         return;

      Selection.BiasForwardStart = false;
      Selection.BiasForwardEnd = false;

      int IndexNext = -1;
      Paragraph startP = (Paragraph)Selection.StartParagraph;

      if (startP.SelectionStartInBlock == 0)
         Selection.Start -= 1;
      else
      {
         Selection.Start -= 1;
         Selection.CollapseToStart();

         startP = (Paragraph)Selection.StartParagraph;

         if (Selection.GetStartInline() is IEditable startInline && !startInline.IsUIContainer)
         {
            IndexNext = startP.Text.LastIndexOfAny(" \n".ToCharArray(), startP.SelectionStartInBlock - 1);
            if (IndexNext == -1)
               IndexNext = 0;
            else
               IndexNext += 1;  // go to right of space

            int NextWordEndPoint = Selection.StartParagraph.StartInDoc + IndexNext;
            Selection.Start = NextWordEndPoint;
         }

      }

      Selection.CollapseToStart();
      ScrollInDirection?.Invoke(-1);

   }


   internal void MoveSelectionDown(bool biasForward)
   {

      Selection.BiasForwardStart = biasForward;

      if (Selection.Length > 0)
         Selection.CollapseToEnd();

      int nextEnd = Selection.EndParagraph.StartInDoc + Selection.EndParagraph.CharNextLineEnd;


      if (Selection.EndParagraph.IsEndAtLastLine)
      {
         List<Paragraph> allPars = AllParagraphs;

         if (Selection.EndParagraph != allPars[^1])
         {
            int nextParIndex = allPars.IndexOf(Selection.EndParagraph) + 1;
            Paragraph nextPar = allPars[nextParIndex];
            Selection.End = Math.Min(nextPar.StartInDoc + nextPar.BlockLength - 1, nextEnd);  // change BlockLength to be length of first line
         }
      }
      else
         Selection.End = nextEnd;


      Selection.CollapseToEnd();
      SelectionExtendMode = ExtendMode.ExtendModeNone;

      ScrollInDirection?.Invoke(1);

   }

   internal void MoveSelectionUp(bool biasForward)
   {

      Selection.BiasForwardStart = biasForward;

      if (Selection.Length > 0)
         Selection.CollapseToStart();

      if (Selection.StartParagraph.IsStartAtFirstLine)
      {        
         List<Paragraph> allPars = AllParagraphs;
         if (Selection.StartParagraph != allPars[0])
         {            
            int prevParIndex = allPars.IndexOf(Selection.StartParagraph) - 1;
            if (allPars[prevParIndex] is Paragraph prevPar)
               Selection.Start = Math.Min(prevPar.StartInDoc + prevPar.BlockLength - 1, prevPar.StartInDoc + prevPar.FirstIndexLastLine + Selection.StartParagraph.CharPrevLineStart);
         }
      }
      else
      {
         Selection.Start = Selection.StartParagraph.StartInDoc + Selection.StartParagraph.CharPrevLineStart;
      }

      Selection.CollapseToStart();

      SelectionExtendMode = ExtendMode.ExtendModeNone;
      ScrollInDirection?.Invoke(-1);

   }


   internal void MoveToDocStart()
   {
      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;
      Selection.Start = 0;
      Selection.CollapseToStart();
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      ScrollInDirection?.Invoke(-1);

      List<Paragraph> allPars = AllParagraphs;
      //foreach (Paragraph p in allPars)
      //   p.ClearSelection();

      if (allPars[0] is Paragraph firstPar)
      {
         firstPar.CallRequestTextLayoutInfoStart();
         firstPar.CallRequestTextLayoutInfoEnd();
      }

   }

   internal void MoveToDocEnd()
   {
      Selection.BiasForwardStart = false;
      Selection.BiasForwardEnd = false;

      List<Paragraph> allPars = AllParagraphs;

      Selection.End = allPars[^1].StartInDoc + allPars[^1].BlockLength - 1;
      Selection.CollapseToEnd();
      SelectionExtendMode = ExtendMode.ExtendModeNone;
      ScrollInDirection?.Invoke(1);

      //foreach (Paragraph p in allPars)
      //   p.ClearSelection();

      if (allPars[^1] is Paragraph lastPar)
      {
         lastPar.SelectionStartInBlock = lastPar.BlockLength - 1;
         lastPar.SelectionEndInBlock = lastPar.BlockLength - 1;
      }


      //Necessary for caret movement
      Selection.Start = 0;
      Selection.CollapseToStart();
      ///////////////////////////////

      Select(DocEndPoint - 1, 0);

      UpdateRTBCaret?.Invoke();

   }

   internal void MoveToStartOfLine(bool selExtend)
   {

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;
      Selection.Start = Selection.StartParagraph.StartInDoc + Selection.StartParagraph.FirstIndexStartLine;

      if (!selExtend)
         Selection.CollapseToStart();
      else
         SelectionExtendMode = ExtendMode.ExtendModeLeft;

      ScrollInDirection?.Invoke(-1);

   }

   internal void MoveToEndOfLine(bool selExtend)
   {

      Selection.BiasForwardStart = false;
      Selection.BiasForwardEnd = false;

      if (Selection.StartParagraph.TextLength == 0) return;

      Paragraph thisEndPar = Selection.EndParagraph;

      if (thisEndPar.IsEndAtLastLine)
         Selection.End = Selection.EndParagraph.StartInDoc + thisEndPar.BlockLength - 1;
      else
         Selection.End = Selection.EndParagraph.StartInDoc + thisEndPar.LastIndexEndLine;

      string parText = thisEndPar.Text;
      if (thisEndPar.LastIndexEndLine <= parText.Length && (parText[thisEndPar.LastIndexEndLine] == ' ' || IsCJKChar(parText[thisEndPar.LastIndexEndLine])))
      {
         Selection.IsAtEndOfLineSpace = true;
         Selection.End += 1;
      }

      if (!selExtend)
         Selection.CollapseToEnd();
      else
         SelectionExtendMode = ExtendMode.ExtendModeRight;

      ScrollInDirection?.Invoke(1);

      Selection.BiasForwardStart = false;
      Selection.BiasForwardEnd = Selection.BiasForwardStart;

      Selection.IsAtEndOfLineSpace = false;

      IEditable? startInline = Selection.GetStartInline();
      IEditable? nextInline = startInline == null ? null : GetNextInline(startInline);
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
            }

            else
            {
               Selection.Start = newIndexInDoc;
               Selection.CollapseToStart();
            }


            break;

      }


   }

   internal void UpdateCaret()
   {
      Selection.StartParagraph.CallRequestTextLayoutInfoStart();
      Selection.StartParagraph.CallRequestTextLayoutInfoEnd();
      Selection.EndParagraph.CallRequestTextLayoutInfoStart();
      Selection.EndParagraph.CallRequestTextLayoutInfoEnd();
   }


}

