using AvRichTextBox;
using Avalonia.Threading;
using DynamicData;


internal class InsertParagraphUndo(
    FlowDocument flowDoc, 
    int origParId, 
    int insertedParId, 
    List<IEditable> keepParInlines, 
    int origSelectionStart, 
    int undoEditOffset, 
    bool IsCellParagraph, 
    int containingTableId, 
    int containingCellId) : IEditDo
{  //all original inlines preserved, so no need to worry about split inlines

    public int UndoEditOffset => undoEditOffset;
    public bool UpdateTextRanges => true;
    int updateBlockIdx = 0;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;

            updateBlockIdx = 0;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == insertedParId) is not Paragraph insertedPar) return;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == origParId) is not Paragraph origPar) return;
            origPar.Inlines.Clear();
            origPar.Inlines.AddRange(keepParInlines);

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

            PostUpdate(origPar, -undoEditOffset);
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Inserted par id: {insertedParId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            flowDoc.disableUndoStack = true;

            updateBlockIdx = 0;
            //if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == insertedParId) is not Paragraph insertedPar) return;
            //if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == origParId) is not Paragraph origPar) return;
            //origPar.Inlines.Clear();
            //origPar.Inlines.AddRange(keepParInlines);

            if (IsCellParagraph)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell containingCell) return;

                updateBlockIdx = flowDoc.Blocks.IndexOf(containingTable);
                //containingCell.CellBlocks.Insert(insertedPar);
            }
            else
            {
            //    updateBlockIdx = flowDoc.Blocks.IndexOf(origPar);
                //flowDoc.Blocks.Insert(insertedPar);
            }

            //PostUpdate(origPar, undoEditOffset);
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Redo Inserted par id: {insertedParId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    private void PostUpdate(Paragraph origPar, int offset)
    {
        flowDoc.disableUndoStack = false;

        flowDoc.UpdateBlockAndInlineStarts(updateBlockIdx);

        flowDoc.UpdateTextRanges(origSelectionStart, offset);

        Dispatcher.UIThread.Post(() =>
        {
            origPar.CallRequestInlinesUpdate();
            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
            flowDoc.InvokeSelectionChanged();
        });

    }
}

internal class AddParagraphUndo(FlowDocument flowDoc, int addedParId, int origSelectionStart, bool IsCellParagraph, int containingTableId, int containingCellId) : IEditDo
{  
    public int UndoEditOffset => 1;
    public bool UpdateTextRanges => true;

    public void PerformUndo()
    {
        try
        {
            int blockIdx = 0;

            if (IsCellParagraph)
            {
                if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == addedParId) is not Paragraph insertedPar) return;
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell containingCell) return;

                blockIdx = flowDoc.Blocks.IndexOf(containingTable);
                containingCell.CellBlocks.Remove(insertedPar);

            }
            else
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == addedParId) is not Paragraph insertedPar) return;
                blockIdx = flowDoc.Blocks.IndexOf(insertedPar);
                flowDoc.Blocks.Remove(insertedPar);
            }

            flowDoc.UpdateBlockAndInlineStarts(blockIdx);

            flowDoc.UpdateTextRanges(origSelectionStart, -1); // offset will always be -1

            Dispatcher.UIThread.Post(() =>
            {
                flowDoc.Selection.Start = origSelectionStart;
                flowDoc.Selection.End = flowDoc.Selection.Start;
            });

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Inserted par id: {addedParId}"); }

    }

    public void PerformRedo()
    {
    }
}


internal class MergeParagraphUndo(int origMergedParInlinesCount, int mergedParId, Paragraph removedParClone, FlowDocument flowDoc, int originalSelectionStart) : IEditDo
{ //removedPar is a clone

    public int UndoEditOffset => 1;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            int blockIdx = 0;
            int lengthBefore = flowDoc.Text.Length;
            Paragraph? mergedPar = null!;
            Cell? CellToRestore = null!;

            if (removedParClone.IsCellBlock)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == removedParClone.OwningTable.Id) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == removedParClone.OwningCell.Id) is not Cell containingCell) return;
                CellToRestore = containingCell;
                mergedPar = containingCell.CellBlocks.FirstOrDefault(cb => cb.Id == mergedParId) as Paragraph;
                if (mergedPar == null!) return;
                blockIdx = containingCell.CellBlocks.IndexOf(mergedPar);
            }
            else
            {
                mergedPar = flowDoc.Blocks.FirstOrDefault(bl => bl.Id == mergedParId) as Paragraph;
                if (mergedPar == null!) return;
                blockIdx = flowDoc.Blocks.IndexOf(mergedPar);
            }

            for (int rno = mergedPar.Inlines.Count - 1; rno >= origMergedParInlinesCount; rno--)
                mergedPar.Inlines.RemoveAt(rno);

            if (mergedPar.Inlines.Count == 0)
                mergedPar.Inlines.Add(new EditableRun(""));

            mergedPar.CallRequestInlinesUpdate();
            mergedPar.UpdateEditableRunPositions();

            if (mergedPar.IsCellBlock)
                CellToRestore.CellBlocks.Insert(blockIdx + 1, removedParClone);
            else
                flowDoc.Blocks.Insert(blockIdx + 1, removedParClone);

            int lengthAfter = flowDoc.Text.Length;

            flowDoc.UpdateBlockAndInlineStarts(blockIdx);
            flowDoc.UpdateTextRanges(originalSelectionStart, lengthAfter - lengthBefore);

            flowDoc.Selection.End = originalSelectionStart;
            flowDoc.Selection.Start = originalSelectionStart;

            flowDoc.InvokeSelectionChanged();

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at MergedPar: {mergedParId}"); }
    }

    public void PerformRedo()
    {
    }
}


