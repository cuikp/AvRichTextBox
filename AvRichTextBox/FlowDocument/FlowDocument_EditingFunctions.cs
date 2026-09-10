using DocumentFormat.OpenXml.Drawing.Charts;
using DynamicData;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace AvRichTextBox;

public partial class FlowDocument
{
    internal void SetRangeToText(TextRange tRange, string newText, bool copyFormatting = true)
    {  //The delete range and SetRangeToText should constitute one Undo operation

        Paragraph startPar = tRange.StartParagraph;
        int rangeStart = tRange.Start;
        int rangeEnd = tRange.End;
        int deleteRangeLength = tRange.Length;
        int parIndex = AllParagraphs.IndexOf(startPar);
        bool firstParEmpty = startPar.Inlines[0] is EditableRun erun && erun.Text == "";
        bool firstParWasDeleted = startPar.StartInDoc == rangeStart && startPar.EndInDoc <= rangeEnd && !firstParEmpty;

        //Delete any selected text first
        if (tRange.Length > 0)
        {
            DeleteRange(tRange, false, false);  // no undo, handled by PasteUndo
            tRange.CollapseToStart();
            SelectionExtendMode = ExtendMode.ExtendModeNone;
        }


        /////// can just call paste text here instead?:::::$$$$$$$$$$$$$$$$$

        if (tRange.StartInline is not IEditable startInline) return;

        List<IEditable> splitInlines = SplitRunAtPos(tRange.Start, startInline, GetCharPosInInline(startInline, tRange.Start));

        int startInlineIndex = startPar.Inlines.IndexOf(splitInlines[0]) + 1;

        if (splitInlines[0] is EditableRun sRun)
        {
            EditableRun newEditableRun = new(newText);

            if (copyFormatting)
            {
                newEditableRun.FontFamily = sRun.FontFamily;
                newEditableRun.FontWeight = sRun.FontWeight;
                newEditableRun.FontStyle = sRun.FontStyle;
                newEditableRun.FontSize = sRun.FontSize;
                newEditableRun.TextDecorations = sRun.TextDecorations;
                newEditableRun.Background = sRun.Background;
                newEditableRun.BaselineAlignment = sRun.BaselineAlignment;
                newEditableRun.Foreground = sRun.Foreground;
            }

            startPar.Inlines.Insert(startInlineIndex, newEditableRun);

            if (splitInlines[0].InlineText == "")
                startPar.Inlines.Remove(splitInlines[0]);
        }

        startPar.CallRequestInvalidateVisual();
        startPar.CallRequestTextLayoutInfoStart();
        startPar.CallRequestInlinesUpdate();

        UpdateBlockAndInlineStarts(startPar);


    }

    internal void Undo()
    {
        if (Undos.Count > 0)
        {
            disableRunTextUndo = true;
            disableUndoStack = true;

            IEditDo lastUndo = Undos.Last();

            lastUndo.PerformUndo();

            UpdateSelection();
            UpdateCaret();

            if (lastUndo.UpdateTextRanges)
                UpdateTextRanges(Selection.Start, lastUndo.UndoEditOffset);

            
            Undos.Remove(lastUndo);
            Redos.Add(lastUndo);

#if DEBUG
            //DebugPrintUndos();
#endif

            UpdateSelectedParagraphs();


            ScrollInDirection?.Invoke(1);
            ScrollInDirection?.Invoke(-1);

            disableRunTextUndo = false;
            disableUndoStack = false;
        }
    }

    private void DebugPrintUndos()
    {
        ////////////////////////////////////////////
        Debug.WriteLine("\nUndos count = " + Undos.Count + "\n" + string.Join("   ", Undos.ToList().ConvertAll(undo => undo.GetType().ToString())));
        Debug.WriteLine("Redos count = " + Redos.Count + "\n" + string.Join("   ", Redos.ToList().ConvertAll(redo => redo.GetType().ToString())));
        ///////////////////////////////////////////
    }

    internal void Redo()
    {
        if (Redos.Count > 0)
        {
            disableRunTextUndo = true;
            disableUndoStack = true;

            IEditDo lastRedo = Redos.Last();
            lastRedo.PerformRedo();

            UpdateSelection();
            UpdateCaret();

            if (lastRedo.UpdateTextRanges)
                UpdateTextRanges(Selection.Start, lastRedo.UndoEditOffset);

            Redos.Remove(lastRedo);
            Undos.Add(lastRedo);

#if DEBUG
            //DebugPrintUndos();
#endif    

            UpdateSelectedParagraphs();


            ScrollInDirection?.Invoke(1);
            ScrollInDirection?.Invoke(-1);

            disableRunTextUndo = false;
            disableUndoStack = false;
        }
    }

    internal void RestoreDeletedBlocks(
        List<Block> blockClones, 
        int blockIndex, 
        bool firstBlockWasDeleted, 
        bool lastBlockWasDeleted, 
        ObservableCollection<Block> blockCollection, 
        int updateBlocksFromIndex)
    {
        bool tablePartiallyDeleted = (blockClones[0] is Table && blockClones[^1] is not Table) || (blockClones[0] is not Table && blockClones[^1] is Table);
        bool startsWithIUC = blockClones.FirstOrDefault() is Paragraph firstPar && firstPar.Inlines.Count == 1 && firstPar.Inlines.FirstOrDefault() is EditableInlineUIContainer;
        bool endsWithIUC = blockClones.LastOrDefault() is Paragraph lastPar && lastPar.Inlines.Count == 1 && lastPar.Inlines.FirstOrDefault() is EditableInlineUIContainer;

        if (startsWithIUC || endsWithIUC)
        {
            //do nothing for single eIUC in paragraph
        }
        else if (lastBlockWasDeleted && firstBlockWasDeleted && this.IsEmpty)
        {
            blockCollection.RemoveAt(blockIndex);
        }
        else if (!lastBlockWasDeleted)
        {
            blockCollection.RemoveAt(blockIndex);
            // Special case if table contents were partially deleted, leaving the old table
            if (!firstBlockWasDeleted && tablePartiallyDeleted)
                blockCollection.RemoveAt(blockIndex);
        }
        else if (!firstBlockWasDeleted)
            blockCollection.RemoveAt(blockIndex);

        //Restore all of the previous paragraphs
        blockCollection.AddOrInsertRange(blockClones, blockIndex);

        //Debug.WriteLine("restoring blocksToInsert = " + blockClones.Count + ", " + blockClones[0].GetType().ToString());

        foreach (Paragraph p in FlattenParagraphs(blockClones))
            p.CallRequestInlinesUpdate();

                
        UpdateBlockAndInlineStarts(updateBlocksFromIndex);


    }

    private int ProcessInsertBlocks(List<Block> blocksToInsert, Paragraph destinationStartPar, int insertIdx, int insertBlockIndex, List<int> addedBlockIds, List<IEditable> rightSplitRuns)
    {
        int pastedTextLength = 0;
        int blockno = 0;
        int currentInsertIdx = insertBlockIndex;
        destinationStartPar.Inlines.RemoveMany(rightSplitRuns);  
        Paragraph addPar = destinationStartPar;

        foreach (Block block in blocksToInsert)
        {
            if (block is Paragraph thisPar)
            {
                bool paragraphCreated = false;

                switch (blockno)
                {
                    case 0:

                        //Remove single empty run if present
                        if (addPar.IsEmptyInlinePar)
                        {
                            addPar.Inlines.RemoveAt(0);
                            insertIdx = 0;
                        }

                        // insert first paragraph into existing paragraph
                        addPar.Inlines.AddOrInsertRange(thisPar.Inlines, insertIdx);
                        break;

                    default:
                        // create new paragraphs for pars 1 onward
                        addPar = thisPar;
                        addPar.CopyPropertiesFromParagraph(destinationStartPar);
                        pastedTextLength += 1;
                        paragraphCreated = true;
                        break;
                }

                pastedTextLength += (thisPar.TextLength - 1); // remove extra length for par CR


                if (paragraphCreated)
                {
                    currentInsertIdx += 1;
                    //Blocks.Insert(currentInsertIdx, addParthat
                    if (destinationStartPar.IsCellBlock)
                        destinationStartPar.OwningCell.CellBlocks.Insert(currentInsertIdx, addPar);
                    else
                        Blocks.Insert(currentInsertIdx, addPar);

                    addedBlockIds.Add(addPar.Id);
                }
                
            }
            else
            { // non-Paragraph block always pastes as new block
                currentInsertIdx += 1;
                Blocks.Insert(currentInsertIdx, block);
                addedBlockIds.Add(block.Id);
                pastedTextLength += block.TextLength;
            }

            blockno++;
        }

        // final fixes
        if (blocksToInsert.Count == 0)
            addPar.Inlines.Add(new EditableRun(""));

        //attach right-split inlines to last pasted paragraph
        if (rightSplitRuns.Count > 0 && !rightSplitRuns[0].IsEmpty)
            addPar.Inlines.AddRange(rightSplitRuns);
            

        return pastedTextLength;
    }

    internal void InsertBlockIntoCollectionAt(ObservableCollection<Block> blockCollection, int insertIdx, Block blockToInsert)
    {
        if (insertIdx < 0 || insertIdx > blockCollection.Count)
            throw new Exception("Block index is out of bounds of the block collection.");

        this.disableUndoStack = true;

        blockCollection.Insert(insertIdx, blockToInsert);
        blockToInsert.IsAttachedToDocument = true;

        int tableId = blockToInsert.IsCellBlock ? blockToInsert.OwningTable.Id : -1;
        int cellId = blockToInsert.IsCellBlock ? blockToInsert.OwningCell.Id : -1;
        int updateIdx = blockToInsert.IsCellBlock ? Blocks.IndexOf(blockToInsert.OwningTable) : Blocks.IndexOf(blockToInsert);

        bool addUndo = !blockToInsert.IsCellBlock || blockToInsert.OwningTable.IsAttachedToDocument;

        if (addUndo)
            Undos.Add(new InsertBlockUndo(this, blockToInsert.Id, blockToInsert.BlockLength, blockToInsert.IsCellBlock, tableId, cellId));
        
        this.disableUndoStack = false;
    }
    
    internal void RemoveBlockFromCollectionAt(ObservableCollection<Block> blockCollection, int removeAtIndex)
    {
        if (removeAtIndex < 0 || removeAtIndex >= blockCollection.Count)
            throw new Exception("Block index is out of bounds of the block collection.");
        
        if (blockCollection.Count == 1 && blockCollection[0].Text == "")
            throw new Exception("Cannot remove default empty paragraph in the collection.");

        Block blockToRemove = blockCollection[removeAtIndex];
        RemoveBlockFromCollection(blockCollection, blockToRemove);

        if (blockCollection.Count == 0)
            AddDefaultParagraph(blockCollection);
    }

    internal void RemoveBlockFromCollection(ObservableCollection<Block> blockCollection, Block? blockToRemove)
    {
        if (blockToRemove == null) 
            throw new Exception("Block to remove must not be null.");
        if (!blockCollection.Contains(blockToRemove)) return;

        this.disableUndoStack = true;

        int tableId = blockToRemove.IsCellBlock ? blockToRemove.OwningTable.Id : -1;
        int cellId = blockToRemove.IsCellBlock ? blockToRemove.OwningCell.Id : -1;
        int removeAtIdx = blockCollection.IndexOf(blockToRemove);
        Block removedBlockClone = blockToRemove.FullClone(true);

        blockCollection.Remove(blockToRemove);
        
        bool addUndo = !blockToRemove.IsCellBlock || blockToRemove.OwningTable.IsAttachedToDocument;
        
        if (addUndo)
            Undos.Add(new RemoveBlockUndo(this, removeAtIdx, removedBlockClone, blockToRemove.BlockLength, blockToRemove.IsCellBlock, tableId, cellId));

        this.disableUndoStack = false;

    }


}