using Avalonia.Threading;

namespace AvRichTextBox; 

internal class InsertCharUndo(int parId, int runId, int insertPos, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int UndoEditOffset => -1;
    public bool UpdateTextRanges => true;

    public void PerformUndo()
    {
        try
        {            
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
            if (thisPar.Inlines.FirstOrDefault(r => r.Id == runId) is not EditableRun thisRun) return;
            
            flowDoc.disableRunTextUndo = true;

            thisRun.Text = thisRun.Text!.Remove(insertPos, 1);

            flowDoc.disableRunTextUndo = false;

            thisPar.CallRequestInlinesUpdate();
            flowDoc.UpdateBlockAndInlineStarts(thisPar);

            Dispatcher.UIThread.Post(() =>
            {
                flowDoc.Selection.BiasForwardStart = thisPar.StartInDoc != flowDoc.Selection.Start;
                flowDoc.Selection.BiasForwardEnd = flowDoc.Selection.BiasForwardStart;

                flowDoc.Selection.Start = origSelectionStart;
                flowDoc.Selection.End = flowDoc.Selection.Start;
                
            });

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at runId: {runId}"); }

    }

    public void PerformRedo()
    {
    }

}



internal class InsertLineBreakUndo(int insertParId, int insertedLBId, List<int> addedInlineIds, int insertIdx, IEditable origInlineClone, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int UndoEditOffset => -1;
    public bool UpdateTextRanges => true;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableRunTextUndo = true;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == insertParId) is not Paragraph thisPar) return;
            int thisParLengthBefore = thisPar.TextLength;
            if (thisPar.Inlines.FirstOrDefault(lb => lb.Id == insertedLBId) is not EditableLineBreak thisELB) return;

            foreach (int ilId in addedInlineIds)
            {
                if (thisPar.Inlines.FirstOrDefault(il => il.Id == ilId) is IEditable ied)
                    thisPar.Inlines.Remove(ied);
            }

            thisPar.Inlines.Remove(thisELB);
            thisPar.Inlines.Insert(insertIdx, origInlineClone);

            int thisParLengthAfter = thisPar.TextLength;

            thisPar.CallRequestInlinesUpdate();
            flowDoc.UpdateBlockAndInlineStarts(thisPar);
            flowDoc.UpdateTextRanges(thisPar.StartInDoc, thisParLengthAfter - thisParLengthBefore);

            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
            flowDoc.disableRunTextUndo = false;
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name}"); }

    }

    public void PerformRedo()
    {
    }

}


