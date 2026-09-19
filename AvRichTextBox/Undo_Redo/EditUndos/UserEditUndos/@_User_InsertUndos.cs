using Avalonia.Threading;

namespace AvRichTextBox; 

internal class InsertCharUndo(int parId, int runId, string insertedText, int insertPos, FlowDocument flowDoc, int origSelectionStart, bool doNextUndo) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => true;
    public int UpdateTextRangesFromCharIdx { get; set; }
    public bool DoNextUndo => doNextUndo;
    public bool DoNextRedo => false;

    int insertedTextLen = insertedText.Length;

    public void PerformUndo()
    {
        try
        {            
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
            if (thisPar.Inlines.FirstOrDefault(r => r.Id == runId) is not EditableRun thisRun) return;

            UpdateTextRangesFromCharIdx = origSelectionStart;
            
            DisableUndoStack =  true;

            thisRun.Text = thisRun.Text!.Remove(insertPos, 1);

            EditOffset = -insertedTextLen;
            PostUpdate(thisPar, origSelectionStart);

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at runId: {runId}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
            if (thisPar.Inlines.FirstOrDefault(r => r.Id == runId) is not EditableRun thisRun) return;
            
            DisableUndoStack =  true;

            thisRun.Text = thisRun.Text!.Insert(insertPos, insertedText);

            EditOffset = insertedTextLen;
            PostUpdate(thisPar, origSelectionStart + 1);

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at runId: {runId}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PostUpdate(Paragraph thisPar, int setCaretPos)
    {
        DisableUndoStack =  false;
        UpdateTextRangesFromCharIdx = origSelectionStart - 1;

        
        thisPar.CallRequestInlinesUpdate();
        flowDoc.UpdateBlockAndInlineStarts(thisPar);

        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.Selection.BiasForwardStart = thisPar.StartInDoc != flowDoc.Selection.Start;
            flowDoc.Selection.BiasForwardEnd = flowDoc.Selection.BiasForwardStart;
            flowDoc.Selection.Start = setCaretPos;
            flowDoc.Selection.End = flowDoc.Selection.Start;
        });
    }

}

internal class InsertLineBreakUndo(int insertParId, int insertedLBId, List<int> addedInlineIds, int insertIdx, IEditable origInlineClone, FlowDocument flowDoc, int origSelectionStart, bool doNextUndo) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => true;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => doNextUndo;
    public bool DoNextRedo => false;

    int thisParLengthBefore = 0;

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack =  true;

            UpdateTextRangesFromCharIdx = origSelectionStart;

            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == insertParId) is not Paragraph thisPar) return;
            thisParLengthBefore = thisPar.TextLength;

            if (thisPar.Inlines.FirstOrDefault(lb => lb.Id == insertedLBId) is not EditableLineBreak thisELB) return;

            foreach (int ilId in addedInlineIds)
            {
                if (thisPar.Inlines.FirstOrDefault(il => il.Id == ilId) is IEditable ied)
                    thisPar.Inlines.Remove(ied);
            }

            thisPar.Inlines.Remove(thisELB);
            thisPar.Inlines.Insert(insertIdx, origInlineClone);

            EditOffset = 2;
            PostUpdate(thisPar, origSelectionStart);
     
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name}"); }
        finally { DisableUndoStack =  false; }

    }

    public void PerformRedo()
    {
        DisableUndoStack =  true;

        try
        {
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == insertParId) is not Paragraph thisPar) return;
            if (flowDoc.GetStartInline(origSelectionStart) is not IEditable startInline) return;
            origInlineClone = startInline.CloneWithId();

            thisParLengthBefore = thisPar.TextLength;
            
            List<IEditable> eruns = flowDoc.SplitRunAtPos(origSelectionStart, startInline, flowDoc.GetCharPosInInline(startInline, origSelectionStart)); // creates empty inline
            for (int erunno = 0; erunno < eruns.Count; erunno++)
                eruns[erunno].Id = addedInlineIds[erunno];
            

            insertIdx += 1;
            thisPar.Inlines.Insert(insertIdx, new EditableLineBreak() { Id = insertedLBId });

            if (insertIdx == thisPar.Inlines.Count - 1 || thisPar.Inlines[insertIdx + 1].IsLineBreak)
            {
                EditableRun newErun = new("") { Id = insertedLBId };
                thisPar.Inlines.Insert(insertIdx + 1, newErun);
                addedInlineIds.Add(newErun.Id);
            }

            insertIdx -= 1;
            //EditOffset = origSelLen - 2;
            EditOffset = -2;
            PostUpdate(thisPar, origSelectionStart + 2);
        }

        catch { Debug.WriteLine($"Failed {this.GetType().Name}"); }
        finally { DisableUndoStack =  false; }

    }

    public void PostUpdate(Paragraph thisPar, int selStart)
    {
        DisableUndoStack =  false;
        EditOffset = thisPar.TextLength - thisParLengthBefore;

        UpdateTextRangesFromCharIdx = origSelectionStart - Math.Abs(EditOffset);

        thisPar.CallRequestInlinesUpdate();
        flowDoc.UpdateBlockAndInlineStarts(thisPar);

        flowDoc.Selection.Start = selStart;
        flowDoc.Selection.End = flowDoc.Selection.Start;
    }
}


