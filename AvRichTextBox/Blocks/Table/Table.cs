using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using System.Collections.ObjectModel;
using static AvRichTextBox.FlowDocument;

namespace AvRichTextBox;

public partial class Table : Block
{   
    public HorizontalAlignment TableAlignment 
    { 
        get; 
        set 
        {
            HorizontalAlignment oldHAlign = field;

            field = value;

            if (!IsAttachedToDocument) return;

            if (!MyFlowDoc.disableUndoStack)
                MyFlowDoc.Undos.Add(new TableAlignmentChangeUndo(this.Id, oldHAlign, MyFlowDoc));

            NotifyPropertyChanged(nameof(TableAlignment)); 
        } 
    } = HorizontalAlignment.Left;

    internal void CallRequestInvalidateVisual() { RequestInvalidateVisual = true; RequestInvalidateVisual = false; }
    internal bool RequestInvalidateVisual { get; set { field = value; NotifyPropertyChanged(nameof(RequestInvalidateVisual)); } } = false;

    internal delegate void ColDefsChangedHandler(Table sender);
    internal event ColDefsChangedHandler? ColDefsChanged;
    internal delegate void RowDefsChangedHandler(Table sender);
    internal event RowDefsChangedHandler? RowDefsChanged;

    internal ObservableCollection<Cell> Cells { get; } = [];
    public IEnumerable<Cell> GetCells => Cells;

    internal ColumnDefinitions ColDefs { get; set; } = [];
    internal RowDefinitions RowDefs { get; set; } = [];
    
    internal double Height { get; set { field = value; NotifyPropertyChanged(nameof(Height)); } } = 50;
    internal double Width { get; set { field = value; NotifyPropertyChanged(nameof(Width)); } } = 500;
    
    internal IBrush SelectionBrush = Brushes.LightSteelBlue;

    public Table() { }

    public Table(FlowDocument flowDoc) 
    {
        flowDoc.disableUndoStack = true;

        MyFlowDoc = flowDoc; 
        Id = ++FlowDocument.BlockIdCounter; 
        SelectionBrush = flowDoc.SelectionBrush;

        ColDefs.CollectionChanged += ColDefs_CollectionChanged;
        RowDefs.CollectionChanged += RowDefs_CollectionChanged;
        Cells.CollectionChanged += Cells_CollectionChanged;

        //flowDoc.disableUndoStack = false;

    }

    internal void UpdateColAndRowPoints()
    {

        Dispatcher.UIThread.Post(() =>
        {
            this.Width = ColDefs.Sum(cd => cd.Width.Value) + this.BorderThickness.Left + this.BorderThickness.Right;
            
            ColDefsChanged?.Invoke(this);
            RowDefsChanged?.Invoke(this);
            this.CallRequestInvalidateVisual();
        });

    }

    internal void UpdateCellParagraphSizes()
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (Cell cell in Cells)
                cell.ResizeCellBlocks();
        });
    }

    private void ColDefs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        ColDefsChanged?.Invoke(this);
        this.UpdateColAndRowPoints();
    }

    private void RowDefs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RowDefsChanged?.Invoke(this);
    }

    public Table(int cols, int rows, FlowDocument flowDoc) : this(flowDoc)
    {
        if (cols <= 0)
            throw new ArgumentOutOfRangeException(nameof(cols), cols, "Number of columns must be greater than zero.");
        if (rows <= 0)
            throw new ArgumentOutOfRangeException(nameof(rows), rows, "Number of rows must be greater than zero.");

        flowDoc.disableUndoStack = true;
       
        double eqWidth = Math.Truncate(Width / cols);
        double eqHeight = Math.Truncate(Height / rows);

        for (int colno = 0; colno < cols; colno++)
            ColDefs.Add(new ColumnDefinition(eqWidth, GridUnitType.Pixel));

        int cellno = 0;

        for (int rowno = 0; rowno < rows; rowno++)
        {
            RowDefs.Add(new RowDefinition(eqHeight, GridUnitType.Pixel));

            for (int colno = 0; colno < cols; colno++)
            {
                Paragraph newPar = new(flowDoc);
                                
                Cell newCell = new(this)
                {
                    ColNo = colno,
                    RowNo = rowno,
                    BorderThickness = new(1),
                    BorderBrush = Brushes.Black,
                    Padding = new(5)
                };

                Cells.Add(newCell);

                newPar.IsTableCellBlock = true;
                newPar.OwningTable = this;
                newPar.Inlines.Add(new EditableRun(""));  
                newPar.TextAlignment = TextAlignment.Center;
                newCell.CellBlocks.Add(newPar);
                                
                cellno++;
            }
        }

        Debug.WriteLine("total cells : " + Cells.Count);

        flowDoc.disableUndoStack = false;

        this.CallRequestInvalidateVisual();

    }

    private void Cells_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {        
        if (Cells.FirstOrDefault() is Cell c)
        {
            if (c.CellBlocks.FirstOrDefault() is Paragraph p)
                MyFlowDoc.UpdateBlockAndInlineStarts(p);
        }

        MyFlowDoc.AllParagraphs = [.. MyFlowDoc.GetAllParagraphs];

    }

    internal override Table PropertyClone()
    {
        MyFlowDoc.disableUndoStack = true;

        Table newTable = new(this.MyFlowDoc)
        {
            ColDefs = CloneColDefs(this.ColDefs),   // copied RowDefs and ColDefs must be cloned to be free of previously bound BindableGrid 
            RowDefs = CloneRowDefs(this.RowDefs),
            IsTableCellBlock = this.IsTableCellBlock,
            Height = this.Height,
            Width = this.Width,
            TableAlignment = this.TableAlignment,
            SelectionBrush = CloneBrush(this.SelectionBrush) ?? Brushes.LightSteelBlue,
            BorderBrush = CloneBrush(this.BorderBrush) ?? Brushes.Black,
            BorderThickness = this.BorderThickness,
            Background = this.Background,
            Margin = this.Margin,
            OwningTable = this.OwningTable,
            OwningCell = this.OwningCell
        };

        //OwningTable & OwningCell of Paragraphs are assigned in CellBlocks.CollectionChanged
        newTable.Cells.AddRange(this.Cells.Select(c => c.PropertyClone(newTable)));

        MyFlowDoc.disableUndoStack = false;

        return newTable;
    }

  
    internal override Table FullClone(bool keepId)
    {
        MyFlowDoc.disableUndoStack = true;

        Table newTable = new(this.MyFlowDoc)
        {
            ColDefs = CloneColDefs(this.ColDefs),   // copied RowDefs and ColDefs must be cloned to be free of previously bound BindableGrid 
            RowDefs = CloneRowDefs(this.RowDefs),
            IsTableCellBlock = this.IsTableCellBlock,
            Height = this.Height,
            Width = this.Width,
            TableAlignment = this.TableAlignment,
            SelectionBrush = CloneBrush(this.SelectionBrush) ?? Brushes.LightSteelBlue,
            BorderBrush = CloneBrush(this.BorderBrush) ?? Brushes.Black,
            BorderThickness = this.BorderThickness,
            Background = this.Background,
            Margin = this.Margin,
            OwningTable = this.OwningTable,
            OwningCell = this.OwningCell
        };

        if (keepId)
            newTable.Id = this.Id;

        //OwningTable & OwningCell of Paragraphs are assigned in CellBlocks.CollectionChanged
        newTable.Cells.AddRange(this.Cells.Select(c => c.FullClone(newTable, keepId)));

        MyFlowDoc.disableUndoStack = false;

        return newTable;

    }


    private static RowDefinitions CloneRowDefs(RowDefinitions source) 
    { 
        var result = new RowDefinitions(); 
        foreach (var r in source) { result.Add(new RowDefinition {  Height = r.Height, MinHeight = r.MinHeight, MaxHeight = r.MaxHeight }); }
        return result; 
    }

    private static ColumnDefinitions CloneColDefs(ColumnDefinitions source) 
    {
        var result = new ColumnDefinitions(); 
        foreach (var c in source) { result.Add(new ColumnDefinition { Width = c.Width, MinWidth = c.MinWidth, MaxWidth = c.MaxWidth }); }
        return result; 
    }

    public Cell? GetCellAt(int rowno,  int colno)
    {
        return Cells.FirstOrDefault(c=> c.RowNo == rowno && c.ColNo == colno);
    }

    public void InsertColumns(int insertColumnIndex, int count)
    {
        int origSelectionStart = MyFlowDoc.Selection.Start;
        List<int> addedCellIds = [];

        if (insertColumnIndex > ColDefs.Count) return;

        for (int insertCol = 0; insertCol < count;  insertCol++)
        {            
            double newWidth = ColDefs[insertColumnIndex].Width.Value;
            //double newWidth = ColDefs[insertColumnIndex].Width.Value / 2D;  // only halve if table is at some max size
            //ColDefs[insertColumnIndex].Width = new GridLength(newWidth, GridUnitType.Pixel);

            ColDefs.Insert(insertColumnIndex, new ColumnDefinition(newWidth, GridUnitType.Pixel));

            for (int rowno = RowDefs.Count - 1; rowno > -1; rowno--)
            {
                if (GetCellAt(rowno, insertColumnIndex) is Cell insertBeforeCell)
                {
                    int insertCellIndex = Cells.IndexOf(insertBeforeCell);

                    // shift all cells right one column from insertion point, *before* adding new cell at insertion point
                    for (int colno = ColDefs.Count - 1; colno >= insertColumnIndex; colno--)
                    {
                        if (GetCellAt(rowno, colno) is Cell rightCell)
                            rightCell.ColNo += 1;
                    }

                    //Create and insert new cell
                    Cell newCell = new(this)
                    {
                        OwningTable = this,
                        ColNo = insertColumnIndex,
                        RowNo = rowno,
                        BorderBrush = Cells[0].BorderBrush,
                    };

                    Cells.Insert(insertCellIndex, newCell);
                    newCell.IsAttachedToDocument = true;

                    addedCellIds.Add(newCell.Id);

                    Paragraph newPar = new(MyFlowDoc) { TextAlignment = TextAlignment.Center };
                    newPar.Inlines.Add(new EditableRun(""));
                    newCell.CellBlocks.Add(newPar);
                                        
                    Dispatcher.UIThread.Post(() =>
                    {
                        newPar.CallRequestTextLayoutInfoStart();
                        newPar.CallRequestTextLayoutInfoEnd();
                    });
                }
            }
        }

        MyFlowDoc.Undos.Add(new InsertColumnsUndo(this.Id, addedCellIds, insertColumnIndex, count, MyFlowDoc, origSelectionStart));
        this.CallRequestInvalidateVisual();

    }

    public void InsertRows(int insertRowIndex, int count)
    {
        if (insertRowIndex > RowDefs.Count) return;

        int origSelectionStart = MyFlowDoc.Selection.Start;
        List<int> addedCellIds = [];

        for (int insertRow = 0; insertRow < count; insertRow++)
        {
            double newHeight = RowDefs[insertRowIndex].Height.Value;

            RowDefs.Insert(insertRowIndex, new RowDefinition(newHeight, GridUnitType.Pixel));

            if (GetCellAt(insertRowIndex, 0) is Cell insertBeforeCell)
            {
                int insertCellIndex = Cells.IndexOf(insertBeforeCell);

                for (int rowno = RowDefs.Count - 1; rowno >= insertRowIndex; rowno--)
                {
                    // shift all cells down one column from insertion point, *before* adding new cell at insertion point
                    for (int colno = ColDefs.Count - 1; colno >= 0; colno--)
                    {
                        if (GetCellAt(rowno, colno) is Cell lowerCell)
                            lowerCell.RowNo += 1;
                    }
                }
                
                for (int colno = ColDefs.Count - 1; colno >= 0; colno--)
                {
                    //Create and insert new cell
                    Cell newCell = new(this)
                    {
                        OwningTable = this,
                        ColNo = colno,
                        RowNo = insertRowIndex,
                        BorderBrush = Cells[0].BorderBrush,
                    };

                    Cells.Insert(insertCellIndex, newCell);
                    newCell.IsAttachedToDocument = true;

                    addedCellIds.Add(newCell.Id);

                    Paragraph newPar = new(MyFlowDoc) { TextAlignment = TextAlignment.Center };
                    newPar.Inlines.Add(new EditableRun(""));
                    newCell.CellBlocks.Add(newPar);

                    Dispatcher.UIThread.Post(() =>
                    {
                        newPar.CallRequestTextLayoutInfoStart();
                        newPar.CallRequestTextLayoutInfoEnd();
                    });

                }
            }
        }
        MyFlowDoc.Undos.Add(new InsertRowsUndo(this.Id, addedCellIds, insertRowIndex, count, MyFlowDoc, origSelectionStart));
        this.CallRequestInvalidateVisual();
        
        //UpdateFlowDoc();

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
        MyFlowDoc.Undos.Add(new MergeCellsUndo(this.Id, firstCell, origMergedCellClones, origMergedCellCloneIndexes, MyFlowDoc));
                
    }

    public void MergeCellsDown(int rowNo, int colNo, int numberCellsToMerge = 1)
    {
        if (GetCellAt(rowNo, colNo) is not Cell firstCell) return;

        List<Cell> getMergeCells = [.. this.Cells.Where(c => c.ColNo == colNo && c.RowNo >= rowNo && c.RowNo <= rowNo + numberCellsToMerge)];
        List<Cell> origMergedCellClones = [.. getMergeCells.Select(cell => cell.FullClone(this, true))];
        List<int> origMergedCellCloneIndexes = getMergeCells.ConvertAll(cc=> this.Cells.IndexOf(cc));

        for (int i = 1; i <= numberCellsToMerge; i++)
        {
            if (GetCellAt(rowNo + 1, colNo) is Cell cellToMerge)
            {
                firstCell.RowSpan += cellToMerge.RowSpan;
                firstCell.CellBlocks.AddRange(cellToMerge.CellBlocks);
                cellToMerge.CellBlocks.Clear();
                Cells.Remove(cellToMerge);
            }
        }

        MyFlowDoc.Undos.Add(new MergeCellsUndo(this.Id, firstCell, origMergedCellClones, origMergedCellCloneIndexes, MyFlowDoc));
        
        
    }

    internal int GetParagraphCount()
    {
        int parCount = 0;
        foreach (var c in Cells) 
            parCount += c.CellBlocks.Count;
        return parCount;
    }

}



