using Avalonia.Threading;
using DynamicData;
using System.Collections.ObjectModel;

namespace AvRichTextBox; 

internal class PasteUndo(
   int origStartBlockId,
   int insertBlockIndex,
   List<Block> keptOrigBlockClones,
   FlowDocument flowDoc,
   int origSelectionStart,
   int deletedRangeLen,
   int pastedTextLength,
   List<int> addedBlockIds,
   bool pastedInCellBlock,
   int containingTableId,
   int containingCellId
   ) : IEditDo

{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => true;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    List<Block> keptPastedBlockClones = [];

    public void PerformUndo()
    {
        try
        {            
            DisableUndoStack =  true;
            int updateBlocksFromIndex = -1;

            keptPastedBlockClones = [];

            if (DetermineBlockCollection(out updateBlocksFromIndex) is not ObservableCollection<Block> blockCollection || updateBlocksFromIndex == -1)
                return;

            var affectedIds = keptOrigBlockClones.Select(b => b.Id).Concat(addedBlockIds).Append(origStartBlockId).ToHashSet();
            keptPastedBlockClones = blockCollection.Where(b => affectedIds.Contains(b.Id))
                .Select(b => b.FullClone(true)).ToList();
            ReplaceBlocks(blockCollection, keptPastedBlockClones, keptOrigBlockClones);
            flowDoc.UpdateBlockAndInlineStarts(updateBlocksFromIndex);

            EditOffset = deletedRangeLen - pastedTextLength;
            UpdateTextRangesFromCharIdx = origSelectionStart;

            PostUpdate(origSelectionStart);

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at OrigSelectionStart: {origSelectionStart}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack =  true;

            int updateBlocksFromIndex = -1;
            if (DetermineBlockCollection(out updateBlocksFromIndex) is not ObservableCollection<Block> blockCollection || updateBlocksFromIndex == -1)
                return;

            ReplaceBlocks(blockCollection, keptOrigBlockClones, keptPastedBlockClones);
            flowDoc.UpdateBlockAndInlineStarts(updateBlocksFromIndex);

            EditOffset = pastedTextLength - deletedRangeLen;


            PostUpdate(origSelectionStart + pastedTextLength);         

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at OrigSelectionStart: {origSelectionStart}"); }
        finally { DisableUndoStack =  false; }
    }

    private void ReplaceBlocks(ObservableCollection<Block> blockCollection, List<Block> currentBlocks, List<Block> restoredBlocks)
    {
        var currentIds = currentBlocks.Select(b => b.Id).ToHashSet();
        foreach (Block block in blockCollection.Where(b => currentIds.Contains(b.Id)).ToList())
            blockCollection.Remove(block);

        // Keep history snapshots detached: subsequent edits must not change a saved undo state.
        blockCollection.AddOrInsertRange(restoredBlocks.Select(b => b.FullClone(true)), insertBlockIndex);
        FlowDocument.UpdateTextLayouts(blockCollection.Skip(insertBlockIndex).Take(restoredBlocks.Count));
    }

    private ObservableCollection<Block> DetermineBlockCollection(out int updateBlocksFromIndex)
    {
        ObservableCollection<Block> returnBlockCollection = flowDoc.Blocks;

        Cell? containingCell = null;
        updateBlocksFromIndex = insertBlockIndex;
        
        if (pastedInCellBlock)
        {
            if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return null!;
            if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell cell) return null!;
            containingCell = cell;
            updateBlocksFromIndex = flowDoc.Blocks.IndexOf(containingTable);
            returnBlockCollection = containingCell.CellBlocks;
        }

        return returnBlockCollection;
    }

    private void PostUpdate(int selStart)
    {
        DisableUndoStack =  false;

        foreach (Table t in keptOrigBlockClones.OfType<Table>())
            t.UpdateColAndRowPoints();
        
        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.UpdateSelection();
            flowDoc.Selection.Start = selStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
            flowDoc.ScrollFlowDocToCaret();
        });
    }

}


