using Avalonia.Threading;
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
            bool keepInCell =
               (backspace && Selection.StartParagraph.SelectionStartInBlock == 0) ||
               (!backspace && Selection.StartParagraph.SelectionStartInBlock >= Selection.StartParagraph.BlockLength - 1);
            if (keepInCell) return;
        }

        if (backspace)
            MoveSelectionLeft();

        //Change bias to be forward for delete
        Selection.BiasForwardStart = true;
        Selection.BiasForwardEnd = true;
        Selection.UpdateContextStart();
        Selection.UpdateContextEnd();

        if (Selection.StartInline is not IEditable startInline) return;

        if (startInline is EditableHyperlink hyperlink && hyperlink.InlineLength < 2)
        {
            if (backspace)
                MoveSelectionRight();
            return;
        }


        Paragraph startP = Selection.StartParagraph;

        if (startP.SelectionStartInBlock == startP.BlockLength - 1)
            MergeParagraphForward(Selection.Start, true, originalSelectionStart);
        else
        {  //Delete one unit

            disableRunTextUndo = true;

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
                int idxStartInlineInPar = startP.Inlines.IndexOf(startInline);

                if (startInline.NextInline is EditableLineBreak lbreak && isSelectionAtInlineEnd)
                {  //Delete linebreak
                    ((Type t1, int id1), (Type t2, int id2)) types = new(new(typeof(EditableLineBreak), lbreak.Id), new());
                    int lbIndex = startP.Inlines.IndexOf(lbreak);
                    IEditable? lbnext = lbreak.NextInline;

                    if (lbnext != null && lbnext.IsEmpty)
                    {
                        startP.Inlines.Remove(lbnext);
                        types = new(new(typeof(EditableLineBreak), lbreak.Id), new(typeof(EditableRun), lbnext.Id));
                    }
                    else if (startInline.IsEmpty)
                    {
                        lbIndex = startP.Inlines.IndexOf(startInline);
                        startP.Inlines.Remove(startInline);
                        types = new(new(typeof(EditableRun), startInline.Id), new(typeof(EditableLineBreak), lbreak.Id));
                    }
                    startP.Inlines.Remove(lbreak);

                    Undos.Add(new DeleteLineBreakUndo(startP.Id, types, lbIndex, this, originalSelectionStart));

                }
                else
                {  // delete normal char
                    bool nextIsLineBreak = startInline.NextInline is EditableLineBreak;
                    bool prevIsLineBreak = startInline.PreviousInline is EditableLineBreak;
                    bool leaveEmptyRun =
                       (nextIsLineBreak && prevIsLineBreak) ||
                       (prevIsLineBreak && startInline.IsLastInlineOfParagraph) ||
                       (nextIsLineBreak && startInline.IsFirstInlineOfParagraph);



                    if (startInline.InlineLength == 1 && !leaveEmptyRun)  // keep empty run on linebreak
                    {  // just one char in the inline, so remove it entirely, unless 
                        if (startInline.CloneWithId() is EditableRun removedRunClone)
                        {
                            startP.Inlines.Remove(startInline);
                            Undos.Add(new DeleteRunUndo(startP.Id, removedRunClone, startInlineIdx, this, originalSelectionStart));
                        }
                    }
                    else
                    { // just remove char from inline
                        selectionStartInInline = GetCharPosInInline(startInline, Selection.Start);
                        char deletedChar = startInline.InlineText[selectionStartInInline];
                        if (selectionStartInInline < startInline.InlineLength)
                            startInline.InlineText = startInline.InlineText.Remove(selectionStartInInline, 1);   // undo handled by PropertyChanged: Text

                        Undos.Add(new DeleteCharUndo(startP.Id, startInline.Id, idxStartInlineInPar, deletedChar, selectionStartInInline, this, originalSelectionStart));
                    }

                    //Paragraph must always have at least an empty run
                    if (startP.Inlines.Count == 0)
                        startP.Inlines.Add(new EditableRun(""));

                }
            }

            disableRunTextUndo = false;

            UpdateSelection();
            UpdateTextRanges(Selection.Start, -1);
        }

        SelectionStart_Changed(Selection, Selection.Start);
        Selection.StartParagraph.CallRequestInlinesUpdate();
        Selection.StartParagraph.CallRequestTextLayoutInfoStart();


    }

    internal void DeleteSelection()
    {
        int lengthBefore = Text.Length;
        int originalSelStart = Selection.Start;
        DeleteRange(Selection, true, true);
        SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeNone;
        Selection.CollapseToStart();

        int lengthAfter = Text.Length;

        UpdateBlockAndInlineStarts(Selection.StartParagraph);

        RestoreCaretTo(originalSelStart);

        Selection.BiasForwardStart = false;
        Selection.BiasForwardEnd = false;

    }

    internal (int idLeft, int idRight) DeleteRange(TextRange trange, bool addUndo, bool adjustCaret)
    {
        bool docContainsOneBlock = Blocks.Count == 1;
        int originalRangeStart = trange.Start;
        int originalTRangeLength = trange.Length;
        int originalRangeEnd = trange.Start + trange.Length;

        List<Block> rangeBlocks = GetOverlappingBlocksInRange(trange);

        int firstBlockId = rangeBlocks.First().Id;
        int firstBlockIndex = Blocks.IndexOf(rangeBlocks.First());

        disableRunTextUndo = true;

        List<Block> blocksFullyInRange = GetFullBlocksInRange(trange);
        bool firstBlockDeleted = blocksFullyInRange.Count > 0 && blocksFullyInRange.First().StartInDoc == originalRangeStart;
        bool lastBlockDeleted = blocksFullyInRange.Count > 0 && blocksFullyInRange.Last().EndInDoc == originalRangeEnd;

        //Check if selection is at end of only inline in only paragraph
        if (rangeBlocks.Count == 1 && rangeBlocks[0] is Paragraph p && p.Inlines.Count == 1)
        {
            IEditable lastInline = p.Inlines.Last();
            if (GetCharPosInInline(lastInline, trange.Start) == lastInline.InlineLength)
                return (lastInline.Id, -1);
        }


        if (addUndo)
            Undos.Add(new DeleteRangeUndo(rangeBlocks.ConvertAll(rblock => rblock.FullClone()), firstBlockIndex, this, originalRangeStart, originalTRangeLength, firstBlockDeleted, lastBlockDeleted));

        //get the inlines in this range and split if necessary, adding newly created inlines to doc
        (List<IEditable> createdInlines, (int idLeft, int idRight) edgeIds) createdInlinesResult = GetTextRangeInlines(trange, addToDoc: true);

        List<IEditable> rangeInlines = createdInlinesResult.createdInlines;
        (int idLeft, int idRight) edgeIds = createdInlinesResult.edgeIds;

        List<Block> toRemoveBlocks = [];

        //Delete the range inlines
        foreach (IEditable toDeleteRun in rangeInlines)
        {
            if (AllParagraphs.FirstOrDefault(p => p.Id == toDeleteRun.MyParagraphId) is Paragraph rangePar)
            {
                rangePar.Inlines.Remove(toDeleteRun);
                if (rangePar.Inlines.Count == 0)
                {
                    if (rangePar.IsTableCellBlock)
                    {  // if overlapping cell blocks got deleted, they shouldn't be removed
                        rangePar.Inlines.Add(new EditableRun(""));
                    }
                    else
                        toRemoveBlocks.Add(rangePar);
                }

                rangePar.CallRequestInlinesUpdate();
                rangePar.CallRequestTextLayoutInfoStart();
                rangePar.CallRequestTextLayoutInfoEnd();
            }
        }

        //Delete any full blocks contained within the range
        foreach (Block fullyContainedBlock in blocksFullyInRange)
        {
            if (!fullyContainedBlock.IsTableCellBlock && !docContainsOneBlock)
            {
                if (fullyContainedBlock is Paragraph fullyContainedPar)
                    fullyContainedPar.Inlines.Clear();
                Blocks.Remove(fullyContainedBlock);
            }
        }

        Blocks.RemoveMany(toRemoveBlocks);


        if (rangeBlocks[0] is Paragraph firstPar)  // merging of first/last paragraphs if applicable
        {   
            if (rangeBlocks.Count == 1 && firstPar.Inlines.Count == 0)
                Blocks.Remove(firstPar);

            //Merge inlines of last paragraph with first if present and both are paragraphs
            if (rangeBlocks.Count > 1 && Blocks.Contains(firstPar))
            {
                if (rangeBlocks[^1] is Paragraph lastPar && !(firstPar.IsTableCellBlock || lastPar.IsTableCellBlock))
                {
                    List<IEditable> moveInlines = [.. lastPar.Inlines];
                    lastPar.Inlines.RemoveMany(moveInlines);
                    lastPar.CallRequestInlinesUpdate();
                    firstPar.Inlines.AddRange(moveInlines);
                    firstPar.CallRequestInlinesUpdate(); // ensure any image containers are updated
                    Blocks.Remove(lastPar);
                }
            }
        }


        // Fix special cases:
        // re-add the first par if no blocks are left
        if (Blocks.Count == 0)
            Blocks.Add(rangeBlocks[0]);
        //Special case with one remaining block with no inlines
        if (Blocks.Count == 1 && Blocks[0] is Paragraph onlyPar && onlyPar.Inlines.Count == 0)
            onlyPar.Inlines.Add(new EditableRun(""));


        disableRunTextUndo = false;

        UpdateTextRanges(originalRangeStart, -originalTRangeLength);


        return edgeIds;

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

        thisPar.CallRequestInlinesUpdate();

        UpdateBlockAndInlineStarts(thisParIndex);
        UpdateTextRanges(mergeCharIndex, -1);

        thisPar.CallRequestTextBoxFocus();

        UpdateSelectedParagraphs();


    }

    internal void DeleteWord(bool backspace)
    {
        if (backspace)
            if (Selection.Start <= 0) 
                return;
            else
            {
                if (Selection.Start >= Selection.StartParagraph.StartInDoc + Selection.StartParagraph.BlockLength)
                    return;
            }
                
        if (backspace)
            MoveLeftWord();

        int originalSelectionStart = Selection.Start;

        Selection.BiasForwardStart = true;
        Selection.BiasForwardEnd = true;

        Paragraph startP = Selection.StartParagraph;

        if (startP.SelectionStartInBlock == startP.BlockLength - 1)
            MergeParagraphForward(Selection.Start, true, originalSelectionStart); //updates text ranges and adds undo
        else
        {
            int NextWordEndPoint = -1;
            if (Selection.StartInline is IEditable startInline && (startInline.IsUIContainer || startInline.IsLineBreak))
                NextWordEndPoint = Selection.Start + 1;
            else
            {
                int IndexNextSpace = Selection.StartParagraph.Text.IndexOf(' ', Selection.Start - Selection.StartParagraph.StartInDoc);
                if (IndexNextSpace == -1)
                    IndexNextSpace = Selection.StartParagraph.BlockLength - 1;
                else
                    IndexNextSpace += 1;
                NextWordEndPoint = Selection.StartParagraph.StartInDoc + IndexNextSpace;
            }

            TextRange deleteTextRange = new(this, Selection.Start, NextWordEndPoint);
            DeleteRange(deleteTextRange, true, true);  // updates all text ranges, block/inline starts, and adds undo

        }

        Selection.StartParagraph.CallRequestInlinesUpdate();
        Selection.StartParagraph.CallRequestTextLayoutInfoStart();

        Dispatcher.UIThread.Post(() =>
        {
            Select(originalSelectionStart, 0);
            UpdateCaret();

        }, DispatcherPriority.Background);



    }


}