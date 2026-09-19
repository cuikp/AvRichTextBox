using Avalonia.Controls;
using DocumentFormat.OpenXml.Drawing;
using DynamicData;
using System.Data;

namespace AvRichTextBox;

public partial class Table
{   

    public Cell? GetCellAt(int rowno,  int colno)
    {
        return Cells.FirstOrDefault(c=> c.RowNo == rowno && c.ColNo == colno);
    }

    public void InsertColumns(int insertColumnIndex, int count)
    {
        int origSelectionStart = MyFlowDoc.Selection.Start;
        List<int> addedCellIds = [];

        if (insertColumnIndex > ColDefs.Count) return;

        int afterSelStart = origSelectionStart;

        for (int insertCol = 0; insertCol < count;  insertCol++)
        {            
            double newWidth = ColDefs[insertColumnIndex].Width.Value;
            //double newWidth = ColDefs[insertColumnIndex].Width.Value / 2D;  // only halve if table is at some max size
            //ColDefs[insertColumnIndex].Width = new GridLength(newWidth, GridUnitType.Pixel);

            ColDefs.Insert(insertColumnIndex, new ColumnDefinition(newWidth, GridUnitType.Pixel));

            for (int rowno = 0; rowno < RowDefs.Count; rowno++)
            {
                if (GetCellAt(rowno, insertColumnIndex) is Cell addedCell)
                {
                    addedCellIds.Add(addedCell.Id);

                    MyFlowDoc.UpdateTextRanges(addedCell.CellBlocks.First().StartInDoc, 1);

                    if (origSelectionStart >= addedCell.CellBlocks.First().StartInDoc)
                        afterSelStart += 1;
                }
            }
        }

        MyFlowDoc.UpdateBlockAndInlineStarts(MyFlowDoc.Blocks.IndexOf(this));

        int selectionOffset = afterSelStart - origSelectionStart;

        MyFlowDoc.Undos.Add(new InsertColumnsUndo(this.Id, addedCellIds, insertColumnIndex, count, MyFlowDoc, origSelectionStart));
        this.CallRequestInvalidateVisual();

        MyFlowDoc.UpdateTextRanges(origSelectionStart, selectionOffset);
        MyFlowDoc.Select(origSelectionStart + selectionOffset, 0);

    }

    public void RemoveColumns(int removeColumnIndex, int count)
    {
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
            removedColDefWidths.Add(ColDefs[removeColumnIndex + i].Width);
            ColDefs.RemoveAt(removeColumnIndex);
        }

        MyFlowDoc.Undos.Add(new RemoveColumnsUndo(this.Id, removedCellClones, removedColDefWidths, removeColumnIndex, count, MyFlowDoc, origSelectionStart, removedTextChange));

        MyFlowDoc.UpdateBlockAndInlineStarts(MyFlowDoc.Blocks.IndexOf(this));
        MyFlowDoc.UpdateTextRanges(origSelectionStart, -removedTextChange);
        MyFlowDoc.UpdateCaret();


    }


    public void InsertRows(int insertRowIndex, int count)
    {
        if (insertRowIndex > RowDefs.Count) return;

        int origSelectionStart = MyFlowDoc.Selection.Start;
        List<int> addedCellIds = [];

        for (int insertRowNo = 0; insertRowNo < count; insertRowNo++)
        {
            double newHeight = RowDefs[insertRowIndex].Height.Value;

            RowDefinition newRowDef = new (newHeight, GridUnitType.Pixel);
            RowDefs.Insert(insertRowIndex, newRowDef);

            for (int colno = 0; colno < ColDefs.Count; colno++)
                if (GetCellAt(insertRowIndex, colno) is Cell addedCell)
                    addedCellIds.Add(addedCell.Id);
        }

        MyFlowDoc.Undos.Add(new InsertRowsUndo(this.Id,  addedCellIds, insertRowIndex, count, MyFlowDoc, origSelectionStart));
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
        if (removeRowIndex > RowDefs.Count - 1 || removeRowIndex < 0) return;

        int origSelectionStart = MyFlowDoc.Selection.Start;
        List<(Cell, int)> removedCellClones = [];
        List<GridLength> removedRowDefHeights = [];
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

            removedRowDefHeights.Add(RowDefs[removeRowNo].Height);
            RowDefs.RemoveAt(removeRowNo);
        }

        MyFlowDoc.Undos.Add(new RemoveRowsUndo(this.Id, removedCellClones, removedRowDefHeights, removeRowIndex, count, MyFlowDoc, origSelectionStart, removedTextChange));

        MyFlowDoc.UpdateBlockAndInlineStarts(MyFlowDoc.Blocks.IndexOf(this));
        MyFlowDoc.UpdateTextRanges(origSelectionStart, -removedTextChange);
        MyFlowDoc.UpdateCaret();

    }



    public void MergeCellsRight(int rowNo, int colNo, int numberCellsToMerge = 1)
    {

        if (GetCellAt(rowNo, colNo) is not Cell firstCell) return;

        List<Cell> getMergeCells = [.. this.Cells.Where(c => c.RowNo == rowNo && c.ColNo >= colNo && c.ColNo <= colNo + numberCellsToMerge)];
        List<Cell> origMergedCellClones = [.. getMergeCells.Select(cell => cell.FullClone(this, true))];
        List<int> origMergedCellCloneIndexes = getMergeCells.ConvertAll(cc=> this.Cells.IndexOf(cc));

        for (int i = 1; i <= numberCellsToMerge; i++)
        {
            if (GetCellAt(rowNo, colNo + i) is Cell cellToMerge)
            {
                firstCell.ColSpan += cellToMerge.ColSpan;
                firstCell.CellBlocks.AddRange(cellToMerge.CellBlocks);
                cellToMerge.CellBlocks.Clear();
                Cells.Remove(cellToMerge);
            }
        }
        MyFlowDoc.Undos.Add(new MergeCellsUndo(this.Id, firstCell.Id, origMergedCellClones, origMergedCellCloneIndexes, MyFlowDoc));

        this.UpdateColAndRowPoints();

    }

    public void MergeCellsDown(int rowNo, int colNo, int numberCellsToMerge = 1)
    {
        if (GetCellAt(rowNo, colNo) is not Cell firstCell) return;

        List<Cell> getMergeCells = [.. this.Cells.Where(c => c.ColNo == colNo && c.RowNo >= rowNo && c.RowNo <= rowNo + numberCellsToMerge)];
        List<Cell> origMergedCellClones = [.. getMergeCells.Select(cell => cell.FullClone(this, true))];
        List<int> origMergedCellCloneIndexes = getMergeCells.ConvertAll(cc=> this.Cells.IndexOf(cc));

        for (int i = 1; i <= numberCellsToMerge; i++)
        {
            if (GetCellAt(rowNo + i, colNo) is Cell cellToMerge)
            {
                firstCell.RowSpan += cellToMerge.RowSpan;
                firstCell.CellBlocks.AddRange(cellToMerge.CellBlocks);
                cellToMerge.CellBlocks.Clear();
                Cells.Remove(cellToMerge);
            }
        }

        MyFlowDoc.Undos.Add(new MergeCellsUndo(this.Id, firstCell.Id, origMergedCellClones, origMergedCellCloneIndexes, MyFlowDoc));

        this.UpdateColAndRowPoints();
        
    }

  

}



