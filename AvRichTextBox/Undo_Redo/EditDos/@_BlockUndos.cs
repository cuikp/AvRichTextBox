
using Avalonia.Media;
using Avalonia.Threading;

namespace AvRichTextBox;

internal class BlockMarginChangedUndo(int blockId, Thickness oldMargin, Thickness newMargin, FlowDocument flowDoc) : IEditDo
{

    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.Margin = oldMargin;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.Margin = newMargin;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

}

internal class BlockBackgroundChangedUndo(int blockId, IBrush oldBrush, IBrush newBrush, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.Background = oldBrush;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
    
    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.Background = newBrush;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

internal class BlockBorderBrushChangedUndo(int blockId, IBrush oldBrush, IBrush newBrush, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.BorderBrush = oldBrush;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.BorderBrush = newBrush;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

internal class BlockBorderThicknessChangedUndo(int blockId, Thickness oldThickness, Thickness newThickness, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.BorderThickness = oldThickness;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.BorderThickness = newThickness;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

internal class BlockFontFamilyChangedUndo(int blockId, FontFamily oldFontFamily, FontFamily newFontFamily, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.FontFamily = oldFontFamily;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.FontFamily = newFontFamily;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

internal class BlockFontSizeChangedUndo(int blockId, double oldFontSize, double newFontSize, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.FontSize = oldFontSize;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.FontSize = newFontSize;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

internal class BlockFontWeightChangedUndo(int blockId, FontWeight oldFontWeight, FontWeight newFontWeight, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.FontWeight = oldFontWeight;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.FontWeight = newFontWeight;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

internal class BlockFontStyleChangedUndo(int blockId, FontStyle oldFontStyle, FontStyle newFontStyle, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.FontStyle = oldFontStyle;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                flowDoc.disableUndoStack = true;
                b.FontStyle = newFontStyle;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

internal class InsertBlockUndo( FlowDocument flowDoc, int insertedBlockId, int undoEditOffset, bool IsCellParagraph, int containingTableId, int containingCellId) : IEditDo
{  
    public int UndoEditOffset => undoEditOffset;
    public bool UpdateTextRanges => true;
    int insertBlockIdx = -1;
    Block insertBlock = null!;
    int startCharIdx = 0;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;

            if (flowDoc.GetBlockFromId(insertedBlockId) is not Block insertedBlock) return;

            insertBlock = insertedBlock.FullClone(true);

            int updateFromBlockIdx = 0;
            startCharIdx = insertedBlock.StartInDoc;
            
            if (IsCellParagraph)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell containingCell) return;
                updateFromBlockIdx = flowDoc.Blocks.IndexOf(containingTable);
                insertBlockIdx = containingCell.CellBlocks.IndexOf(insertedBlock);
                containingCell.CellBlocks.Remove(insertedBlock);
                containingTable.UpdateCellParagraphSizes();
            }
            else
            {
                updateFromBlockIdx = flowDoc.Blocks.IndexOf(insertedBlock);
                insertBlockIdx = updateFromBlockIdx;
                flowDoc.Blocks.Remove(insertedBlock);
            }

            PostUpdate(updateFromBlockIdx, startCharIdx);

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Inserted block id: {insertedBlockId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            flowDoc.disableUndoStack = true;
            
            int blockIdx = 0;

            if (IsCellParagraph)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell containingCell) return;
                
                containingCell.CellBlocks.Insert(insertBlockIdx, insertBlock);
                containingTable.UpdateCellParagraphSizes();
            }
            else
            {
                flowDoc.Blocks.Insert(insertBlockIdx, insertBlock);
            }

            PostUpdate(blockIdx, startCharIdx);

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Inserted block id: {insertedBlockId}"); }
        finally { flowDoc.disableUndoStack = false; }

    }

    private void PostUpdate(int blockIdx, int startCharIdx)
    {
        flowDoc.disableUndoStack = false;
        flowDoc.UpdateBlockAndInlineStarts(blockIdx);
        flowDoc.UpdateTextRanges(startCharIdx, -undoEditOffset);

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            flowDoc.Selection.Start = Math.Min(flowDoc.Selection.Start, flowDoc.DocEndPoint);
            flowDoc.Selection.CollapseToStart();
        });
    }
}

internal class RemoveBlockUndo(FlowDocument flowDoc, int originalIndex, Block removedBlockClone, int undoEditOffset, bool IsCellParagraph, int containingTableId, int containingCellId) : IEditDo
{  
    public int UndoEditOffset => undoEditOffset;
    public bool UpdateTextRanges => true;
    int startCharIdx = 0;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;
            startCharIdx = removedBlockClone.StartInDoc;

            if (IsCellParagraph)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell containingCell) return;
                containingCell.CellBlocks.Insert(originalIndex, removedBlockClone);
            }
            else
            {
                flowDoc.Blocks.Insert(originalIndex, removedBlockClone);
            }

            PostUpdate();
            
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Removed at block idx: {originalIndex}"); }
        finally {flowDoc.disableUndoStack = false;}
    }

    public void PerformRedo()
    {
        try
        {
            flowDoc.disableUndoStack = true;
            
            if (IsCellParagraph)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell containingCell) return;
                
                containingCell.CellBlocks.Remove(removedBlockClone);
            }
            else
            {
                flowDoc.Blocks.Remove(removedBlockClone);
            }

            PostUpdate();

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Removed at block idx: {originalIndex}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    private void PostUpdate()
    {
        flowDoc.disableUndoStack = false;
        flowDoc.UpdateTextRanges(startCharIdx, undoEditOffset);
        flowDoc.InvokeSelectionChanged();
    }
}
