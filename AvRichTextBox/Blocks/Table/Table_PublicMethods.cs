using Avalonia.Controls;
using Avalonia.Media;
using DynamicData;
using System.Data;

namespace AvRichTextBox;

public partial class Table
{   

    public Cell? GetCellAt(int rowno,  int colno) => Cells.FirstOrDefault(c=> c.RowNo == rowno && c.ColNo == colno);

    public void InsertColumnsAt(int insertColIndex, int count) { InsertColumns(insertColIndex, count, true); }

    [Obsolete("Use InsertColumnsAt(int insertRowIndex, int count) instead.")]
    public void InsertColumns(int insertColumnIndex, int count) { InsertColumns(insertColumnIndex, count, true); }

    internal void InsertColumns(int insertColumnIndex, int count, bool addUndo)
    {
        if (insertColumnIndex > ColDefs.Count + 1) return;

        int origSelectionStart = MyFlowDoc.Selection.Start;
        List<int> addedCellIds = [];

        int afterSelStart = origSelectionStart;

        for (int insertCol = 0; insertCol < count;  insertCol++)
        {            
            double newWidth = ColDefs[Math.Min(insertColumnIndex, ColDefs.Count - 1)].Width.Value;
            //double newWidth = ColDefs[insertColumnIndex].Width.Value / 2D;  // only halve if table is at some max size
            //ColDefs[insertColumnIndex].Width = new GridLength(newWidth, GridUnitType.Pixel);

            ColDefs.Insert(insertColumnIndex, new ColumnDefinition(newWidth, GridUnitType.Pixel));

            for (int rowno = 0; rowno < RowDefs.Count; rowno++)
            {
                if (GetCellAt(rowno, insertColumnIndex) is Cell addedCell)
                {
                    addedCellIds.Add(addedCell.Id);

                    if (addedCell.CellBlocks.FirstOrDefault() is Block firstCellBlock)
                    {
                        MyFlowDoc.UpdateTextRanges(firstCellBlock.StartInDoc, 1);

                        if (origSelectionStart >= firstCellBlock.StartInDoc)
                            afterSelStart += 1;
                    }
                }
            }
        }

        this.UpdateColAndRowPoints();
        MyFlowDoc.UpdateBlockAndInlineStarts(MyFlowDoc.Blocks.IndexOf(this));

        int selectionOffset = afterSelStart - origSelectionStart;

        if (addUndo)
            MyFlowDoc.Undos.Add(new InsertColumnsUndo(this.Id, addedCellIds, insertColumnIndex, count, MyFlowDoc, origSelectionStart));

        this.CallRequestInvalidateVisual();
                
        MyFlowDoc.Select(origSelectionStart + selectionOffset, 0);

        MyFlowDoc.Redos.Clear();
    }

    public void RemoveColumns(int removeColumnIndex, int count)
    {
        count = Math.Min(count, ColDefs.Count - removeColumnIndex);

        if (this.ColDefs.Count <= count) return;

        if (removeColumnIndex > ColDefs.Count - 1 || removeColumnIndex < 0) return;

        int origSelectionStart = MyFlowDoc.Selection.Start;
        List<(Cell, int)> removedCellClones = [];
        List<GridLength> removedColDefWidths = [];
        int removedTextChange = 0;

        for (int rowno = RowDefs.Count - 1; rowno >= 0; rowno--)
        {
            for (int removeColNo = removeColumnIndex + count - 1; removeColNo >= removeColumnIndex; removeColNo--)
            {
                if (GetCellAt(rowno, removeColNo) is Cell cellToRemove)
                {
                    removedCellClones.Insert(0, new(cellToRemove.FullClone(this, true), this.Cells.IndexOf(cellToRemove)));
                    removedTextChange += cellToRemove.CellBlocks.Sum(cb => cb.BlockLength);
                    //celltoRemove is actually removed in RowDefs.CollectionChanged event
                }
            }
        }

        for (int i = 0; i < count; i++)
        {
            removedColDefWidths.Add(ColDefs[removeColumnIndex].Width);
            ColDefs.RemoveAt(removeColumnIndex);
        }

        MyFlowDoc.Undos.Add(new RemoveColumnsUndo(this.Id, removedCellClones, removedColDefWidths, removeColumnIndex, count, MyFlowDoc, origSelectionStart));
        
        MyFlowDoc.UpdateBlockAndInlineStarts(MyFlowDoc.Blocks.IndexOf(this));
        MyFlowDoc.UpdateTextRanges(origSelectionStart, -removedTextChange);
        MyFlowDoc.UpdateCaret();

        this.UpdateColAndRowPoints();

        MyFlowDoc.Redos.Clear();
    }


    public void InsertRowsAt(int insertRowIndex, int count) { InsertRows(insertRowIndex, count, true); }

    [Obsolete("Use InsertRowsAt(int insertRowIndex, int count) instead.")]
    public void InsertRows(int insertRowIndex, int count) { InsertRows(insertRowIndex, count, true); }

    internal void InsertRows(int insertRowIndex, int count, bool addUndo)
    {
        if (insertRowIndex > RowDefs.Count + 1) return;

        int origSelectionStart = MyFlowDoc.Selection.Start;
        List<int> addedCellIds = [];

        for (int insertRowNo = 0; insertRowNo < count; insertRowNo++)
        {
            double newHeight = RowDefs[Math.Min(insertRowIndex, RowDefs.Count - 1)].Height.Value;

            RowDefinition newRowDef = new(newHeight, GridUnitType.Pixel);
            RowDefs.Insert(insertRowIndex, newRowDef);

            for (int colno = 0; colno < ColDefs.Count; colno++)
                if (GetCellAt(insertRowIndex, colno) is Cell addedCell)
                {
                    //if (GetCellAt(insertRowIndex - 1, colno) is Cell aboveCell)
                    //{  // keep cell formatting of original cell
                        //addedCell.CellBackground = aboveCell.CellBackground;
                        //addedCell.CellVerticalAlignment = aboveCell.CellVerticalAlignment;
                        //addedCell.ColSpan = aboveCell.ColSpan;
                        //addedCell.Padding= aboveCell.Padding;
                    //}
                    
                    addedCellIds.Add(addedCell.Id);
                }

        }

        if (addUndo)
            MyFlowDoc.Undos.Add(new InsertRowsUndo(this.Id, addedCellIds, insertRowIndex, count, MyFlowDoc, origSelectionStart));

        this.CallRequestInvalidateVisual();

        this.UpdateCellParagraphSizes();
        this.UpdateColAndRowPoints();

        int offset = count * ColDefs.Count;

        MyFlowDoc.UpdateBlockAndInlineStarts(MyFlowDoc.Blocks.IndexOf(this));
        MyFlowDoc.UpdateTextRanges(origSelectionStart, offset);

        MyFlowDoc.Select(origSelectionStart + offset, 0);
    }


    public void RemoveRows(int removeRowIndex, int count)
    {
        count = Math.Min(count, RowDefs.Count - removeRowIndex);

        if (this.RowDefs.Count <= count) return;

        if (removeRowIndex > RowDefs.Count - 1 || removeRowIndex < 0) return;

        int origSelectionStart = MyFlowDoc.Selection.Start;
        List<(Cell, int)> removedCellClones = [];
        List<double> removedRowDefMinHeights = [];
        int removedTextChange = 0;


        for (int removeRowNo = removeRowIndex + count - 1; removeRowNo >= removeRowIndex; removeRowNo--)
        {            
            for (int colno = ColDefs.Count - 1; colno >= 0; colno--)
            {
                if (GetCellAt(removeRowNo, colno) is Cell cellToRemove)
                {
                    removedCellClones.Insert(0, new(cellToRemove.FullClone(this, true), this.Cells.IndexOf(cellToRemove)));
                    removedTextChange += cellToRemove.CellBlocks.Sum(cb => cb.BlockLength);
                    //celltoRemove is actually removed in RowDefs.CollectionChanged event
                }
            }

            removedRowDefMinHeights.Add(RowDefs[removeRowNo].MinHeight);
            RowDefs.RemoveAt(removeRowNo);
        }

        MyFlowDoc.Undos.Add(new RemoveRowsUndo(this.Id, removedCellClones, removedRowDefMinHeights, removeRowIndex, count, MyFlowDoc, origSelectionStart));

        MyFlowDoc.UpdateBlockAndInlineStarts(MyFlowDoc.Blocks.IndexOf(this));
        MyFlowDoc.UpdateTextRanges(origSelectionStart, -removedTextChange);
        MyFlowDoc.UpdateCaret();

        
    }

    [Obsolete("Use MergeCellsRightAt(int rowNo, int colNo, int numberCellsToMerge = 1) instead.")]
    public void MergeCellsRight(int rowNo, int colNo, int numberCellsToMerge = 1) { MergeCellsRight(rowNo, colNo, true, numberCellsToMerge);  }

    public void MergeCellsRightAt(int rowNo, int colNo, int numberCellsToMerge = 1) { MergeCellsRight(rowNo, colNo, true, numberCellsToMerge); }

    internal void MergeCellsRight(int rowNo, int colNo, bool addUndo, int numberCellsToMerge = 1) 
    {
        if (GetCellAt(rowNo, colNo) is not Cell firstCell) return;

        int firstCellEndColNo = colNo + firstCell.ColSpan - 1;
        //Do not allow horizontal merging of mixed row-span cells or at end of row
        for (int i = 1; i <= numberCellsToMerge; i++)
        {
            Cell? checkCellToMerge = GetCellAt(rowNo, firstCellEndColNo + i);
            if (checkCellToMerge == null || checkCellToMerge.RowSpan != firstCell.RowSpan)
                return;
        }

        List<Cell> getMergeCells = [.. this.Cells.Where(c => c.RowNo == rowNo && c.ColNo >= colNo && c.ColNo <= firstCellEndColNo + numberCellsToMerge)];
        List<Cell> origMergedCellClones = [.. getMergeCells.Select(cell => cell.FullClone(this, true))];
        List<int> origMergedCellCloneIndexes = getMergeCells.ConvertAll(cc => this.Cells.IndexOf(cc));

        for (int i = 1; i <= numberCellsToMerge; i++)
        {
            if (GetCellAt(rowNo, firstCellEndColNo + i) is Cell cellToMerge)
            {
                firstCell.ColSpan += cellToMerge.ColSpan;
                firstCell.CellBlocks.AddRange(cellToMerge.CellBlocks);
                cellToMerge.CellBlocks.Clear();
                Cells.Remove(cellToMerge);
            }
        }

        if (addUndo)
            MyFlowDoc.Undos.Add(new MergeCellsUndo(this.Id, firstCell.Id, origMergedCellClones, origMergedCellCloneIndexes, MyFlowDoc));


        RemoveUnnecessaryColumns();


        this.UpdateColAndRowPoints();
        MyFlowDoc.UpdateCaret();

    }

    internal void MergeCellsDown(int rowNo, int colNo, bool addUndo, int numberCellsToMerge = 1)
    {
        if (GetCellAt(rowNo, colNo) is not Cell firstCell) return;

        int firstCellEndRowNo = rowNo + firstCell.RowSpan - 1;

        //Do not allow vertical merging of mixed col-span cells 
        for (int i = 1; i <= numberCellsToMerge; i++)
        {
            Cell? checkCellToMerge = GetCellAt(firstCellEndRowNo + i, colNo);
            if (checkCellToMerge == null || checkCellToMerge.ColSpan != firstCell.ColSpan)
            return;
        } 

        List<Cell> getMergeCells = [.. this.Cells.Where(c => c.ColNo == colNo && c.RowNo >= rowNo && c.RowNo <= firstCellEndRowNo + numberCellsToMerge)];
        List<Cell> origMergedCellClones = [.. getMergeCells.Select(cell => cell.FullClone(this, true))];
        List<int> origMergedCellCloneIndexes = getMergeCells.ConvertAll(cc => this.Cells.IndexOf(cc));

        for (int i = 1; i <= numberCellsToMerge; i++)
        {
            for (int mergeRightNo = 0; mergeRightNo < firstCell.ColSpan; mergeRightNo++)
            {
                if (GetCellAt(firstCellEndRowNo + i, colNo + mergeRightNo) is Cell cellToMerge)
                {
                    firstCell.RowSpan += cellToMerge.RowSpan;
                    firstCell.CellBlocks.AddRange(cellToMerge.CellBlocks);
                    cellToMerge.CellBlocks.Clear();
                    Cells.Remove(cellToMerge);
                }
            }
        }

        if (addUndo)
            MyFlowDoc.Undos.Add(new MergeCellsUndo(this.Id, firstCell.Id, origMergedCellClones, origMergedCellCloneIndexes, MyFlowDoc));

        this.UpdateColAndRowPoints();

    }

    public void MergeCellsDownAt(int rowNo, int colNo, int numberCellsToMerge = 1) { MergeCellsDown(rowNo, colNo, true, numberCellsToMerge); }

    [Obsolete("Use MergeCellsDownAt(int rowNo, int colNo, int numberCellsToMerge = 1) instead.")]
    public void MergeCellsDown(int rowNo, int colNo, int numberCellsToMerge = 1) { MergeCellsDown(rowNo, colNo, true, numberCellsToMerge); }

  
    public void SplitCellVertical(int rowNo, int colNo, int numberTargetRows = 2)
    {
        if (GetCellAt(rowNo, colNo) is not Cell firstCell) return;

        int addedRowsIndexStart = -1; 
        int addedRowsCount = 0;
        int noRequiredRowsToAdd = numberTargetRows - firstCell.RowSpan;
        List<int> addedCellIds = [];

        if (noRequiredRowsToAdd > 0)
        {
            addedRowsIndexStart = rowNo + 1;
            addedRowsCount = numberTargetRows - 1;

            InsertRows(addedRowsIndexStart, addedRowsCount, addUndo: false);
            MyFlowDoc.UpdateBlockAndInlineStarts(this.StartInDoc);

            DisableUndoStack = true;

            for (int colIterate = 0; colIterate < ColDefs.Count; colIterate++)
            {
                for (int nextDownNo = 1; nextDownNo < numberTargetRows; nextDownNo++)
                {
                    if (GetCellAt(rowNo + nextDownNo, colIterate) is Cell nextDownCell)
                    {
                        if (colIterate == colNo)
                        {
                            nextDownCell.BorderThickness = firstCell.BorderThickness;
                            nextDownCell.BorderBrush = firstCell.BorderBrush;
                            nextDownCell.CellBackground = firstCell.CellBackground;
                            nextDownCell.CellVerticalAlignment = firstCell.CellVerticalAlignment;
                            nextDownCell.Padding = firstCell.Padding;
                            addedCellIds.Add(nextDownCell.Id);
                        }
                        else
                        {
                            MyFlowDoc.UpdateTextRanges(nextDownCell.StartInDoc, -1);
                            nextDownCell.CellBlocks.Clear();
                        }
                    }
                }

                if (colIterate != colNo)
                {  // if a row was added, other col cells need to be merged with the new row
                    int mergeFromRowNo = rowNo;
                    Cell rowToIncreaseSpan = null!;

                    while (rowToIncreaseSpan is null)
                    {
                        rowToIncreaseSpan = GetCellAt(mergeFromRowNo, colIterate)!;
                        if (rowToIncreaseSpan != null)
                        {
                            if (mergeFromRowNo < 0 || (mergeFromRowNo < rowNo - 1 && rowToIncreaseSpan.RowSpan == 1))
                                rowToIncreaseSpan = null!;
                            break;
                        }
                        mergeFromRowNo -= 1;

                    }

                    if (rowToIncreaseSpan != null)
                        MergeCellsDown(mergeFromRowNo, colIterate, false, noRequiredRowsToAdd);
                    
                }

            }
        }
        else
        { // split in place, no row additions necessary

            DisableUndoStack = true;

            int thisCellIndex = Cells.Count;

            for (int i = numberTargetRows - 1; i >=1; i--)
            {
                Cell newCell = new()
                {
                    ColNo = colNo,
                    RowNo = rowNo + i,
                    BorderThickness = firstCell.BorderThickness,
                    BorderBrush = firstCell.BorderBrush,
                    CellBackground = firstCell.CellBackground,
                    CellVerticalAlignment = firstCell.CellVerticalAlignment,
                    Padding = firstCell.Padding,
                    OwningTable = this,
                };

                Paragraph newPar = new() { MyFlowDoc = this.MyFlowDoc, IsTableCellBlock = true, OwningTableId = this.Id, OwningCellId = newCell.Id, TextAlignment = TextAlignment.Center };
                newPar.Inlines.Add(new EditableRun(""));
                newCell.CellBlocks.Add(newPar);


                if (Cells.FirstOrDefault(c => c.RowNo == rowNo + i && c.ColNo == colNo + 1) is Cell nextRightCell)
                    thisCellIndex = Cells.IndexOf(nextRightCell);
                else if (Cells.FirstOrDefault(c => c.RowNo == rowNo + i + 1 && c.ColNo == 0) is Cell firstCellNextRow)
                    thisCellIndex = Cells.IndexOf(firstCellNextRow);

                Cells.Insert(thisCellIndex, newCell);

                addedCellIds.Insert(0, newCell.Id);

                MyFlowDoc.UpdateTextRanges(newCell.StartInDoc, 1);
                                
                newCell.IsAttachedToDocument = this.IsAttachedToDocument;

                firstCell.RowSpan -= 1;

            }
            
        }

        MyFlowDoc.Undos.Add(new SplitCellVerticalUndo(this.Id, firstCell.Id, addedRowsIndexStart, addedRowsCount, addedCellIds, MyFlowDoc));
        MyFlowDoc.Redos.Clear();

        this.UpdateColAndRowPoints();

        MyFlowDoc.UpdateCaret();
        DisableUndoStack = false;


    }


    public void SplitCellHorizontal(int rowNo, int colNo, int numberTargetCols = 2)
    {
        if (GetCellAt(rowNo, colNo) is not Cell firstCell) return;

        int addedColsIndexStart = -1;
        int addedColsCount = 0;
        List<int> addedCellIds = [];
        int noRequiredColsToAdd = numberTargetCols - firstCell.ColSpan;

        if (noRequiredColsToAdd > 0)
        {
            addedColsIndexStart = colNo + 1;
            addedColsCount = numberTargetCols - 1;

            InsertColumns(colNo + 1, noRequiredColsToAdd, addUndo: false); // adds default cells

            //return;

            DisableUndoStack = true;

            for (int rowIterate = 0; rowIterate < RowDefs.Count; rowIterate++)
            {
                int mergeFromColNo = colNo;

                for (int nextRightNo = 1; nextRightNo < numberTargetCols; nextRightNo++)
                {
                    if (GetCellAt(rowIterate, colNo + nextRightNo) is Cell nextRightCell)
                    {
                        if (rowIterate == rowNo)
                        {
                            nextRightCell.BorderThickness = firstCell.BorderThickness;
                            nextRightCell.BorderBrush = firstCell.BorderBrush;
                            nextRightCell.CellBackground = firstCell.CellBackground;
                            nextRightCell.CellVerticalAlignment = firstCell.CellVerticalAlignment;
                            nextRightCell.Padding = firstCell.Padding;
                            addedCellIds.Add(nextRightCell.Id);
                        }
                        else
                        {
                            nextRightCell.CellBlocks.Clear();  // clear so no text is added during merge
                            ////for (int wlno = colNo + nextRightNo - 1; wlno >=0; wlno--)
                            //for (int wlno = colNo; wlno >=0; wlno--)
                            //{
                            //    if (GetCellAt(rowIterate, wlno) is Cell cmg)
                            //    {
                            //        mergeFromColNo = wlno;
                            //        noRequiredColsToAdd += (cmg.ColSpan - 1);
                            //        break;
                            //    }
                            //}
                        }
                            
                    }
                }

                if (rowIterate != rowNo)
                {   // if a col was added, other row cells need to be merged with the new column
                    Cell cellToIncreaseSpan = null!;
                    while (cellToIncreaseSpan is null)
                    {
                        cellToIncreaseSpan = GetCellAt(rowIterate, mergeFromColNo)!;
                        if (cellToIncreaseSpan != null)
                        {
                            if (mergeFromColNo < 0 || (mergeFromColNo < colNo - 1 && cellToIncreaseSpan.ColSpan == 1))
                                cellToIncreaseSpan = null!;
                            break;
                        }
                        mergeFromColNo -= 1;
                    }

                    if (cellToIncreaseSpan != null)
                        MergeCellsRight(rowIterate, mergeFromColNo, addUndo: false, noRequiredColsToAdd);
                }


            }
        }
        else
        {  // split in place, no column additions necessary
            DisableUndoStack = true;

            int thisCellIndex = Cells.IndexOf(firstCell);

            for (int i = numberTargetCols - 1; i >= 1; i--)
            {
                Cell newCell = new()
                {
                    //ColNo = colNo + firstCell.ColSpan - i,
                    ColNo = colNo + i,
                    RowNo = rowNo,
                    BorderThickness = firstCell.BorderThickness,
                    BorderBrush = firstCell.BorderBrush,
                    CellBackground = firstCell.CellBackground,
                    CellVerticalAlignment = firstCell.CellVerticalAlignment,
                    ColSpan = 1,
                    Padding = firstCell.Padding,
                    OwningTable = this,
                };
               
                Paragraph newPar = new() { MyFlowDoc = this.MyFlowDoc, IsTableCellBlock = true, OwningTableId = this.Id, OwningCellId = newCell.Id, TextAlignment = TextAlignment.Center };
                newPar.Inlines.Add(new EditableRun(""));
                newCell.CellBlocks.Add(newPar);

                Cells.Insert(thisCellIndex + 1, newCell);

                addedCellIds.Add(newCell.Id);

                newCell.IsAttachedToDocument = this.IsAttachedToDocument;

                firstCell.ColSpan -= 1;

            }
        }

        MyFlowDoc.Undos.Add(new SplitCellHorizontalUndo(this.Id, firstCell.Id, addedColsIndexStart, addedColsCount, addedCellIds, MyFlowDoc));
        
        MyFlowDoc.Redos.Clear();

        this.UpdateCellParagraphSizes();
        this.UpdateColAndRowPoints();

        MyFlowDoc.UpdateCaret();
        DisableUndoStack = false;

    }

    internal void RemoveUnnecessaryColumns()
    {
        for (int cdefno = ColDefs.Count - 1; cdefno >= 0; cdefno--)
        {
            ColumnDefinition thisCD = ColDefs[cdefno];
            if (!Cells.Any(c => c.ColNo == cdefno))
            {
                foreach (Cell c in Cells)
                {
                    if (c.ColNo + c.ColSpan - 1 == cdefno)
                        c.ColSpan -= 1;
                }

                ColDefs.RemoveAt(cdefno);

            }
        }
    }


}



