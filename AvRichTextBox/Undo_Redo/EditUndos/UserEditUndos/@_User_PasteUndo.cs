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
   bool firstParEmpty,
   List<int> addedBlockIds,
   bool firstParWasDeleted,
   bool lastParWasDeleted,
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

            if (blockCollection.FirstOrDefault(b => b.Id == origStartBlockId) is Block firstOrigBlock)
                keptPastedBlockClones.Add(firstOrigBlock.FullClone(true));

            flowDoc.RestoreDeletedBlocks(keptOrigBlockClones, insertBlockIndex, firstParWasDeleted, lastParWasDeleted, blockCollection, updateBlocksFromIndex);

            foreach (int bid in addedBlockIds)
            {
                if (blockCollection.FirstOrDefault(b => b.Id == bid) is Block foundBlock)
                {
                    int indexInCol = blockCollection.IndexOf(foundBlock);
                    keptPastedBlockClones.Add(foundBlock.FullClone(true));
                    blockCollection.Remove(foundBlock);
                }
            }

            keptPastedBlockClones.ForEach(bc => bc.MyFlowDoc = null!);

            if (firstParEmpty)
            {
                Block firstBlock = blockCollection[insertBlockIndex];
                if (firstBlock is Paragraph firstPar && firstPar.Inlines.Count == 1 && firstPar.Inlines[0] is EditableRun run)
                    run.Text = ""; 
            }



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

            //if (!firstParEmpty)  // ?????????????
            blockCollection.RemoveAt(insertBlockIndex);

            //keptOrigBlockClones = keptOrigBlockClones.ConvertAll(kbc => kbc.FullClone(true));
            for (int bno = 0; bno < keptOrigBlockClones.Count; bno++)
            {
                if (flowDoc.GetBlockFromId(keptOrigBlockClones[bno].Id) is Block b)
                    keptOrigBlockClones[bno] = b.FullClone(true);
            }
            
            blockCollection.AddOrInsertRange(keptPastedBlockClones, insertBlockIndex);

            EditOffset = pastedTextLength - deletedRangeLen;


            PostUpdate(origSelectionStart + pastedTextLength);         

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at OrigSelectionStart: {origSelectionStart}"); }
        finally { DisableUndoStack =  false; }
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


