using Avalonia.Layout;
using Avalonia.Threading;

namespace AvRichTextBox;

internal class InsertColumnsUndo(int thisTableId, List<int> insertedCellIds, int insertedColumnIdx, int insertedCount, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int UndoEditOffset => -insertedCount;
    public bool UpdateTextRanges => true;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;
            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                foreach (int cellId in insertedCellIds)
                {
                    if (table.Cells.FirstOrDefault(c => c.Id == cellId) is Cell cellToRemove)
                        table.Cells.Remove(cellToRemove);
                }

                for (int i = 0; i < insertedCount; i++)
                    table.ColDefs.RemoveAt(insertedColumnIdx);

                for (int rowno = 0; rowno < table.RowDefs.Count; rowno++)
                {
                    for (int colno = insertedColumnIdx + 1; colno < table.ColDefs.Count + insertedCount; colno++)
                    {
                        if (table.GetCellAt(rowno, colno) is Cell shiftCell)
                            shiftCell.ColNo -= insertedCount;
                    }
                }

                table.Width = table.ColDefs.Sum(cd => cd.Width.Value);
                //table.UpdateFlowDoc();
                flowDoc.Select(origSelectionStart, 0);
                flowDoc.disableUndoStack = false;
            }
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at Col index: {insertedColumnIdx}\n{ex.Message}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
    }
}

internal class InsertRowsUndo(int thisTableId, List<int> insertedCellIds, int insertedRowIdx, int insertedCount, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int UndoEditOffset => -insertedCount;
    public bool UpdateTextRanges => true;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;

            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                foreach (int cellId in insertedCellIds)
                {
                    if (table.Cells.FirstOrDefault(c => c.Id == cellId) is Cell cellToRemove)
                        table.Cells.Remove(cellToRemove);
                }

                for (int i = 0; i < insertedCount; i++)
                    table.RowDefs.RemoveAt(insertedRowIdx);

                for (int rowno = insertedRowIdx + 1; rowno < table.RowDefs.Count + insertedCount; rowno++)
                {
                    for (int colno = 0; colno < table.ColDefs.Count; colno++)
                    {
                        if (table.GetCellAt(rowno, colno) is Cell shiftCell)
                            shiftCell.RowNo -= insertedCount;
                    }
                }

                //table.Width = table.ColDefs.Sum(cd => cd.Width.Value);
                //table.UpdateFlowDoc();
                flowDoc.Select(origSelectionStart, 0);
                flowDoc.disableUndoStack = false;
            }
        }
        catch (Exception ex) { Debug.WriteLine($"Failed InsertColumnsUndo at Col index: {insertedRowIdx}\n{ex.Message}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
    }

}

internal class AdjustTableColumnSizeUndo(int thisTableId, int columnIndex, double oldPrimarySize, bool shiftWasOn, double oldSecondarySize, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;

            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                table.ColDefs[columnIndex].Width = new Avalonia.Controls.GridLength(oldPrimarySize, Avalonia.Controls.GridUnitType.Pixel);

                if (!shiftWasOn)
                {
                    if (columnIndex < table.ColDefs.Count - 1)
                        table.ColDefs[columnIndex + 1].Width = new Avalonia.Controls.GridLength(oldSecondarySize, Avalonia.Controls.GridUnitType.Pixel);
                }

                table.UpdateColAndRowPoints();
                table.UpdateCellParagraphSizes();

            }

            flowDoc.disableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at Col index: {columnIndex}\n{ex.Message}"); }

        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
    }

}

internal class AdjustTableRowSizeUndo(int thisTableId, int rowIndex, List<double> oldVertPaddings1, bool shiftWasOn, List<double> oldVertPaddings2, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;

            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {

                List<Cell> cellsToRepad = [.. table.Cells.Where(c => c.RowNo == rowIndex)];
                for (int cellno = 0; cellno < cellsToRepad.Count; cellno++)
                {
                    Cell cell = cellsToRepad[cellno];
                    double oldVPSplit1 = oldVertPaddings1[cellno] / 2; ;
                    cell.Padding = new Thickness(cell.Padding.Left, oldVPSplit1, cell.Padding.Right, oldVPSplit1);
                    cell.ResizeCellBlocks();
                }

                if (!shiftWasOn && rowIndex < table.RowDefs.Count - 1)
                {
                    cellsToRepad = [.. table.Cells.Where(c => c.RowNo == rowIndex + 1)];

                    for (int cellno = 0; cellno < cellsToRepad.Count; cellno++)
                    {
                        Cell cell = cellsToRepad[cellno];
                        double oldVPSplit2 = oldVertPaddings2[cellno] / 2; ;
                        cell.Padding = new Thickness(cell.Padding.Left, oldVPSplit2, cell.Padding.Right, oldVPSplit2);
                        cell.ResizeCellBlocks();
                    }
                }

                table.UpdateColAndRowPoints();
                table.UpdateCellParagraphSizes();

            }
            flowDoc.disableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at Row index: {rowIndex}\n{ex.Message}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
    }

}


internal class TableAlignmentChangeUndo(int tableId, HorizontalAlignment oldHAlign, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;

            if (flowDoc.GetBlockFromId(tableId) is Table table) 
                table.TableAlignment = oldHAlign;

            flowDoc.disableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at tableId: {tableId}\n{ex.Message}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
    }
}

internal class MergeCellsUndo(int tableId, Cell currentMergedCell, List<Cell> origMergedCellClones, List<int> origMergedCellCloneIndexes, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;
            flowDoc.disableRunTextUndo = true;

            if (flowDoc.GetBlockFromId(tableId) is Table table)
            {
                int cellIndex = table.Cells.IndexOf(currentMergedCell);
                table.Cells.Remove(currentMergedCell);

                for (int cloneIdx = 0; cloneIdx < origMergedCellClones.Count; cloneIdx++)
                {
                    table.Cells.Insert(origMergedCellCloneIndexes[cloneIdx], origMergedCellClones[cloneIdx]);
                    origMergedCellClones[cloneIdx].IsAttachedToDocument = true;
                    origMergedCellClones[cloneIdx].IsClonedCell = false;
                }

                table.UpdateCellParagraphSizes();

            }

            flowDoc.disableRunTextUndo = false;
            flowDoc.disableUndoStack = false;
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} for Merged Cells count: {origMergedCellClones.Count}"); }
        finally { flowDoc.disableUndoStack = false; flowDoc.disableRunTextUndo = false; }
    }

    public void PerformRedo()
    {
    }
}


