
using Avalonia.Media;
using Avalonia.Threading;
using static AvRichTextBox.FlowDocument;

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
                DisableUndoStack =  true;
                b.Margin = oldMargin;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                DisableUndoStack =  true;
                b.Margin = newMargin;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
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
                DisableUndoStack =  true;
                b.Background = oldBrush;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
    }
    
    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                DisableUndoStack =  true;
                b.Background = newBrush;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
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
                DisableUndoStack =  true;
                b.BorderBrush = oldBrush;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                DisableUndoStack =  true;
                b.BorderBrush = newBrush;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
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
                DisableUndoStack =  true;
                b.BorderThickness = oldThickness;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                DisableUndoStack =  true;
                b.BorderThickness = newThickness;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
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
                DisableUndoStack =  true;
                b.FontFamily = oldFontFamily;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                DisableUndoStack =  true;
                b.FontFamily = newFontFamily;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
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
                DisableUndoStack =  true;
                b.FontSize = oldFontSize;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                DisableUndoStack =  true;
                b.FontSize = newFontSize;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
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
                DisableUndoStack =  true;
                b.FontWeight = oldFontWeight;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                DisableUndoStack =  true;
                b.FontWeight = newFontWeight;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
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
                DisableUndoStack =  true;
                b.FontStyle = oldFontStyle;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(blockId) is Block b)
            {
                DisableUndoStack =  true;
                b.FontStyle = newFontStyle;
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at blockId: {blockId}"); }
        finally { DisableUndoStack =  false; }
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
            DisableUndoStack =  true;

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
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack =  true;
            
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
        finally { DisableUndoStack =  false; }

    }

    private void PostUpdate(int blockIdx, int startCharIdx)
    {
        undoEditOffset = -undoEditOffset;

        DisableUndoStack =  false;
              
        flowDoc.Selection.Start = Math.Min(flowDoc.Selection.Start, flowDoc.DocEndPoint);
        flowDoc.Selection.CollapseToStart();
      
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
            DisableUndoStack =  true;
            startCharIdx = removedBlockClone.StartInDoc;

            if (IsCellParagraph)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell containingCell) return;
                containingCell.CellBlocks.Insert(originalIndex, removedBlockClone);
                containingTable.UpdateCellParagraphSizes();
            }
            else
            {
                flowDoc.Blocks.Insert(originalIndex, removedBlockClone);
            }

            PostUpdate();
            
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Removed at block idx: {originalIndex}"); }
        finally {DisableUndoStack =  false;}
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack =  true;
            
            if (IsCellParagraph)
            {
                if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == containingTableId) is not Table containingTable) return;
                if (containingTable.Cells.FirstOrDefault(cell => cell.Id == containingCellId) is not Cell containingCell) return;
                
                containingCell.CellBlocks.Remove(removedBlockClone);
                containingTable.UpdateCellParagraphSizes();
            }
            else
            {
                flowDoc.Blocks.Remove(removedBlockClone);
            }

            PostUpdate();

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at Removed at block idx: {originalIndex}"); }
        finally { DisableUndoStack =  false; }
    }

    private void PostUpdate()
    {
        DisableUndoStack =  false;
        flowDoc.UpdateTextRanges(startCharIdx, undoEditOffset);
        flowDoc.InvokeSelectionChanged();
    }
}
