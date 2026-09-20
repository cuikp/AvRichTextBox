using Avalonia.Threading;

namespace AvRichTextBox; 

internal class DeleteCharUndo(int parId, int runId, int origRunIdx, char deleteChar, int deletePos, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => true;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    int RedoSelectionStart => origSelectionStart - 1;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            DisableUndoStack =  true;

            if (thisPar.Inlines.FirstOrDefault(r => r.Id == runId) is EditableRun thisRun)
                thisRun.Text = thisRun.Text!.Insert(deletePos, $"{deleteChar}");
            else
            {  // run not found, it must have been deleted
                EditableRun restoreRun = new($"{deleteChar}") { Id = runId };
                thisPar.Inlines.Insert(origRunIdx, restoreRun);
            }

            EditOffset = 1;
            PostUpdate(thisPar, origSelectionStart);
                        

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at delete pos: {deletePos}"); }
        finally { DisableUndoStack =  false;}
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            DisableUndoStack =  true;

            if (thisPar.Inlines.FirstOrDefault(r => r.Id == runId) is EditableRun thisRun)
            {
                thisRun.Text = thisRun.Text!.Remove(deletePos, 1);
                if (thisRun.IsEmpty)
                    thisPar.Inlines.Remove(thisRun);
            }

            EditOffset = -1;
            PostUpdate(thisPar, RedoSelectionStart + 1);
            

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at restore pos: {deletePos}"); }
        finally { DisableUndoStack =  false; }
    }

    private void PostUpdate(Paragraph thisPar, int selStart)
    {
        DisableUndoStack = false;

        thisPar.CallRequestInlinesUpdate();

        flowDoc.UpdateBlockAndInlineStarts(thisPar);

        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.Selection.Start = selStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
            
        });
    }

}


internal class DeleteImageUndo(int parId, IEditable deletedIUC, int deletedInlineIdx, FlowDocument flowDoc, int origSelectionStart, bool emptyRunAdded) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => true;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack =  true;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
            if (emptyRunAdded)
                thisPar.Inlines.RemoveAt(deletedInlineIdx);
            thisPar.Inlines.Insert(deletedInlineIdx, deletedIUC);

            EditOffset = 1;
     
            PostUpdate(thisPar);

            DisableUndoStack =  false;

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at delete pos: {origSelectionStart}"); }
        finally { DisableUndoStack =  false; }

    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack =  true;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            thisPar.Inlines.Remove(deletedIUC);
            if (thisPar.Inlines.Count == 0)
            {
                thisPar.Inlines.Insert(0, new EditableRun(""));
                emptyRunAdded = true;
            }

            EditOffset = -1;
            

            PostUpdate(thisPar);
            DisableUndoStack =  false;
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at restore pos: {origSelectionStart}"); }
        finally { DisableUndoStack =  false; }
    }

    private void PostUpdate(Paragraph thisPar)
    {
        UpdateTextRangesFromCharIdx = origSelectionStart;
        thisPar.CallRequestInlinesUpdate();
        flowDoc.UpdateBlockAndInlineStarts(thisPar);
        
        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
        });
    }
}

internal class DeleteRunUndo(int parId, EditableRun removedRunClone, int deletedRunIdx, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => true;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack =  true;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            thisPar.Inlines.Insert(deletedRunIdx, removedRunClone);
                        
            EditOffset = 1;
            PostUpdate(thisPar);

            DisableUndoStack =  false;
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at delete pos: {origSelectionStart}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack =  true;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            thisPar.Inlines.Remove(removedRunClone);

            EditOffset = -1;
            PostUpdate(thisPar);

            DisableUndoStack =  false;
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at delete pos: {origSelectionStart}"); }
        finally { DisableUndoStack =  false; }
    }

    private void PostUpdate(Paragraph thisPar)
    {
        UpdateTextRangesFromCharIdx = origSelectionStart;

        thisPar.CallRequestInlinesUpdate();
        flowDoc.UpdateBlockAndInlineStarts(thisPar);

        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
        });
    }
}


internal class DeleteLineBreakUndo(int parId, ((Type t1, int id1), (Type t2, int id2)) types, int lineBreakIdx, FlowDocument flowDoc, int origSelectionStart, bool deleteNext, bool startLineIsEmpty) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => true;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    readonly List<int> addedInlineIds = [];
    
    public void PerformUndo()
    {
        if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

        DisableUndoStack =  true;

        IEditable addIED1 = null!;
        if (types.Item1.t1 == typeof(EditableRun))
            addIED1 = new EditableRun("");
        else
            addIED1 = new EditableLineBreak();
            
        addIED1.Id = types.Item1.id1;

        thisPar.Inlines.Insert(lineBreakIdx, addIED1);
        addedInlineIds.Add(addIED1.Id);
                

        if (types.Item2.t2 != null)
        {
            IEditable addIED2 = null!;
            if (types.Item2.t2 == typeof(EditableRun))
                addIED2 = new EditableRun("");
            else
                addIED2 = new EditableLineBreak();
                
            addIED2.Id = types.Item2.id2;
            thisPar.Inlines.Insert(lineBreakIdx + 1, addIED2);
            addedInlineIds.Add(addIED2.Id);
        }

        EditOffset = 2;
        PostUpdate(thisPar);

        DisableUndoStack =  false;
    }

    public void PerformRedo()
    {

        if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;


        DisableUndoStack =  true;
                
        if (thisPar.Inlines.FirstOrDefault(il=> il.Id == types.Item1.id1) is EditableLineBreak elb)
        {
            thisPar.Inlines.Remove(elb);
        }
            
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
        {
            thisPar.Inlines.RemoveAt(lineBreakIdx);
        }
            

        foreach (int addedId in addedInlineIds)
        {
            if (thisPar.Inlines.FirstOrDefault(il => il.Id == addedId) is IEditable ied)
                thisPar.Inlines.Remove(ied);
        }

        EditOffset = -2;

        PostUpdate(thisPar);

        DisableUndoStack = false;
    }

    private void PostUpdate(Paragraph thisPar)
    {        

        thisPar.CallRequestInlinesUpdate();
        flowDoc.UpdateBlockAndInlineStarts(thisPar);

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
   int origRangeLen,
   bool firstBlockWasDeleted,
   bool lastBlockWasDeleted,
   bool doNextRedo
   ) : IEditDo

{  //parInlines are cloned inlines

    public int EditOffset { get; set; } = 0;
    public bool UpdateTextRanges => true;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;
    public bool DoNextRedo => doNextRedo;

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack =  true;

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

            EditOffset = origRangeLen;
        
            PostUpdate();

        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at Par index: {startBlockIndex}\n{ex.Message}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        DisableUndoStack =  true;

        keptBlockClones = keptBlockClones.ConvertAll(kpc => kpc.FullClone(true));

        TextRange origRange = new (flowDoc, origSelectionStart, originalRangeEnd);
        
        flowDoc.DeleteRange(origRange, false, true, false);
                
        flowDoc.UpdateBlockAndInlineStarts(startBlockIndex);
        
        flowDoc.TextRanges.Remove(origRange);

        EditOffset = -origRangeLen;

        PostUpdate();
    }

    private void PostUpdate()
    {
        DisableUndoStack =  false;

        foreach (Table t in keptBlockClones.OfType<Table>())
            t.UpdateColAndRowPoints();

        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
        });

    }

}




