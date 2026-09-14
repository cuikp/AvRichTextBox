using Avalonia.Threading;
using DynamicData;
using System.Collections.ObjectModel;

namespace AvRichTextBox;

internal class InsertHyperlinkAtCharIdxUndo(
       int destStartParId,
       int insertBlockIndex,
       List<Block> origBlockClones,
       FlowDocument flowDoc,
       int origSelectionStart,
       int textChangeLen,
       bool firstParEmpty,
       bool firstParWasDeleted = false,
       bool lastParWasDeleted = false,
       bool isCellBlock = false,
       int owningTableId = -1,
       int owningCellId = -1
    ) : IEditDo
{
    public int UndoEditOffset { get; }
    public bool UpdateTextRanges => false;
    Paragraph keepRedoParagraph = null!;
    
    public void PerformUndo()
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.AllParagraphs.FirstOrDefault(p=> p.Id == destStartParId) is Paragraph destStartPar)
            {
                keepRedoParagraph = destStartPar.FullClone(true);

                int updateBlocksFromIndex = -1;
                
                if (flowDoc.DetermineBlockCollection(isCellBlock, insertBlockIndex, owningTableId, owningCellId, out updateBlocksFromIndex) is ObservableCollection<Block> blockCollection && updateBlocksFromIndex > -1)
                    flowDoc.RestoreDeletedBlocks(origBlockClones, insertBlockIndex, firstParWasDeleted, lastParWasDeleted, blockCollection, updateBlocksFromIndex);
            }

            PostUpdate();

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockIndex: {insertBlockIndex}"); }
        finally { DisableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack = true;

            int updateBlocksFromIndex = -1;
            if (flowDoc.DetermineBlockCollection(isCellBlock, insertBlockIndex, owningTableId, owningCellId, out updateBlocksFromIndex) is ObservableCollection<Block> blockCollection && updateBlocksFromIndex > -1)
            {
                if (!firstParEmpty)
                    flowDoc.Blocks.RemoveAt(insertBlockIndex);

                origBlockClones = origBlockClones.ConvertAll(kbc => kbc.FullClone(true));

                blockCollection.Insert(updateBlocksFromIndex, keepRedoParagraph);
            }

            PostUpdate();

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockIndex: {insertBlockIndex}"); }
        finally { DisableUndoStack = false; }
    }

    private void PostUpdate()
    {
        DisableUndoStack = false;

        flowDoc.UpdateTextRanges(origSelectionStart, textChangeLen);

        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = origSelectionStart;
            flowDoc.UpdateSelection();
        });

    }

}

internal class HyperlinkUpdateUndo (int parId, int hyperlinkId, string oldNavUri, string newNavUri, string oldText, string newText, FlowDocument flowDoc, int selStart, int textChangeLen): IEditDo
{
    public int UndoEditOffset { get; }
    public bool UpdateTextRanges => false;

    string previousText = "";

    public void PerformUndo()
    {
        previousText = newText;
        DoUpdate(oldNavUri, oldText);
    }
    
    public void PerformRedo()
    {
        DoUpdate(newNavUri, newText);
    }

    private void DoUpdate(string uri, string text)
    {
        DisableUndoStack = true;
        if (flowDoc.AllParagraphs.FirstOrDefault(p => p.Id == parId) is not Paragraph thisPar) return;
        if (thisPar.Inlines.FirstOrDefault(il => il.Id == hyperlinkId) is not EditableHyperlink edHL) return;
        
        int textLenChange = text.Length - previousText.Length;
        previousText = text;

        edHL.NavigateUri = uri;
        edHL.LinkDisplayText = text;

        Dispatcher.UIThread.Post(() =>
        {
            thisPar.CallRequestInlinesUpdate();
            thisPar.UpdateEditableRunPositions();
            flowDoc.UpdateBlockAndInlineStarts(thisPar);
            flowDoc.UpdateTextRanges(flowDoc.GetAbsPositionOfInlineInDoc(edHL), textLenChange);
        });

        DisableUndoStack = false;
    }
}


internal class HyperlinkDisplayTextChangedUndo(int parId, int hyperlinkId, string oldText, string newText, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset { get; }
    public bool UpdateTextRanges => false;

    string previousText = "";

    public void PerformUndo() { previousText = newText; DoChange(oldText); } 
    public void PerformRedo() { DoChange(newText);  }

    void DoChange(string text)
    {
        if (flowDoc.AllParagraphs.FirstOrDefault(p => p.Id == parId) is not Paragraph thisPar || thisPar.Inlines.FirstOrDefault(il => il.Id == hyperlinkId) is not EditableHyperlink eHL)
            return;

        try
        {
            DisableUndoStack = true;
            
            eHL.LinkDisplayText = text;
            int textLenChange = text.Length - previousText.Length;
            previousText = text;
            thisPar.UpdateEditableRunPositions();
            flowDoc.UpdateBlockAndInlineStarts(thisPar);
            flowDoc.UpdateTextRanges(flowDoc.GetAbsPositionOfInlineInDoc(eHL), textLenChange);
            
            DisableUndoStack = false;

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at hyperlinkId: {hyperlinkId}"); }
        finally { DisableUndoStack = false; }
        
    }
}

internal class HyperlinkNavigateUriChangedUndo(int parId, int hyperlinkId, string oldUri, string newUri, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset { get; }
    public bool UpdateTextRanges => false;

    public void PerformUndo() { DoChange(oldUri); } 
    public void PerformRedo() { DoChange(newUri);  }

    void DoChange(string text)
    {
        if (flowDoc.AllParagraphs.FirstOrDefault(p => p.Id == parId) is not Paragraph thisPar || thisPar.Inlines.FirstOrDefault(il => il.Id == hyperlinkId) is not EditableHyperlink eHL)
            return;

        try
        {
            DisableUndoStack = true;

            eHL.NavigateUri = text;

            DisableUndoStack = false;

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at hyperlinkId: {hyperlinkId}"); }
        finally { DisableUndoStack = false; }
        
    }
}


internal class RemoveHyperlinkUndo( int parId, EditableHyperlink removedHyperlinkClone, int addedRunId, FlowDocument flowDoc ) : IEditDo
{
    public int UndoEditOffset { get; }
    public bool UpdateTextRanges => false;
    EditableRun addedRunClone = null!;

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.AllParagraphs.FirstOrDefault(p => p.Id == parId) is Paragraph thisPar && thisPar.Inlines.FirstOrDefault(il=> il.Id == addedRunId) is EditableRun erun)
            {
                addedRunClone = erun.CloneWithId();
                int index = thisPar.Inlines.IndexOf(erun);
                thisPar.Inlines[index] = removedHyperlinkClone;
                thisPar.CallRequestInlinesUpdate();
            }

            DisableUndoStack = false;

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Undo runId: {addedRunId}"); }
        finally { DisableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.AllParagraphs.FirstOrDefault(p => p.Id == parId) is Paragraph thisPar && thisPar.Inlines.FirstOrDefault(il => il.Id == removedHyperlinkClone.Id) is EditableHyperlink eHL)
            {
                removedHyperlinkClone = eHL.CloneWithId();
                int index = thisPar.Inlines.IndexOf(eHL);
                thisPar.Inlines[index] = addedRunClone;
                thisPar.CallRequestInlinesUpdate();
            }

            DisableUndoStack = false;

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Redo hyperlinkId: {removedHyperlinkClone.Id}"); }
        finally { DisableUndoStack = false; }
    }

    
}
