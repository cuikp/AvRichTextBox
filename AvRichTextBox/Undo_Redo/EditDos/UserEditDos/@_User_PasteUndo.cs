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
   int undoEditOffset,
   bool firstParEmpty,
   List<int> addedBlockIds,
   bool firstParWasDeleted,
   bool lastParWasDeleted,
   bool pastedInCellBlock,
   int containingTableId,
   int containingCellId
   ) : IEditDo

{
    public int UndoEditOffset => undoEditOffset;
    public bool UpdateTextRanges => true;
    List<Block> keptPastedBlockClones = [];

    int lengthBefore = flowDoc.Text.Length;

    public void PerformUndo()
    {
        try
        {            
            flowDoc.disableRunTextUndo = true;
            flowDoc.disableUndoStack = true;
            lengthBefore = flowDoc.Text.Length;
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

            PostUpdate();

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at OrigSelectionStart: {origSelectionStart}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            flowDoc.disableRunTextUndo = true;
            flowDoc.disableUndoStack = true;

            lengthBefore = flowDoc.Text.Length;

            int updateBlocksFromIndex = -1;
            if (DetermineBlockCollection(out updateBlocksFromIndex) is not ObservableCollection<Block> blockCollection || updateBlocksFromIndex == -1)
                return;

            //if (firstParEmpty)  // not necessary
            flowDoc.Blocks.RemoveAt(insertBlockIndex);

            keptOrigBlockClones = keptOrigBlockClones.ConvertAll(kbc => kbc.FullClone(true));

            flowDoc.Blocks.AddOrInsertRange(keptPastedBlockClones, insertBlockIndex);
                    
            PostUpdate();         

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at OrigSelectionStart: {origSelectionStart}"); }
        finally { flowDoc.disableUndoStack = false; }
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

    private void PostUpdate()
    {
        flowDoc.disableRunTextUndo = false;
        flowDoc.disableUndoStack = false;

        foreach (Table t in keptOrigBlockClones.OfType<Table>())
            t.UpdateColAndRowPoints();

        
        int lengthAfter = flowDoc.Text.Length;
        flowDoc.UpdateTextRanges(origSelectionStart, lengthAfter - lengthBefore);

        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.UpdateSelection();
            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
            flowDoc.ScrollFlowDocToCaret();
        });
    }

}


