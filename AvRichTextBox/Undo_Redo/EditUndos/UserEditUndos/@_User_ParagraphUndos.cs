using Avalonia.Threading;
using AvRichTextBox;
using DynamicData;

internal class InsertParagraphUndo(
    FlowDocument flowDoc, 
    int origParId, 
    int insertedParId, 
    List<IEditable> keepParInlineClones, 
    int origSelectionStart, 
    int undoEditOffset, 
    bool IsCellParagraph, 
    int containingTableId, 
    int containingCellId,
    bool doNextUndo) : IEditDo
{  //all original inlines preserved, so no need to worry about split inlines

    public int EditOffset { get; set; } = 0;
    public bool UpdateTextRanges => true;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => doNextUndo;
    public bool DoNextRedo => false;

    int updateBlockIdx = 0;
    Paragraph splitPar1Clone = null!;
    Paragraph splitPar2Clone = null!;

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack =  true;
            UpdateTextRangesFromCharIdx = origSelectionStart;

            updateBlockIdx = 0;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == insertedParId) is not Paragraph insertedPar) return;
            splitPar1Clone = insertedPar.FullClone(true);
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == origParId) is not Paragraph origPar) return;
            splitPar2Clone = origPar.FullClone(true);

            origPar.Inlines.Clear();
            origPar.Inlines.AddRange(keepParInlineClones);

            if (IsCellParagraph)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell containingCell) return;

                updateBlockIdx = flowDoc.Blocks.IndexOf(containingTable);
                containingCell.CellBlocks.Remove(insertedPar);
            }
            else
            {
                updateBlockIdx = flowDoc.Blocks.IndexOf(origPar);
                flowDoc.Blocks.Remove(insertedPar);
            }

            EditOffset = undoEditOffset;
            PostUpdate(origPar, null!, origSelectionStart);
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Inserted par id: {insertedParId}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack =  true;

            updateBlockIdx = 0;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == origParId) is not Paragraph origPar) return;

            if (IsCellParagraph)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell containingCell) return;
                updateBlockIdx = flowDoc.Blocks.IndexOf(containingTable);
                int insertIdx = containingCell.CellBlocks.IndexOf(origPar);
                containingCell.CellBlocks.Remove(origPar);
                containingCell.CellBlocks.Insert(insertIdx, splitPar1Clone);
                containingCell.CellBlocks.Insert(insertIdx, splitPar2Clone);
            }
            else
            {
                updateBlockIdx = flowDoc.Blocks.IndexOf(origPar);
                int insertIdx = flowDoc.Blocks.IndexOf(origPar);
                flowDoc.Blocks.Remove(origPar);
                flowDoc.Blocks.Insert(insertIdx, splitPar1Clone);
                flowDoc.Blocks.Insert(insertIdx, splitPar2Clone);
            }

            EditOffset = -undoEditOffset;
            PostUpdate(splitPar1Clone, splitPar2Clone, origSelectionStart + 1);
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Redo Inserted par id: {insertedParId}"); }
        finally { DisableUndoStack =  false; }
    }

    private void PostUpdate(Paragraph updatePar1, Paragraph updatePar2, int restoreSelectionTo)
    {
        DisableUndoStack =  false;

        flowDoc.UpdateBlockAndInlineStarts(updateBlockIdx);

        Dispatcher.UIThread.Post(() =>
        {
            updatePar1?.CallRequestInlinesUpdate();
            updatePar2?.CallRequestInlinesUpdate();
            flowDoc.Selection.Start = restoreSelectionTo;
            flowDoc.Selection.End = flowDoc.Selection.Start;
            flowDoc.InvokeSelectionChanged();
        });

    }
}

internal class AddParagraphUndo(
    FlowDocument flowDoc, 
    int addedParId, 
    int origSelectionStart, 
    bool IsCellParagraph, 
    int containingTableId, 
    int containingCellId, 
    int editOffset, 
    bool doNextUndo) : IEditDo

{  
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => true;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => doNextUndo;
    public bool DoNextRedo => false;

    Paragraph addedParagraph = null!;
    int addedParagraphIndex = -1;

    public void PerformUndo()
    {
        try
        {
            UpdateTextRangesFromCharIdx = origSelectionStart;

            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == addedParId) is not Paragraph insertedPar) return;
            //addedParagraphClone = insertedPar.FullClone(true);
            addedParagraph = insertedPar;

            if (IsCellParagraph)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell containingCell) return;

                addedParagraphIndex = flowDoc.Blocks.IndexOf(containingTable);
                containingCell.CellBlocks.Remove(insertedPar);
            }
            else
            {
                addedParagraphIndex = flowDoc.Blocks.IndexOf(insertedPar);
                flowDoc.Blocks.Remove(insertedPar);
            }

            EditOffset = editOffset;
            PostUpdate(addedParagraphIndex, origSelectionStart);

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Inserted par id: {addedParId}"); }

    }

    public void PerformRedo()
    {
        try
        {
            int blockIdx = 0;

            if (IsCellParagraph)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell containingCell) return;
                containingCell.CellBlocks.Insert(addedParagraphIndex, addedParagraph);
            }
            else
            {
                flowDoc.Blocks.Insert(addedParagraphIndex, addedParagraph);
            }

            EditOffset = -editOffset;
            PostUpdate(blockIdx, origSelectionStart + 1);

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Inserted par id: {addedParId}"); }

    }

    private void PostUpdate(int updateFromBlockIndex, int newSelStart)
    {
        flowDoc.UpdateBlockAndInlineStarts(updateFromBlockIndex);
        
        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.Selection.Start = newSelStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
        });
    }
}


internal class MergeParagraphUndo(int origMergedParInlinesCount, int mergedParId, Paragraph removedParClone, FlowDocument flowDoc, int originalSelectionStart) : IEditDo
{ 
    public int EditOffset { get; set; } =  -1;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    int lengthBefore = 0;
    
    Paragraph keepMergedPar = null!;
    int keepMergedParIndex = -1;
    bool addedEmptyRun = false;
    readonly List<IEditable> keepRemovedInlines = [];

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == mergedParId) is not Paragraph mergedPar) return;
            keepMergedPar = mergedPar;

            lengthBefore = flowDoc.Text.Length;
            Cell ContainingCell = null!;

            if (removedParClone.IsCellBlock)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == removedParClone.OwningTable?.Id) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == removedParClone.OwningCell?.Id) is not Cell containingCell) return;
                keepMergedParIndex = containingCell.CellBlocks.IndexOf(mergedPar);
                ContainingCell = containingCell;
            }
            else
                keepMergedParIndex = flowDoc.Blocks.IndexOf(mergedPar);

            keepRemovedInlines.Clear();
            for (int rno = keepMergedPar.Inlines.Count - 1; rno >= origMergedParInlinesCount; rno--)
            {
                keepRemovedInlines.Insert(0, keepMergedPar.Inlines[rno]);
                keepMergedPar.Inlines.RemoveAt(rno);
            }

            if (keepMergedPar.Inlines.Count == 0)
            {
                addedEmptyRun = true;
                keepMergedPar.Inlines.Add(new EditableRun(""));
            }
            
            keepMergedPar.CallRequestInlinesUpdate();
            keepMergedPar.UpdateEditableRunPositions();

            if (keepMergedPar.IsCellBlock)
                ContainingCell.CellBlocks.Insert(keepMergedParIndex + 1, removedParClone);
            else
                flowDoc.Blocks.Insert(keepMergedParIndex + 1, removedParClone);


            PostUpdate(keepMergedParIndex);

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at MergedPar: {mergedParId}"); }
        finally{ DisableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack = true;

            lengthBefore = flowDoc.Text.Length;

            if (addedEmptyRun)
               keepMergedPar.Inlines.RemoveAt(0);

            keepMergedPar.Inlines.AddRange(keepRemovedInlines);

            if (removedParClone.IsCellBlock)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == removedParClone.OwningTable?.Id) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == removedParClone.OwningCell?.Id) is not Cell containingCell) return;
                containingCell.CellBlocks.RemoveAt(keepMergedParIndex + 1);
            }
            else
                flowDoc.Blocks.RemoveAt(keepMergedParIndex + 1);

            keepMergedPar.CallRequestInlinesUpdate();
            keepMergedPar.UpdateEditableRunPositions();

            
            PostUpdate(keepMergedParIndex);

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at MergedPar: {mergedParId}"); }
        finally { DisableUndoStack = false; }
    }

    private void PostUpdate(int updateFromBlockIdx)
    {

        EditOffset = -EditOffset;

        flowDoc.UpdateBlockAndInlineStarts(updateFromBlockIdx);
        flowDoc.UpdateTextRanges(originalSelectionStart, flowDoc.Text.Length - lengthBefore);

        flowDoc.Selection.End = originalSelectionStart;
        flowDoc.Selection.Start = originalSelectionStart;

        flowDoc.Selection.BiasForwardStart = true;
        flowDoc.Selection.BiasForwardEnd = true;

        flowDoc.InvokeSelectionChanged();

        DisableUndoStack = false;

    }
}


