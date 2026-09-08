using Avalonia.Threading;

namespace AvRichTextBox; 


internal class DeleteCharUndo(int parId, int runId, int origRunIdx, char deleteChar, int deletePos, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int UndoEditOffset => 1;
    public bool UpdateTextRanges => true;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            flowDoc.disableRunTextUndo = true;

            if (thisPar.Inlines.FirstOrDefault(r => r.Id == runId) is EditableRun thisRun)
                thisRun.Text = thisRun.Text!.Insert(deletePos, $"{deleteChar}");
            else
            {  // run not found, it must have been deleted
                EditableRun restoreRun = new($"{deleteChar}") { Id = runId };
                thisPar.Inlines.Insert(origRunIdx, restoreRun);
            }

            flowDoc.disableRunTextUndo = false;

            thisPar.CallRequestInlinesUpdate();

            flowDoc.UpdateBlockAndInlineStarts(thisPar);

            Dispatcher.UIThread.Post(() =>
            {
                flowDoc.Selection.Start = origSelectionStart;
                flowDoc.Selection.End = flowDoc.Selection.Start;
            });

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at delete pos: {deletePos}"); }
    }

    public void PerformRedo()
    {
    }

}


internal class DeleteImageUndo(int parId, IEditable deletedIUC, int deletedInlineIdx, FlowDocument flowDoc, int origSelectionStart, bool emptyRunAdded) : IEditDo
{
    public int UndoEditOffset => 1;
    public bool UpdateTextRanges => true;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
            if (emptyRunAdded)
                thisPar.Inlines.RemoveAt(deletedInlineIdx);
            thisPar.Inlines.Insert(deletedInlineIdx, deletedIUC);
            thisPar.CallRequestInlinesUpdate();
            flowDoc.UpdateBlockAndInlineStarts(thisPar);
            flowDoc.UpdateTextRanges(thisPar.StartInDoc, 1);

            Dispatcher.UIThread.Post(() =>
            {
                flowDoc.Selection.Start = origSelectionStart;
                flowDoc.Selection.End = flowDoc.Selection.Start;
            });
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at delete pos: {origSelectionStart}"); }
    }

    public void PerformRedo()
    {
    }

}

internal class DeleteRunUndo(int parId, EditableRun removedRunClone, int deletedRunIdx, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int UndoEditOffset => 1;
    public bool UpdateTextRanges => true;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            int thisParLengthBefore = thisPar.TextLength;

            thisPar.Inlines.Insert(deletedRunIdx, removedRunClone);

            int thisParLengthAfter = thisPar.TextLength;

            thisPar.CallRequestInlinesUpdate();
            flowDoc.UpdateBlockAndInlineStarts(thisPar);
            flowDoc.UpdateTextRanges(thisPar.StartInDoc, thisParLengthAfter - thisParLengthBefore);

            Dispatcher.UIThread.Post(() =>
            {
                flowDoc.Selection.Start = origSelectionStart;
                flowDoc.Selection.End = flowDoc.Selection.Start;
            });

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at delete pos: {origSelectionStart}"); }
    }

    public void PerformRedo()
    {
    }

}


internal class DeleteLineBreakUndo(int parId, ((Type t1, int id1), (Type t2, int id2)) types, int lineBreakIdx, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int UndoEditOffset => -1;
    public bool UpdateTextRanges => true;

    public void PerformUndo()
    {
        if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
        int thisParLengthBefore = thisPar.TextLength;

        flowDoc.disableRunTextUndo = true;

        IEditable addIED1 = null!;
        if (types.Item1.t1 == typeof(EditableRun))
            addIED1 = new EditableRun("");
        else
            addIED1 = new EditableLineBreak();
        addIED1.Id = types.Item1.id1;

        thisPar.Inlines.Insert(lineBreakIdx, addIED1);


        if (types.Item2.t2 != null)
        {
            IEditable addIED2 = null!;
            if (types.Item2.t2 == typeof(EditableRun))
                addIED2 = new EditableRun("");
            else
                addIED2 = new EditableLineBreak();
            addIED2.Id = types.Item2.id2;
            thisPar.Inlines.Insert(lineBreakIdx + 1, addIED2);
        }

        int thisParLengthAfter = thisPar.TextLength;

        thisPar.CallRequestInlinesUpdate();
        flowDoc.UpdateBlockAndInlineStarts(thisPar);
        flowDoc.UpdateTextRanges(thisPar.StartInDoc, thisParLengthAfter - thisParLengthBefore);

        flowDoc.Selection.Start = origSelectionStart;
        flowDoc.Selection.End = flowDoc.Selection.Start;

        flowDoc.disableRunTextUndo = false;

    }

    public void PerformRedo()
    {
    }

}



internal class DeleteRangeUndo(
   List<Block> keptBlockClones,
   int startBlockIndex,
   FlowDocument flowDoc,
   int origSelectionStart,
   int undoEditOffset,
   bool firstBlockWasDeleted,
   bool lastBlockWasDeleted
   ) : IEditDo

{  //parInlines are cloned inlines

    public int UndoEditOffset => undoEditOffset;
    public bool UpdateTextRanges => true;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableRunTextUndo = true;
            flowDoc.disableUndoStack = true;
            int lengthBefore = flowDoc.Text.Length;  // optimize by getting flowDoc.Blocks.Last().StartInDoc + lastPar.BlockLength instead of calculating entire flowdoc text length

            //Cell? containingCell = null;
            //int updateBlocksFromIndex = startBlockIndex;
            //ObservableCollection<Block> blockCollection = flowDoc.Blocks;
            //if (pastedInCellBlock)
            //{
            //    if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
            //    if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell cell) return;
            //    containingCell = cell;
            //    updateBlocksFromIndex = flowDoc.Blocks.IndexOf(containingTable);
            //    blockCollection = containingCell.CellBlocks;
            //}

            flowDoc.RestoreDeletedBlocks(keptBlockClones, startBlockIndex, firstBlockWasDeleted, lastBlockWasDeleted, flowDoc.Blocks, startBlockIndex);

            flowDoc.disableRunTextUndo = false;
            flowDoc.disableUndoStack = false;

            int lengthAfter = flowDoc.Text.Length;  // optimize by getting from lastPar.StartInDoc + lastPar.BlockLength instead of calculating entire flowdoc text length
            flowDoc.UpdateTextRanges(keptBlockClones[0].StartInDoc, lengthAfter - lengthBefore);

            foreach (Table t in keptBlockClones.OfType<Table>())
                t.UpdateColAndRowPoints();

            Dispatcher.UIThread.Post(() =>
            {
                flowDoc.Selection.Start = origSelectionStart;
                flowDoc.Selection.End = origSelectionStart;
                //flowDoc.UpdateSelection();
            });


        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at Par index: {startBlockIndex}\n{ex.Message}"); }
    }

    public void PerformRedo()
    {
    }

}




