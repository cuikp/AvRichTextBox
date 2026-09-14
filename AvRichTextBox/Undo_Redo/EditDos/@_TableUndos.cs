using Avalonia.Controls;
using Avalonia.Layout;
using DynamicData;

namespace AvRichTextBox;

internal class TableAlignmentChangeUndo(int tableId, HorizontalAlignment oldHAlign, HorizontalAlignment newHAlign, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.GetBlockFromId(tableId) is Table table)
                table.TableAlignment = oldHAlign;

            DisableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at tableId: {tableId}\n{ex.Message}"); }
        finally { DisableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.GetBlockFromId(tableId) is Table table)
                table.TableAlignment = newHAlign;

            DisableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at tableId: {tableId}\n{ex.Message}"); }
        finally { DisableUndoStack = false; }
    }
}

internal class InsertColumnsUndo(int thisTableId, List<int> insertedCellIds, int insertedColumnIdx, int insertedCount, FlowDocument flowDoc, int undoSelStart, int redoSelStart) : IEditDo
{
    public int UndoEditOffset => -insertedCount;
    public bool UpdateTextRanges => true;

    readonly List<(Cell, int)> insertedCells = [];
    readonly List<ColumnDefinition> insertedColDefs = [];

    public void PerformUndo()
    {        
        DisableUndoStack = true;
     
        try
        {           
            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                insertedCells.Clear();
                foreach (int cellId in insertedCellIds)
                {
                    if (table.Cells.FirstOrDefault(c => c.Id == cellId) is Cell cellToRemove)
                    {
                        insertedCells.Add(new(cellToRemove, table.Cells.IndexOf(cellToRemove)));
                        table.Cells.Remove(cellToRemove);
                    }
                }

                insertedColDefs.Clear();
                for (int i = 0; i < insertedCount; i++)
                {
                    ColumnDefinition removedCDef = table.ColDefs[insertedColumnIdx];
                    table.ColDefs.Remove(removedCDef);
                    insertedColDefs.Add(removedCDef);
                }
                    

                for (int rowno = 0; rowno < table.RowDefs.Count; rowno++)
                {
                    for (int colno = insertedColumnIdx + insertedCount; colno < table.ColDefs.Count + insertedCount; colno++)
                    {
                        if (table.GetCellAt(rowno, colno) is Cell shiftCell)
                            shiftCell.ColNo -= insertedCount;
                    }
                }

                table.Width = table.ColDefs.Sum(cd => cd.Width.Value);

                PostUpdate(undoSelStart);
                
            }
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at Col index: {insertedColumnIdx}\n{ex.Message}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        DisableUndoStack = true;

        if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
        {
            try
            {
                for (int i = 0; i < insertedColDefs.Count; i++)
                    table.ColDefs.Insert(insertedColumnIdx, insertedColDefs[i]);

                for (int rowno = 0; rowno < table.RowDefs.Count; rowno++)
                {
                    for (int colno = table.ColDefs.Count - 1; colno >= insertedColumnIdx; colno--)
                    {
                        if (table.GetCellAt(rowno, colno) is Cell shiftCell)
                            shiftCell.ColNo += insertedCount;
                    }
                }

                for (int clonedCellNo = insertedCells.Count - 1; clonedCellNo >= 0; clonedCellNo--)
                {
                    Cell insertCell = insertedCells[clonedCellNo].Item1;
                    int insertIdx = insertedCells[clonedCellNo].Item2;
                    table.Cells.Insert(insertIdx, insertCell);
                    
                }


                table.Width = table.ColDefs.Sum(cd => cd.Width.Value);


                PostUpdate(redoSelStart);
                
            }
            catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at Col index: {insertedColumnIdx}\n{ex.Message}"); }
            finally { DisableUndoStack = false; }
        }
    }
    

    private void PostUpdate(int updateFromPos)
    {
        flowDoc.Select(updateFromPos, 0);
        DisableUndoStack = false;
    }
}

internal class InsertRowsUndo(int thisTableId, List<int> insertedCellIds, int insertedRowIdx, int insertedCount, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int UndoEditOffset => -insertedCount;
    //public int UndoEditOffsetFrom => undoSelStart - undoEditOffset;
    //private int undoEditOffset = 0;
    public bool UpdateTextRanges => true;

    readonly List<(Cell, int)> insertedCells = [];
    readonly List<RowDefinition> insertedRowDefs = [];

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack =  true;

            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                insertedCells.Clear();
                foreach (int cellId in insertedCellIds)
                {
                    if (table.Cells.FirstOrDefault(c => c.Id == cellId) is Cell cellToRemove)
                    {
                        insertedCells.Add(new(cellToRemove, table.Cells.IndexOf(cellToRemove)));
                        table.Cells.Remove(cellToRemove);
                    }
                }

                insertedRowDefs.Clear();
                for (int i = 0; i < insertedCount; i++)
                {
                    RowDefinition removedRDef = table.RowDefs[insertedRowIdx];
                    table.RowDefs.Remove(removedRDef);
                    insertedRowDefs.Add(removedRDef);
                }
                                               
                for (int rowno = insertedRowIdx + insertedCount; rowno < table.RowDefs.Count + insertedCount; rowno++)
                {
                    for (int colno = 0; colno < table.ColDefs.Count; colno++)
                    {
                        if (table.GetCellAt(rowno, colno) is Cell shiftCell)
                            shiftCell.RowNo -= insertedCount;
                    }
                }

                PostUpdate(origSelectionStart);
                                                
            }
        }
        catch (Exception ex) { Debug.WriteLine($"Failed InsertColumnsUndo at Col index: {insertedRowIdx}\n{ex.Message}"); }
        finally { DisableUndoStack =  false; }

    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                for (int rowno = table.RowDefs.Count - 1; rowno >= insertedRowIdx; rowno--)
                {
                    for (int colno = 0; colno < table.ColDefs.Count; colno++)
                    {
                        if (table.GetCellAt(rowno, colno) is Cell shiftCell)
                            shiftCell.RowNo += insertedCount;
                    }
                }

                for (int i = 0; i < insertedRowDefs.Count; i++)
                    table.RowDefs.Insert(insertedRowIdx, insertedRowDefs[i]);

                for (int clonedCellNo = insertedCells.Count - 1; clonedCellNo >= 0; clonedCellNo--)
                {
                    Cell insertCell = insertedCells[clonedCellNo].Item1;
                    int insertIdx = insertedCells[clonedCellNo].Item2;
                    table.Cells.Insert(insertIdx, insertCell);
                }

                PostUpdate(origSelectionStart + insertedCount * table.ColDefs.Count * 2);
            }
        }
        catch (Exception ex) { Debug.WriteLine($"Failed InsertColumnsUndo at Col index: {insertedRowIdx}\n{ex.Message}"); }
        finally { DisableUndoStack = false; }

    }

    private void PostUpdate(int updateFromPos)
    {
        flowDoc.Select(updateFromPos, 0);
        DisableUndoStack = false;
    }
}

internal class AdjustTableColumnSizeUndo(int thisTableId, int columnIndex, double oldPrimarySize, double newPrimarySize, bool shiftWasOn, double oldSecondarySize, double newSecondarySize, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        SetSizes(oldPrimarySize, shiftWasOn, oldSecondarySize);
    }

    public void PerformRedo()
    {
        SetSizes(newPrimarySize, shiftWasOn, newSecondarySize);
    }

    private void SetSizes(double setPrimarySize, bool shiftWasOn, double setSecondarySize)
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                table.ColDefs[columnIndex].Width = new GridLength(setPrimarySize, GridUnitType.Pixel);

                if (!shiftWasOn)
                {
                    if (columnIndex < table.ColDefs.Count - 1)
                        table.ColDefs[columnIndex + 1].Width = new GridLength(setSecondarySize, GridUnitType.Pixel);
                }

                table.UpdateColAndRowPoints();
                table.UpdateCellParagraphSizes();

            }

            DisableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at Col index: {columnIndex}\n{ex.Message}"); }

        finally { DisableUndoStack = false; }
    }
}

internal class AdjustTableRowSizeUndo(
    int thisTableId, 
    int rowIndex, 
    double oldRowHeight1,
    double newRowHeight1, 
    bool shiftWasOn, 
    double oldRowHeight2, 
    double newRowHeight2, 
    FlowDocument flowDoc) 
    : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        SetRowHeights(oldRowHeight1, shiftWasOn, oldRowHeight2);
    }

    public void PerformRedo()
    {
        SetRowHeights(newRowHeight1, shiftWasOn, newRowHeight2);
    }

    private void SetRowHeights(double setRowHeight1, bool shiftWasOn, double setRowHeight2)
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                table.RowDefs[rowIndex].MinHeight = setRowHeight1;
                table.RowDefs[rowIndex].Height = new GridLength(setRowHeight1, GridUnitType.Pixel);

                if (!shiftWasOn && rowIndex < table.RowDefs.Count - 1)
                {
                    table.RowDefs[rowIndex + 1].MinHeight = setRowHeight2;
                    table.RowDefs[rowIndex + 1].Height = new GridLength(setRowHeight2, GridUnitType.Pixel);
                }

                table.UpdateColAndRowPoints();
                table.UpdateCellParagraphSizes();
            }

            DisableUndoStack = true;

        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at Row index: {rowIndex}\n{ex.Message}"); }
        finally { DisableUndoStack = false; }
    }

 }


internal class MergeCellsUndo(int tableId, int startMergedCellId, List<Cell> origMergedCellClones, List<int> origMergedCellCloneIndexes, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;
    Cell keepStartMergedCell = null!;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(tableId) is Table table && table.Cells.FirstOrDefault(c => c.Id == startMergedCellId) is Cell startMergedCell)
            {
                DisableUndoStack = true;

                this.keepStartMergedCell = startMergedCell;

                int cellIndex = table.Cells.IndexOf(startMergedCell);
                table.Cells.Remove(startMergedCell);

                for (int cloneIdx = 0; cloneIdx < origMergedCellClones.Count; cloneIdx++)
                {
                    table.Cells.Insert(origMergedCellCloneIndexes[cloneIdx], origMergedCellClones[cloneIdx]);
                    origMergedCellClones[cloneIdx].IsAttachedToDocument = true;
                    origMergedCellClones[cloneIdx].IsClonedCell = false;
                }

                table.UpdateCellParagraphSizes();
                table.UpdateColAndRowPoints();
                DisableUndoStack = false;

            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} for Merged Cells count: {origMergedCellClones.Count}"); }
        finally { DisableUndoStack =  false;  }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(tableId) is Table table)
            {
                DisableUndoStack = true;

                table.Cells.RemoveMany(origMergedCellClones);

                int cellIndex = origMergedCellCloneIndexes[0];
                table.Cells.Insert(cellIndex, keepStartMergedCell);

                table.UpdateCellParagraphSizes();
                table.UpdateColAndRowPoints();
                DisableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} for Merged Cells count: {origMergedCellClones.Count}"); }
        finally { DisableUndoStack = false; }

    }


}


