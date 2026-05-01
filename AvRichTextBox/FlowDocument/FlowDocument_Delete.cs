using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Wordprocessing;
using DynamicData;

namespace AvRichTextBox;

public partial class FlowDocument
{
   internal void DeleteChar(bool backspace)
   {
      int originalSelectionStart = Selection.Start;
      
      //keep in cell
      if (Selection.StartParagraph.IsTableCellBlock)
      {
         bool keepInCell = (backspace && Selection.StartParagraph.SelectionStartInBlock == 0) || (!backspace && Selection.StartParagraph.SelectionStartInBlock >= Selection.StartParagraph.BlockLength - 1);
         if (keepInCell) return;
      }

      if (backspace)
         MoveSelectionLeft(true);

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

      if (Selection.GetStartInline() is not IEditable startInline) return;

      Paragraph startP = Selection.StartParagraph;

      if (startP.SelectionStartInBlock == startP.TextLength)
         MergeParagraphForward(Selection.Start, true, originalSelectionStart);
      else
      {  //Delete one unit
         int startInlineIdx = startP.Inlines.IndexOf(startInline);
         int selectionStartInInline = 0;

         if (startInline is EditableInlineUIContainer eIUC)
         {
            bool emptyRunAdded = false;
            if (startP.Inlines.Count == 1)
            {
               startP.Inlines.Add(new EditableRun(""));
               emptyRunAdded = true;
            }
               
            Undos.Add(new DeleteImageUndo(startP.Id, eIUC, startInlineIdx, this, originalSelectionStart, emptyRunAdded));

            startP.Inlines.Remove(eIUC);
         }
         else
         {
            bool isSelectionAtInlineEnd = GetCharPosInInline(startInline, Selection.End) == startInline.InlineLength;

            if (GetNextInline(startInline) is EditableLineBreak lbreak && isSelectionAtInlineEnd)
            {  //Delete linebreak
               IEditable? lbnext = GetNextInline(lbreak);
               startP.Inlines.Remove(lbreak);
               int emptyRunId = -1;
               if (lbnext != null && lbnext.IsEmpty)
               {
                  startP.Inlines.Remove(lbnext);
                  emptyRunId = lbnext.Id;
               }
               else if (startInline.IsEmpty)
               {
                  startP.Inlines.Remove(startInline);
                  emptyRunId = startInline.Id;
               }

               Undos.Add(new DeleteLineBreakUndo(startP.Id, lbreak.Id, emptyRunId, this, originalSelectionStart));

            }
            else
            {  // delete normal run char
               if (startInline.InlineLength == 1 && GetNextInline(startInline) is not EditableLineBreak elb)  // keep empty run on linebreak
               {
                  if (startInline.CloneWithId() is EditableRun removedRunClone)
                  {
                     startP.Inlines.Remove(startInline);
                     Undos.Add(new DeleteRunUndo(startP.Id, removedRunClone, startInlineIdx, this, originalSelectionStart));
                  }
               }
               else
               {
                  selectionStartInInline = GetCharPosInInline(startInline, Selection.Start);
                  if (selectionStartInInline < startInline.InlineLength)
                     startInline.InlineText = startInline.InlineText.Remove(selectionStartInInline, 1);   // undo handled by PropertyChanged: Text
               }
               
               //Paragraph must always have at least an empty run
               if (startP.Inlines.Count == 0)
                  startP.Inlines.Add(new EditableRun(""));
            }
         }

         UpdateTextRanges(Selection.Start, -1);

         UpdateBlockAndInlineStarts(AllParagraphs.ToList().IndexOf(startP));
      }

      SelectionStart_Changed(Selection, Selection.Start);
      Selection.StartParagraph.CallRequestInlinesUpdate();
      Selection.StartParagraph.CallRequestTextLayoutInfoStart();



   }

   internal void DeleteSelection()
   {
      DeleteRange(Selection, true, true);
      SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeNone;
      UpdateBlockAndInlineStarts(Selection.StartParagraph);
      Selection.CollapseToStart();
      Selection.BiasForwardStart = false;  
      Selection.BiasForwardEnd = false;  

   }

   internal (int idLeft, int idRight) DeleteRange(TextRange trange, bool addUndo, bool adjustCursor)
   {
      bool docContainsOneBlock = Blocks.Count == 1;
      int originalSelectionStart = Selection.Start;
      int originalTRangeLength = trange.Length;
      List<Paragraph> rangePars = GetOverlappingParagraphsInRange(trange);
      int firstParId = rangePars.First().Id;
      int firstParIndex = GetAllParagraphs.IndexOf(rangePars.First());
      
      disableRunTextUndo = true;

      List<Table> tablesFullyInRange = GetFullTablesInRange(trange);
      List<Paragraph> paragraphsFullyInRange = GetFullParagraphsInRange(trange);

      bool firstParDeleted = paragraphsFullyInRange.Count > 0 && paragraphsFullyInRange.First().StartInDoc == originalSelectionStart;

      IEditable lastInline = rangePars[0].Inlines.Last();
      
      if (rangePars.Count == 1 && rangePars[0].Inlines.Count == 1 && GetCharPosInInline(lastInline, Selection.Start) ==  lastInline.InlineLength) 
         return (lastInline.Id, -1);      

      if (addUndo) 
         Undos.Add(new DeleteRangeUndo(rangePars.ConvertAll(rpar=> rpar.FullClone()), firstParIndex, this, originalSelectionStart, originalTRangeLength, firstParDeleted));

      (int idLeft, int idRight) edgeIds;
      List<IEditable> rangeInlines = GetRangeInlinesAndAddToDoc(trange, out edgeIds);

      //Delete the created inlines
      foreach (IEditable toDeleteRun in rangeInlines)
      {
         if (AllParagraphs.FirstOrDefault(p => p.Id == toDeleteRun.MyParagraphId) is Paragraph rangePar)
         {
            rangePar.Inlines.Remove(toDeleteRun);
            rangePar.CallRequestInlinesUpdate();
         }
      }

      //Delete any full blocks contained within the range
      foreach (Paragraph fullyContainedPar in paragraphsFullyInRange)
      {
         fullyContainedPar.Inlines.Clear();
         if (!fullyContainedPar.IsTableCellBlock && !docContainsOneBlock)
            Blocks.Remove(fullyContainedPar);
      }

      Blocks.RemoveMany(tablesFullyInRange);

      Paragraph firstPar = rangePars[0];
      Paragraph lastPar = rangePars[^1];

      //Add a blank run if all runs were deleted in one paragraph
      if (rangePars.Count == 1 && firstPar.Inlines.Count == 0)
         firstPar.Inlines.Add(new EditableRun(""));


      //Merge inlines of last paragraph with first if present
      if (rangePars.Count > 1 && Blocks.Contains(firstPar))
      {
         if (!(firstPar.IsTableCellBlock || lastPar.IsTableCellBlock))
         {
            List<IEditable> moveInlines = [.. lastPar.Inlines];
            lastPar.Inlines.RemoveMany(moveInlines);
            lastPar.CallRequestInlinesUpdate();
            firstPar.Inlines.AddRange(moveInlines);
            firstPar.CallRequestInlinesUpdate(); // ensure any image containers are updated
            Blocks.Remove(lastPar);
         }
      }

      // re-add the first par if no blocks are left
      if (Blocks.Count == 0) 
         Blocks.Add(firstPar);

      //Special case with one remaining block with no inlines
      if (Blocks.Count == 1 && Blocks[0] is Paragraph onlyPar && onlyPar.Inlines.Count == 0)
         onlyPar.Inlines.Add(new EditableRun(""));


      disableRunTextUndo = false;

      UpdateBlockAndInlineStarts(firstParIndex);

      //if (firstParDeleted) // && adjustCursor)
      //{ // fix caret position if 1st paragraph was deleted
      //   Selection.Start = Math.Max(0, Selection.Start - 1);
      //   Selection.CollapseToStart();
      //}
            
      Selection.Start = Selection.Start;

      SelectionExtendMode = ExtendMode.ExtendModeNone;

      UpdateTextRanges(originalSelectionStart, -originalTRangeLength);
      UpdateSelection();


      _ = AsyncUpdateCaret(trange);

      return edgeIds;

   }

   internal async Task AsyncUpdateCaret(TextRange trange)
   {
      //await Task.Delay(100);
      trange.CollapseToStart();
      UpdateCaret();

   }

   internal void MergeParagraphForward(int mergeCharIndex, bool addUndo, int originalSelectionStart)
   {
      if (GetContainingParagraph(mergeCharIndex) is not Paragraph thisPar) return;
      
      int thisParIndex = Blocks.IndexOf(thisPar);
      if (thisParIndex == Blocks.Count - 1) return; //is last Paragraph, can't merge forward
      int origMergedParInlinesCount = thisPar.Inlines.Count;

      if (Blocks[thisParIndex + 1] is not Paragraph nextPar) return;
      
      bool IsNextParagraphEmpty = nextPar.Inlines.Count == 1 && nextPar.Inlines[0].IsEmpty;
      bool IsThisParagraphEmpty = thisPar.Inlines.Count == 1 && thisPar.Inlines[0].IsEmpty;

      if (IsThisParagraphEmpty)
      {
         thisPar.Inlines.Clear();
         origMergedParInlinesCount = 0;
      }

      if (addUndo)
         Undos.Add(new MergeParagraphUndo(origMergedParInlinesCount, thisPar.Id, nextPar.FullClone(), this, originalSelectionStart)); // cloned with Id and inlines

      //bool runAdded = false;
      if (IsNextParagraphEmpty)
      {
         if (IsThisParagraphEmpty)
         {
            thisPar.Inlines.Add(new EditableRun(""));
            //runAdded = true;
         }
      }
      else
      {
         List<IEditable> inlinesToMove = [.. nextPar.Inlines];
         nextPar.Inlines.Clear();
         nextPar.CallRequestInlinesUpdate(); // ensure image containers are updated
         thisPar.Inlines.AddRange(inlinesToMove);
      }
           
      Blocks.Remove(nextPar);

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

      UpdateTextRanges(mergeCharIndex, -1);

      thisPar.CallRequestInlinesUpdate();
      UpdateBlockAndInlineStarts(thisParIndex);

      thisPar.CallRequestTextBoxFocus();

      UpdateSelectedParagraphs();


   }
   
   internal void DeleteWord(bool backspace)
   {
      if (backspace)
         if (Selection.Start <= 0) return;
      else
         if (Selection.Start >= Selection.StartParagraph.StartInDoc + Selection.StartParagraph.BlockLength)
            return;

      
      int originalSelectionStart = Selection.Start;
      
      if (backspace)
         MoveLeftWord();
      
      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

      Paragraph startP = Selection.StartParagraph;

      if (startP.SelectionStartInBlock == startP.TextLength)
         MergeParagraphForward(Selection.Start, true, originalSelectionStart); //updates text ranges and adds undo
      else
      {
         int NextWordEndPoint = -1;
         if (Selection.GetStartInline() is IEditable startInline && (startInline.IsUIContainer || startInline.IsLineBreak))
            NextWordEndPoint = Selection.Start + 1;
         else
         {
            int IndexNextSpace = Selection.StartParagraph.Text.IndexOf(' ', Selection.Start - Selection.StartParagraph.StartInDoc);
            if (IndexNextSpace == -1)
               IndexNextSpace = Selection.StartParagraph.TextLength;
            else
               IndexNextSpace += 1;
            NextWordEndPoint = Selection.StartParagraph.StartInDoc + IndexNextSpace;
         }

         //if (startP.Inlines.Count > 1)
         //   startP.RemoveEmptyInlines();
         
         TextRange deleteTextRange = new (this, Selection.Start, NextWordEndPoint);
         DeleteRange(deleteTextRange, true, true);  // updates all text ranges and adds undo
                           
         UpdateBlockAndInlineStarts(AllParagraphs.IndexOf(startP));
      }

      SelectionStart_Changed(Selection, Selection.Start);
      Selection.StartParagraph.CallRequestInlinesUpdate();
      Selection.StartParagraph.CallRequestTextLayoutInfoStart();

   }


}