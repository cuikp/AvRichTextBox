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

            flowDoc.disableUndoStack = true;
            flowDoc.disableRunTextUndo = true;

            if (thisPar.Inlines.FirstOrDefault(r => r.Id == runId) is EditableRun thisRun)
                thisRun.Text = thisRun.Text!.Insert(deletePos, $"{deleteChar}");
            else
            {  // run not found, it must have been deleted
                EditableRun restoreRun = new($"{deleteChar}") { Id = runId };
                thisPar.Inlines.Insert(origRunIdx, restoreRun);
            }

            flowDoc.disableRunTextUndo = false;
            flowDoc.disableUndoStack = false;

            PostUpdate(thisPar);

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at delete pos: {deletePos}"); }
        finally { flowDoc.disableUndoStack = false;}
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            flowDoc.disableRunTextUndo = true;
            flowDoc.disableUndoStack = true;

            if (thisPar.Inlines.FirstOrDefault(r => r.Id == runId) is EditableRun thisRun)
            {
                thisRun.Text = thisRun.Text!.Remove(deletePos, 1);
                if (thisRun.IsEmpty)
                    thisPar.Inlines.Remove(thisRun);
            }

            flowDoc.disableRunTextUndo = false;
            flowDoc.disableUndoStack = false;

            PostUpdate(thisPar);

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at restore pos: {deletePos}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    private void PostUpdate(Paragraph thisPar)
    {
        thisPar.CallRequestInlinesUpdate();

        flowDoc.UpdateBlockAndInlineStarts(thisPar);

        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
        });
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
            flowDoc.disableUndoStack = true;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
            if (emptyRunAdded)
                thisPar.Inlines.RemoveAt(deletedInlineIdx);
            thisPar.Inlines.Insert(deletedInlineIdx, deletedIUC);

            PostUpdate(thisPar);

            flowDoc.disableUndoStack = false;

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at delete pos: {origSelectionStart}"); }
        finally { flowDoc.disableUndoStack = false; }

    }

    public void PerformRedo()
    {
        try
        {
            flowDoc.disableUndoStack = true;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            thisPar.Inlines.Remove(deletedIUC);
            if (thisPar.Inlines.Count == 0)
            {
                thisPar.Inlines.Insert(0, new EditableRun(""));
                emptyRunAdded = true;
            }

            PostUpdate(thisPar);
            flowDoc.disableUndoStack = false;
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at restore pos: {origSelectionStart}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    private void PostUpdate(Paragraph thisPar)
    {
        thisPar.CallRequestInlinesUpdate();
        flowDoc.UpdateBlockAndInlineStarts(thisPar);
        flowDoc.UpdateTextRanges(thisPar.StartInDoc, 1);

        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
        });
    }
}

internal class DeleteRunUndo(int parId, EditableRun removedRunClone, int deletedRunIdx, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int UndoEditOffset => 1;
    public bool UpdateTextRanges => true;

    int textChangeLen = 0;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            int thisParLengthBefore = thisPar.TextLength;

            thisPar.Inlines.Insert(deletedRunIdx, removedRunClone);

            int thisParLengthAfter = thisPar.TextLength;
            textChangeLen = thisParLengthAfter - thisParLengthBefore;

            PostUpdate(thisPar);

            flowDoc.disableUndoStack = false;
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at delete pos: {origSelectionStart}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            flowDoc.disableUndoStack = true;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            int thisParLengthBefore = thisPar.TextLength;

            thisPar.Inlines.Remove(removedRunClone);

            int thisParLengthAfter = thisPar.TextLength;
            textChangeLen = thisParLengthAfter - thisParLengthBefore;

            PostUpdate(thisPar);

            flowDoc.disableUndoStack = false;
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at delete pos: {origSelectionStart}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    private void PostUpdate(Paragraph thisPar)
    {
        thisPar.CallRequestInlinesUpdate();
        flowDoc.UpdateBlockAndInlineStarts(thisPar);
        flowDoc.UpdateTextRanges(thisPar.StartInDoc, textChangeLen);

        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
        });
    }
}


internal class DeleteLineBreakUndo(int parId, ((Type t1, int id1), (Type t2, int id2)) types, int lineBreakIdx, FlowDocument flowDoc, int origSelectionStart, bool deleteNext, bool startLineIsEmpty) : IEditDo
{
    public int UndoEditOffset => -1;
    public bool UpdateTextRanges => true;
    int textChangeLen = 0;

    public void PerformUndo()
    {
        if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
        int thisParLengthBefore = thisPar.TextLength;

        flowDoc.disableRunTextUndo = true;
        flowDoc.disableUndoStack = true;

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

        textChangeLen = thisPar.TextLength - thisParLengthBefore;

        PostUpdate(thisPar);

        flowDoc.disableRunTextUndo = false;
        flowDoc.disableUndoStack = false;
    }

    public void PerformRedo()
    {

        if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
        int thisParLengthBefore = thisPar.TextLength;

        flowDoc.disableRunTextUndo = true;
        flowDoc.disableUndoStack = true;
                
        if (deleteNext)
        {
            IEditable nextIEd = null!;
            if (types.Item2.t2 == typeof(EditableRun))
                nextIEd = new EditableRun("");
            else
                nextIEd = new EditableLineBreak();
            nextIEd.Id = types.Item2.id2;
            thisPar.Inlines.Remove(nextIEd);
        }
        else if (startLineIsEmpty)
            thisPar.Inlines.RemoveAt(lineBreakIdx);

        IEditable addLineBreak = new EditableLineBreak() { Id = types.Item1.id1 };
        thisPar.Inlines.Remove(addLineBreak);

        textChangeLen = thisPar.TextLength - thisParLengthBefore;

        PostUpdate(thisPar);

        flowDoc.disableRunTextUndo = false;
        flowDoc.disableUndoStack = false;
    }

    private void PostUpdate(Paragraph thisPar)
    {
       
        thisPar.CallRequestInlinesUpdate();
        flowDoc.UpdateBlockAndInlineStarts(thisPar);
        flowDoc.UpdateTextRanges(thisPar.StartInDoc, textChangeLen);

        flowDoc.Selection.Start = origSelectionStart;
        flowDoc.Selection.End = flowDoc.Selection.Start;

        
    }

}


internal class DeleteRangeUndo(
   List<Block> keptBlockClones,
   int startBlockIndex,
   FlowDocument flowDoc,
   int origSelectionStart,
   int originalRangeEnd,
   int undoEditOffset,
   bool firstBlockWasDeleted,
   bool lastBlockWasDeleted
   ) : IEditDo

{  //parInlines are cloned inlines

    public int UndoEditOffset => undoEditOffset;
    public bool UpdateTextRanges => true;
    private int changedTextLength = 0;
    int lengthBefore = 0;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableRunTextUndo = true;
            flowDoc.disableUndoStack = true;
            lengthBefore = flowDoc.Text.Length;  // optimize by getting flowDoc.Blocks.Last().StartInDoc + lastPar.BlockLength instead of calculating entire flowdoc text length

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

        
            PostUpdate();

        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at Par index: {startBlockIndex}\n{ex.Message}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        flowDoc.disableRunTextUndo = true;
        flowDoc.disableUndoStack = true;
        lengthBefore = flowDoc.Text.Length;  // optimize by getting flowDoc.Blocks.Last().StartInDoc + lastPar.BlockLength instead of calculating entire flowdoc text length

        keptBlockClones = keptBlockClones.ConvertAll(kpc => kpc.FullClone(true));

        TextRange origRange = new (flowDoc, origSelectionStart, originalRangeEnd);
        
        flowDoc.DeleteRange(origRange, false, true);
                
        flowDoc.UpdateBlockAndInlineStarts(startBlockIndex);
        
        flowDoc.TextRanges.Remove(origRange);

                
        PostUpdate();
    }

    private void PostUpdate()
    {
        flowDoc.disableRunTextUndo = false;
        flowDoc.disableUndoStack = false;


        changedTextLength = flowDoc.Text.Length - lengthBefore; // optimize by getting from lastPar.StartInDoc + lastPar.BlockLength instead of calculating entire flowdoc text length

        flowDoc.UpdateTextRanges(keptBlockClones[0].StartInDoc, changedTextLength);

        foreach (Table t in keptBlockClones.OfType<Table>())
            t.UpdateColAndRowPoints();

        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
        });

    }

}




