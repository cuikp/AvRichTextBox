using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using DynamicData;

namespace AvRichTextBox;

internal class TableAlignmentChangeUndo(int tableId, HorizontalAlignment oldHAlign, HorizontalAlignment newHAlign, FlowDocument flowDoc) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

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


internal class InsertRowsUndo(int thisTableId, List<int> insertedCellIds, int insertedRowIdx, int insertedRowCount, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int EditOffset { get; set; } = 0;
    public bool UpdateTextRanges => true;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    readonly List<(Cell, int)> insertedCells = [];
    readonly List<RowDefinition> insertedRowDefs = [];

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table && table.GetCellAt(insertedRowIdx, 0) is Cell firstCell)
            {
                UpdateTextRangesFromCharIdx = firstCell.StartInDoc;

                DisableUndoStack = true;

                insertedCells.Clear();
                foreach (int cellId in insertedCellIds)
                {
                    if (table.Cells.FirstOrDefault(c => c.Id == cellId) is Cell cellToRemove)
                    {
                        insertedCells.Add(new(cellToRemove.FullClone(table, true), table.Cells.IndexOf(cellToRemove)));
                        table.Cells.Remove(cellToRemove);
                    }
                }

                insertedRowDefs.Clear();
                for (int i = 0; i < insertedRowCount; i++)
                {
                    RowDefinition removedRDef = table.RowDefs[insertedRowIdx];
                    table.RowDefs.Remove(removedRDef);
                    insertedRowDefs.Add(removedRDef);
                }
                                               
                for (int rowno = insertedRowIdx + insertedRowCount; rowno < table.RowDefs.Count + insertedRowCount; rowno++)
                {
                    for (int colno = 0; colno < table.ColDefs.Count; colno++)
                    {
                        if (table.GetCellAt(rowno, colno) is Cell shiftCell)
                            shiftCell.RowNo -= insertedRowCount;
                    }
                }

                EditOffset = -insertedRowCount * table.ColDefs.Count;
                UpdateTextRangesFromCharIdx = table.StartInDoc; // temporary

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
            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                DisableUndoStack = true;

                for (int rowno = table.RowDefs.Count - 1; rowno >= insertedRowIdx; rowno--)
                {
                    for (int colno = 0; colno < table.ColDefs.Count; colno++)
                    {
                        if (table.GetCellAt(rowno, colno) is Cell shiftCell)
                            shiftCell.RowNo += insertedRowCount;
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


                EditOffset = insertedRowCount * table.ColDefs.Count;
                UpdateTextRangesFromCharIdx = table.StartInDoc; // temporary
                PostUpdate(origSelectionStart + insertedRowCount * table.ColDefs.Count);
                

                DisableUndoStack = false;
            }
        }
        catch (Exception ex) { Debug.WriteLine($"Failed InsertColumnsUndo at Col index: {insertedRowIdx}\n{ex.Message}"); }
        finally { DisableUndoStack = false; }

    }

    private void PostUpdate(int restoreStart)
    {
        DisableUndoStack = false;

        Dispatcher.UIThread.Post(() => { flowDoc.Select(restoreStart, 0);  });
        
    }
}

internal class RemoveRowsUndo( int thisTableId, List<(Cell, int)> removedCellClones, List<double> removedRowDefHeights, int removedRowIdx, int removedRowCount, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => true;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    private int removedTextLength = 0;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                DisableUndoStack = true;
                removedTextLength = 0;

                for (int rowno = table.RowDefs.Count - 1; rowno >= removedRowIdx; rowno--)
                {
                    for (int colno = 0; colno < table.ColDefs.Count; colno++)
                    {
                        if (table.GetCellAt(rowno, colno) is Cell shiftCell)
                            shiftCell.RowNo += removedRowCount;
                    }
                }

                for (int i = 0; i < removedRowCount; i++)
                    table.RowDefs.Insert(removedRowIdx, new RowDefinition() { MinHeight = removedRowDefHeights[i] });

                for (int clonedCellNo = 0; clonedCellNo < removedCellClones.Count; clonedCellNo++)
                {
                    Cell insertCell = removedCellClones[clonedCellNo].Item1;
                    int insertIdx = removedCellClones[clonedCellNo].Item2;
                    table.Cells.Insert(insertIdx, insertCell);
                    removedTextLength += insertCell.CellBlocks.Sum(cb => cb.BlockLength);
                }

           
                EditOffset = removedTextLength; 
                UpdateTextRangesFromCharIdx = origSelectionStart;
                
                PostUpdate(origSelectionStart);

            }
        }
        catch (Exception ex) { Debug.WriteLine($"Failed RemoveRowsUndo at Row index: {removedRowIdx}\n{ex.Message}"); }
        finally { DisableUndoStack =  false; }

    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                DisableUndoStack = true;

                for (int clonedCellNo = 0; clonedCellNo < removedCellClones.Count; clonedCellNo++)
                {
                    if (table.Cells.FirstOrDefault(c=> c.Id == removedCellClones[clonedCellNo].Item1.Id) is Cell removeCell)
                    {
                        removedCellClones[clonedCellNo] = new(removeCell, removedCellClones[clonedCellNo].Item2);
                        table.Cells.Remove(removeCell);
                    }
                }

                for (int i = 0; i < removedRowCount; i++)
                    table.RowDefs.RemoveAt(removedRowIdx);

                for (int rowno = removedRowIdx; rowno < table.RowDefs.Count; rowno++)
                {
                    for (int colno = 0; colno < table.ColDefs.Count; colno++)
                    {
                        if (table.GetCellAt(rowno + removedRowCount, colno) is Cell shiftCell)
                            shiftCell.RowNo -= removedRowCount;
                    }
                }

                EditOffset = -removedTextLength;
                UpdateTextRangesFromCharIdx = origSelectionStart;

                int selStart = origSelectionStart;
                if (table.GetCellAt(removedRowIdx, 0) is Cell nextCell)
                    selStart = nextCell.StartInDoc;
                PostUpdate(selStart);

            }

        }
        catch (Exception ex) { Debug.WriteLine($"Failed RemoveRowsRedo at Row index: {removedRowIdx}\n{ex.Message}"); }
        finally { DisableUndoStack = false; }

    }

    private void PostUpdate(int restoreStart)
    {
        DisableUndoStack = false;

        Dispatcher.UIThread.Post(() => { flowDoc.Select(restoreStart, 0); });

    }

}

internal class InsertColumnsUndo(int thisTableId, List<int> insertedCellIds, int insertedColumnIdx, int insertedColCount, FlowDocument flowDoc, int undoSelStart) : IEditDo
{
    public int EditOffset { get; set; } = 0;
    public bool UpdateTextRanges => false;   // must update text ranges selectively while iterating
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    int redoSelStart = 0;
    readonly List<(Cell, int)> insertedCells = [];
    readonly List<ColumnDefinition> insertedColDefs = [];
    public bool DoNextUndo => false; public bool DoNextRedo => false;

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                insertedCells.Clear();

                redoSelStart = undoSelStart;

                foreach (int cellId in insertedCellIds)
                {
                    if (table.Cells.FirstOrDefault(c => c.Id == cellId) is Cell cellToRemove)
                    {
                        int startThisCell = cellToRemove.CellBlocks.First().StartInDoc;
                        insertedCells.Add(new(cellToRemove.FullClone(table, true), table.Cells.IndexOf(cellToRemove)));

                        flowDoc.UpdateTextRanges(startThisCell, -1);

                        if (undoSelStart >= startThisCell)
                            redoSelStart += 1;

                        table.Cells.Remove(cellToRemove);
                    }
                }

                insertedColDefs.Clear();
                for (int i = 0; i < insertedColCount; i++)
                {
                    ColumnDefinition removedCDef = table.ColDefs[insertedColumnIdx];
                    insertedColDefs.Add(removedCDef);
                    table.ColDefs.Remove(removedCDef);
                }


                for (int rowno = 0; rowno < table.RowDefs.Count; rowno++)
                {
                    for (int colno = insertedColumnIdx + insertedColCount; colno < table.ColDefs.Count + insertedColCount; colno++)
                    {
                        if (table.GetCellAt(rowno, colno) is Cell shiftCell)
                            shiftCell.ColNo -= insertedColCount;
                    }
                }

                table.Width = table.ColDefs.Sum(cd => cd.Width.Value);

                PostUpdate(undoSelStart);

            }
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at Col index: {insertedColumnIdx}\n{ex.Message}"); }
        finally { DisableUndoStack = false; }
    }

    public void PerformRedo()
    {
        if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
        {
            DisableUndoStack = true;

            try
            {
                for (int rowno = 0; rowno < table.RowDefs.Count; rowno++)
                {
                    for (int colno = table.ColDefs.Count - 1; colno >= insertedColumnIdx; colno--)
                    {
                        if (table.GetCellAt(rowno, colno) is Cell shiftCell)
                            shiftCell.ColNo += insertedColCount;
                    }
                }

                for (int i = 0; i < insertedColDefs.Count; i++) 
                    table.ColDefs.Insert(insertedColumnIdx, insertedColDefs[i]);

                //Debug.WriteLine("cells: " + string.Join('\n', table.Cells.ToList().ConvertAll(c => c.RowNo + ":" + c.ColNo)));

                for (int clonedCellNo = insertedCells.Count - 1; clonedCellNo >= 0; clonedCellNo--)
                {
                    Cell insertCell = insertedCells[clonedCellNo].Item1;
                    int insertIdx = insertedCells[clonedCellNo].Item2;
                    table.Cells.Insert(insertIdx, insertCell);

                    flowDoc.UpdateTextRanges(insertCell.CellBlocks.First().StartInDoc, 1);
                }

                table.Width = table.ColDefs.Sum(cd => cd.Width.Value);


                PostUpdate(redoSelStart);

            }
            catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at Col index: {insertedColumnIdx}\n{ex.Message}"); }
            finally { DisableUndoStack = false; }
        }
    }

    private void PostUpdate(int selStart)
    {
        // no textrange update so no editoffset 

        flowDoc.Select(selStart, 0);
        DisableUndoStack = false;
    }
}

internal class RemoveColumnsUndo( int thisTableId, List<(Cell, int)> removedCellClones, List<GridLength> removedColDefWidths, int removedColIdx, int removedColCount, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;   // must update text ranges selectively while iterating
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    private int removedTextLength = 0;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                DisableUndoStack = true;
                removedTextLength = 0;

                for (int rowno = 0; rowno < table.RowDefs.Count; rowno++)
                {
                    for (int colno = table.ColDefs.Count - 1; colno >= removedColIdx; colno--)
                    {
                        if (table.GetCellAt(rowno, colno) is Cell shiftCell)
                            shiftCell.ColNo += removedColCount;
                    }
                }

                for (int i = 0; i < removedColCount; i++)
                    table.ColDefs.Insert(removedColIdx + i, new ColumnDefinition(removedColDefWidths[i]));

                for (int clonedCellNo = 0; clonedCellNo < removedCellClones.Count; clonedCellNo++)
                {
                    Cell insertCell = removedCellClones[clonedCellNo].Item1;
                    int insertIdx = removedCellClones[clonedCellNo].Item2;
                    table.Cells.Insert(insertIdx, insertCell);
                    
                    flowDoc.UpdateTextRanges(insertCell.CellBlocks.First().StartInDoc, insertCell.CellBlocks.Sum(cb=> cb.BlockLength));
                }

                table.UpdateColAndRowPoints();
                table.UpdateCellParagraphSizes();

                EditOffset = removedTextLength;
                UpdateTextRangesFromCharIdx = origSelectionStart; 
                
                PostUpdate(origSelectionStart);

            }
        }
        catch (Exception ex) { Debug.WriteLine($"Failed RemoveRowsUndo at Row index: {removedColIdx}\n{ex.Message}"); }
        finally { DisableUndoStack =  false; }

    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == thisTableId) is Table table)
            {
                DisableUndoStack = true;

                for (int clonedCellNo = 0; clonedCellNo < removedCellClones.Count; clonedCellNo++)
                {
                    if (table.Cells.FirstOrDefault(c => c.Id == removedCellClones[clonedCellNo].Item1.Id) is Cell removeCell)
                    {
                        removedCellClones[clonedCellNo] = new(removeCell, removedCellClones[clonedCellNo].Item2);
                        int startThisCell = removeCell.CellBlocks.First().StartInDoc;
                        table.Cells.Remove(removeCell);

                        flowDoc.UpdateTextRanges(startThisCell, -removeCell.CellBlocks.Sum(cb => cb.BlockLength));

                    }
                }

                for (int i = 0; i < removedColCount; i++)
                    table.ColDefs.RemoveAt(removedColIdx);

                for (int rowno = 0; rowno < table.RowDefs.Count; rowno++) 
                {
                    for (int colno = removedColIdx; colno < table.ColDefs.Count; colno++)
                    {
                        if (table.GetCellAt(rowno, colno + removedColCount) is Cell shiftCell)
                            shiftCell.ColNo -= removedColCount;
                    }
                }

                table.UpdateColAndRowPoints();
                table.UpdateCellParagraphSizes();

                
                int selStart = origSelectionStart;
                if (table.GetCellAt(removedColIdx, 0) is Cell nextCell)
                    selStart = nextCell.StartInDoc;
                PostUpdate(selStart);

            }

        }
        catch (Exception ex) { Debug.WriteLine($"Failed RemoveRowsRedo at Row index: {removedColIdx}\n{ex.Message}"); }
        finally { DisableUndoStack = false; }

    }

    private void PostUpdate(int restoreStart)
    {
        DisableUndoStack = false;

        Dispatcher.UIThread.Post(() => { flowDoc.Select(restoreStart, 0); });

    }

}

internal class AdjustTableColumnSizeUndo(int thisTableId, int columnIndex, double oldPrimarySize, double newPrimarySize, bool shiftWasOn, double oldSecondarySize, double newSecondarySize, FlowDocument flowDoc) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

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
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

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
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    Cell keepStartMergedCell = null!;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(tableId) is Table table && table.Cells.FirstOrDefault(c => c.Id == startMergedCellId) is Cell startMergedCell)
            {
                DisableUndoStack = true;

                keepStartMergedCell = startMergedCell;

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

                for (int mergedCellNo = 0; mergedCellNo < origMergedCellClones.Count; mergedCellNo++)
                {
                    if (table.Cells.FirstOrDefault(c=> c.Id == origMergedCellClones[mergedCellNo].Id) is Cell getClonedCell)
                    {
                        origMergedCellClones[mergedCellNo] = getClonedCell;
                        table.Cells.Remove(getClonedCell);
                    }
                }
                

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


