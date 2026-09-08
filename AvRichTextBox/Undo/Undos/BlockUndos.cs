
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;

namespace AvRichTextBox;

internal class BlockMarginChangedUndo(int blockId, Thickness oldMargin, FlowDocument flowDoc) : IEditDo
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
    }

}

internal class BlockBackgroundChangedUndo(int blockId, IBrush oldBrush, FlowDocument flowDoc) : IEditDo
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
    }
}

internal class BlockBorderBrushChangedUndo(int blockId, IBrush oldBrush, FlowDocument flowDoc) : IEditDo
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
    }
}

internal class BlockBorderThicknessChangedUndo(int blockId, Thickness oldThickness, FlowDocument flowDoc) : IEditDo
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
    }
}

internal class BlockFontFamilyChangedUndo(int blockId, FontFamily oldFontFamily, FlowDocument flowDoc) : IEditDo
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
    }
}

internal class BlockFontSizeChangedUndo(int blockId, double oldFontSize, FlowDocument flowDoc) : IEditDo
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
    }
}

internal class BlockFontWeightChangedUndo(int blockId, FontWeight oldFontWeight, FlowDocument flowDoc) : IEditDo
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
    }
}

internal class BlockFontStyleChangedUndo(int blockId, FontStyle oldFontStyle, FlowDocument flowDoc) : IEditDo
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
    }
}

internal class InsertBlockUndo( FlowDocument flowDoc, int insertedBlockId, int undoEditOffset, bool IsCellParagraph, int containingTableId, int containingCellId) : IEditDo
{  
    public int UndoEditOffset => undoEditOffset;
    public bool UpdateTextRanges => true;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(insertedBlockId) is not Block insertedBlock) return;

            int blockIdx = 0;
            int startCharIdx = insertedBlock.StartInDoc;

            if (IsCellParagraph)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell containingCell) return;
                blockIdx = flowDoc.Blocks.IndexOf(containingTable);
                containingCell.CellBlocks.Remove(insertedBlock);
                containingTable.UpdateCellParagraphSizes();
            }
            else
            {
                blockIdx = flowDoc.Blocks.IndexOf(insertedBlock);
                flowDoc.Blocks.Remove(insertedBlock);
            }

            flowDoc.UpdateBlockAndInlineStarts(blockIdx);
            flowDoc.UpdateTextRanges(startCharIdx, -undoEditOffset);

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                flowDoc.Selection.Start = Math.Min(flowDoc.Selection.Start, flowDoc.DocEndPoint);
                flowDoc.Selection.CollapseToStart();
            });
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Inserted block id: {insertedBlockId}"); }

    }

    public void PerformRedo()
    {
    }
}

internal class RemoveBlockUndo(FlowDocument flowDoc, int originalIndex, Block removedBlockClone, int undoEditOffset, bool IsCellParagraph, int containingTableId, int containingCellId) : IEditDo
{  
    public int UndoEditOffset => undoEditOffset;
    public bool UpdateTextRanges => true;

    public void PerformUndo()
    {
        try
        {            
            int startCharIdx = removedBlockClone.StartInDoc;

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

            flowDoc.UpdateTextRanges(startCharIdx, undoEditOffset);
            flowDoc.InvokeSelectionChanged();

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Removed at block idx: {originalIndex}"); }

    }

    public void PerformRedo()
    {

    }
}
