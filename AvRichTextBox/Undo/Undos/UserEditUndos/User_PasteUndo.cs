using Avalonia.Threading;
using System.Collections.ObjectModel;

namespace AvRichTextBox; 


internal class PasteUndo(
   List<Block> keptBlocks,
   int blockIndex,
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

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableRunTextUndo = true;

            int lengthBefore = flowDoc.Text.Length;

            Cell? containingCell = null;
            int updateBlocksFromIndex = blockIndex;
            ObservableCollection<Block> blockCollection = flowDoc.Blocks;
            if (pastedInCellBlock)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell cell) return;
                containingCell = cell;
                updateBlocksFromIndex = flowDoc.Blocks.IndexOf(containingTable);
                blockCollection = containingCell.CellBlocks;
            }

            flowDoc.RestoreDeletedBlocks(keptBlocks, blockIndex, firstParWasDeleted, lastParWasDeleted, blockCollection, updateBlocksFromIndex);

         
            foreach (int bid in addedBlockIds)
                if (blockCollection.FirstOrDefault(b => b.Id == bid) is Block foundBlock)
                    blockCollection.Remove(foundBlock);

            if (firstParEmpty)
            {
                Block firstBlock = blockCollection[blockIndex];
                if (firstBlock is Paragraph firstPar && firstPar.Inlines.Count == 1 && firstPar.Inlines[0] is EditableRun run)
                    run.Text = ""; //firstPar.CallRequestInlinesUpdate();
            }


            foreach (Table t in keptBlocks.OfType<Table>())
                t.UpdateColAndRowPoints();

            flowDoc.disableRunTextUndo = false;

            int lengthAfter = flowDoc.Text.Length;
                        
            flowDoc.UpdateTextRanges(keptBlocks[0].StartInDoc, lengthAfter - lengthBefore);


            Dispatcher.UIThread.Post(() =>
            {
                flowDoc.UpdateSelection();
                flowDoc.Selection.Start = origSelectionStart;
                flowDoc.Selection.End = flowDoc.Selection.Start;
            });

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at OrigSelectionStart: {origSelectionStart}"); }
    }

    public void PerformRedo()
    {
    }

}


