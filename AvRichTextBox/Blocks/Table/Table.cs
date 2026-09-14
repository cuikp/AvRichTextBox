using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using System.Collections.ObjectModel;
using System.Data;
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

            if (!DisableUndoStack)
                MyFlowDoc.Undos.Add(new TableAlignmentChangeUndo(this.Id, oldHAlign, field, MyFlowDoc));

            NotifyPropertyChanged(nameof(TableAlignment));

            MyFlowDoc?.UpdateCaret();
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

    public ColumnDefinitions ColDefs 
    { 
        get; 
        set 
        { 
            field?.CollectionChanged -= ColDefs_CollectionChanged; 
            field = value; 
            field.CollectionChanged += ColDefs_CollectionChanged; 
            foreach (ColumnDefinition cdef in field) AddDefaultCellToNewColDef(ColDefs.IndexOf(cdef)); 
        } 
    } = [];
    
    public RowDefinitions RowDefs 
    { 
        get; 
        set 
        { 
            field?.CollectionChanged -= RowDefs_CollectionChanged; 
            field = value; 
            field.CollectionChanged += RowDefs_CollectionChanged; 
            foreach (RowDefinition rdef in field) AddDefaultCellToNewRowDef(RowDefs.IndexOf(rdef)); 
        } 
    } = [];


    internal double Height { get; set { field = value; NotifyPropertyChanged(nameof(Height)); } } = 50;
    internal double Width { get; set { field = value; NotifyPropertyChanged(nameof(Width)); } } = 500;
    
    internal IBrush SelectionBrush  = Brushes.LightSteelBlue;

    public Table() 
    {
        Id = ++FlowDocument.BlockIdCounter;

        Cells.CollectionChanged += Cells_CollectionChanged;

        ColDefs.CollectionChanged += ColDefs_CollectionChanged;
        RowDefs.CollectionChanged += RowDefs_CollectionChanged;
    }

    /// <summary>
    /// Retained for backwards compatibility. Prefer <see cref="Table()"/>.
    /// </summary>
    [Obsolete("Use the parameterless Table() constructor instead.")]
    public Table(FlowDocument flowDoc) : this(){ }


    public Table(int noCols, int noRows, FlowDocument flowDoc) : this()
    {
        MyFlowDoc = flowDoc;
        if (noCols <= 0)
            throw new ArgumentOutOfRangeException(nameof(noCols), noCols, "Number of columns must be greater than zero.");
        if (noRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(noRows), noRows, "Number of rows must be greater than zero.");

        DisableUndoStack = true;

        double eqWidth = Math.Truncate(Width / noCols);
        double eqHeight = Math.Truncate(Height / noRows);

        // Col/Row definitions must be set anew to trigger addition of default cells (because Table is not yet attached to document)
        string colDefString = string.Join(',', Enumerable.Repeat(eqWidth, noCols));
        ColDefs = new(colDefString);

        string rowDefString = string.Join(',', Enumerable.Repeat(eqHeight, noRows));
        RowDefs = new(rowDefString);

     
        Debug.WriteLine("total cells : " + Cells.Count);

        DisableUndoStack = false;

        this.CallRequestInvalidateVisual();

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

    private void AddDefaultCellToNewRowDef(int rowDefIndex)
    {
        DisableUndoStack = true;

        // Insert default cells
        for (int colno = 0; colno < ColDefs.Count; colno++)
        {
            int insertNo = rowDefIndex * ColDefs.Count + colno;

            Cell newCell = new()
            {
                ColNo = colno,
                RowNo =  rowDefIndex,
                BorderThickness = new(1),
                BorderBrush = Brushes.Black,
                Padding = new(5),
                OwningTable = this
            };

            Paragraph newPar = new() { IsTableCellBlock = true, OwningTable = this, OwningCell = newCell, TextAlignment = TextAlignment.Center };
            newPar.Inlines.Add(new EditableRun(""));
                        
            newCell.CellBlocks.Add(newPar);

            Cells.Insert(insertNo, newCell);

            newCell.IsAttachedToDocument = this.IsAttachedToDocument;
            newCell.ResizeCellBlocks();

        }

        this.UpdateColAndRowPoints();
        MyFlowDoc?.UpdateBlockAndInlineStarts(MyFlowDoc.Blocks.IndexOf(this));
        
        DisableUndoStack = false;

    }

    private void AddDefaultCellToNewColDef(int cdefIndex)
    {
        DisableUndoStack = true;

        for (int rowno = 0; rowno < this.RowDefs.Count; rowno++)
        {
            int insertNo = rowno * ColDefs.Count + cdefIndex;

            Cell newCell = new()
            {
                ColNo = cdefIndex,
                RowNo = rowno,
                BorderThickness = new(1),
                BorderBrush = Brushes.Black,
                Padding = new(5),
                OwningTable = this,
            };

            Paragraph newPar = new() { IsTableCellBlock = true, OwningTable = this, OwningCell = newCell, TextAlignment = TextAlignment.Center };
            newPar.Inlines.Add(new EditableRun(""));

            newCell.CellBlocks.Add(newPar);
            
            Cells.Insert(insertNo, newCell);

            newCell.IsAttachedToDocument = this.IsAttachedToDocument;
            newCell.ResizeCellBlocks();
        }

        MyFlowDoc?.UpdateBlockAndInlineStarts(MyFlowDoc.Blocks.IndexOf(this));
        this.UpdateColAndRowPoints();

        DisableUndoStack = false;
    }

    // revise this to remove Cells from Table.Cells, when ColDefs become removable! $$$$$$$$$$$$$$$$$
    private void ColDefs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (!this.IsAttachedToDocument || DisableUndoStack) return;

        if (e.NewItems != null)
        {
            int numAdded = e.NewItems.Count;

            foreach (ColumnDefinition cdef in e.NewItems)
            {
                int colIndex = ColDefs.IndexOf(cdef);

                for (int rowno = RowDefs.Count - 1; rowno >= 0; rowno--)
                {
                    // shift all cells right one column from insertion point, *before* adding new cell at insertion point
                    for (int colno = ColDefs.Count - numAdded; colno >= colIndex; colno--)
                    {
                        if (GetCellAt(rowno, colno) is Cell lowerCell)
                            lowerCell.ColNo += 1;
                    }
                }

                AddDefaultCellToNewColDef(colIndex);

            }

            this.CallRequestInvalidateVisual();
        }

        ColDefsChanged?.Invoke(this);

    }

    // revise this to remove Cells from Table.Cells, when RowDefs become removable! $$$$$$$$$$$$$$$$$
    private void RowDefs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (!this.IsAttachedToDocument || DisableUndoStack) return;

        if (e.NewItems != null)
        {
            int numAdded = e.NewItems.Count;
            foreach (RowDefinition rdef in e.NewItems)
            {
                int rowIndex = RowDefs.IndexOf(rdef);

                for (int rowno = RowDefs.Count - numAdded; rowno >= rowIndex; rowno--)
                {
                    // shift all cells down one row from insertion point, *before* adding new cell at insertion point
                    for (int colno = ColDefs.Count - 1; colno >= 0; colno--)
                    {
                        if (GetCellAt(rowno, colno) is Cell lowerCell)
                            lowerCell.RowNo += 1;
                    }
                }
                     
                AddDefaultCellToNewRowDef(rowIndex);

            }

            this.CallRequestInvalidateVisual();

        }

        RowDefsChanged?.Invoke(this);

    }

    bool _internalChange = false;

    private void Cells_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_internalChange) return;
        

        if (Cells.FirstOrDefault() is Cell c)
        {
            if (c.CellBlocks.FirstOrDefault() is Paragraph p)
                MyFlowDoc?.UpdateBlockAndInlineStarts(p);
        }

        MyFlowDoc?.AllParagraphs = [.. MyFlowDoc.GetAllParagraphs];

        if (e.NewItems != null)
        {
            foreach (Cell cell in e.NewItems)
            {
                // Allow re-defining of Cells in Xaml - which will already exist when <Table/> is defined
                if (Cells.FirstOrDefault(c=> c.RowNo == cell.RowNo && c.ColNo == cell.ColNo && c != cell) is Cell existingCell)
                {
                    _internalChange = true;
                    try
                    {
                        int removeCellIndex = Cells.IndexOf(existingCell);
                        int currentCellIndex = Cells.IndexOf(cell);
                        Cells.Move(currentCellIndex, removeCellIndex);
                        Cells.Remove(existingCell);
                        if (cell.CellBlocks.Count == 0)
                            AddDefaultParagraph(cell.CellBlocks);
                    }
                    finally { _internalChange = false; }
                }

                cell.OwningTable = this;
                cell.IsAttachedToDocument = this.IsAttachedToDocument;
            }
        }
                
    }

    internal override Table PropertyClone()
    {
        DisableUndoStack =  true;

        Table newTable = new()
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
            OwningCell = this.OwningCell,
            MyFlowDoc = this.MyFlowDoc
        };

        //OwningTable & OwningCell of Paragraphs are assigned in CellBlocks.CollectionChanged
        newTable.Cells.AddRange(this.Cells.Select(c => c.PropertyClone(newTable)));

        DisableUndoStack =  false;

        return newTable;
    }

  
    internal override Table FullClone(bool keepId)
    {
        DisableUndoStack =  true;

        Table newTable = new()
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
            OwningCell = this.OwningCell,
            MyFlowDoc = this.MyFlowDoc
        };

        if (keepId)
            newTable.Id = this.Id;

        //OwningTable & OwningCell of Paragraphs are assigned in CellBlocks.CollectionChanged
        newTable.Cells.AddRange(this.Cells.Select(c => c.FullClone(newTable, keepId)));

        DisableUndoStack =  false;

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
                    
                    if (afterSelStart >= addedCell.CellBlocks.First().StartInDoc)
                    {
                        MyFlowDoc.UpdateTextRanges(afterSelStart, 2);
                        afterSelStart += 2;
                    }
                }
            }
        }

        MyFlowDoc.UpdateBlockAndInlineStarts(MyFlowDoc.Blocks.IndexOf(this));

        
        MyFlowDoc.Undos.Add(new InsertColumnsUndo(this.Id, addedCellIds, insertColumnIndex, count, MyFlowDoc, origSelectionStart, afterSelStart));
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

            RowDefinition newRowDef = new (newHeight, GridUnitType.Pixel);
            RowDefs.Insert(insertRowIndex, newRowDef);

            for (int colno = 0; colno < ColDefs.Count; colno++)
                if (GetCellAt(insertRowIndex, colno) is Cell addedCell)
                    addedCellIds.Add(addedCell.Id);
        }

        MyFlowDoc.Undos.Add(new InsertRowsUndo(this.Id,  addedCellIds, insertRowIndex, count, MyFlowDoc, origSelectionStart));
        this.CallRequestInvalidateVisual();

        MyFlowDoc.UpdateBlockAndInlineStarts(MyFlowDoc.Blocks.IndexOf(this));
        MyFlowDoc.UpdateTextRanges(this.StartInDoc, count * ColDefs.Count * 2);

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

    internal int GetParagraphCount()
    {
        int parCount = 0;
        foreach (var c in Cells) 
            parCount += c.CellBlocks.Count;
        return parCount;
    }

   

}



