using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
    
    /// <summary>
    /// Enumerates the cells in this table.
    /// Do not cast this collection to a mutable collection type to add/remove Cells, instead use Add/RemoveColumns() and Add/RemoveRows()
    /// </summary>
    public IEnumerable<Cell> GetCells => Cells;
    
    public ColumnDefinitions ColDefs 
    { 
        get; 
        init 
        { 
            field?.CollectionChanged -= ColDefs_CollectionChanged; 
            field = value; 
            field.CollectionChanged += ColDefs_CollectionChanged; 
            foreach (ColumnDefinition cdef in field) AddDefaultCellsToNewColDef(ColDefs.IndexOf(cdef)); 
        } 
    } = [];
    
    public RowDefinitions RowDefs 
    { 
        get; 
        init
        { 
            field?.CollectionChanged -= RowDefs_CollectionChanged; 
            field = value; 
            field.CollectionChanged += RowDefs_CollectionChanged; 
            foreach (RowDefinition rdef in field) AddDefaultCellsToNewRowDef(RowDefs.IndexOf(rdef)); 
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

    private void AddDefaultCellsToNewRowDef(int rowDefIndex)
    {
        bool keepDisableUndoStack = DisableUndoStack;
        DisableUndoStack = true;

        int baseInsertIdx = Cells.Count;

        if (Cells.FirstOrDefault(c => c.RowNo == rowDefIndex + 1) is Cell insertBeforeCell)
            baseInsertIdx = Cells.IndexOf(insertBeforeCell);

        // Insert default cells
        for (int colno = 0; colno < ColDefs.Count; colno++)
        {
            int insertNo = baseInsertIdx + colno;

            Cell newCell = new()
            {
                ColNo = colno,
                RowNo =  rowDefIndex,
                BorderThickness = new(1),
                BorderBrush = Brushes.Black,
                Padding = new(5),
                OwningTable = this
            };

            Paragraph newPar = new() { MyFlowDoc = this.MyFlowDoc, IsTableCellBlock = true, OwningTableId = this.Id, OwningCellId = newCell.Id, TextAlignment = TextAlignment.Center };
            newPar.Inlines.Add(new EditableRun(""));
                        
            newCell.CellBlocks.Add(newPar);

            Cells.Insert(insertNo, newCell);

            newCell.IsAttachedToDocument = this.IsAttachedToDocument;
            newCell.ResizeCellBlocks();

        }

        this.UpdateColAndRowPoints();
        MyFlowDoc?.UpdateBlockAndInlineStarts(MyFlowDoc.Blocks.IndexOf(this));

        DisableUndoStack = keepDisableUndoStack;

    }

    private void AddDefaultCellsToNewColDef(int colDefIndex)
    {
        bool keepDisableUndoStack = DisableUndoStack;
        DisableUndoStack = true;

        int insertIdx = Cells.Count;

        for (int rowno = 0; rowno < this.RowDefs.Count; rowno++)
        {        
            if (Cells.FirstOrDefault(c => c.RowNo == rowno && c.ColNo == colDefIndex + 1) is Cell insertBeforeCell)
                insertIdx = Cells.IndexOf(insertBeforeCell);
            else if(Cells.FirstOrDefault(c => c.RowNo == rowno + 1 && c.ColNo == 0) is Cell firstCellNextRow)
                insertIdx = Cells.IndexOf(firstCellNextRow);

            Cell newCell = new()
            {
                ColNo = colDefIndex,
                RowNo = rowno,
                BorderThickness = new(1),
                BorderBrush = Brushes.Black,
                Padding = new(5),
                OwningTable = this,
            };

            Paragraph newPar = new() { MyFlowDoc = this.MyFlowDoc, IsTableCellBlock = true, OwningTableId = this.Id, OwningCellId = newCell.Id, TextAlignment = TextAlignment.Center };
            newPar.Inlines.Add(new EditableRun(""));

            newCell.CellBlocks.Add(newPar);
            
            Cells.Insert(insertIdx, newCell);

            newCell.IsAttachedToDocument = this.IsAttachedToDocument;
            newCell.ResizeCellBlocks();
        }

        MyFlowDoc?.UpdateBlockAndInlineStarts(MyFlowDoc.Blocks.IndexOf(this));
        this.UpdateColAndRowPoints();

        DisableUndoStack = keepDisableUndoStack;
    }

    private void ColDefs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
                        if (GetCellAt(rowno, colno) is Cell rightCell)
                            rightCell.ColNo += 1;
                    }
                }

                AddDefaultCellsToNewColDef(colIndex);

            }

            this.CallRequestInvalidateVisual();
        }

        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            int numRemoved = e.OldItems!.Count;
            int oldIndex = e.OldStartingIndex + numRemoved - 1;

            
            for (int itemNo = e.OldItems.Count - 1; itemNo >= 0; itemNo--)
            {
                if (e.OldItems[itemNo] is ColumnDefinition cdef)
                {
                    // shift all cells up one row from insertion point, *after* removing old cell at remove point
                    for (int rowno = RowDefs.Count - 1; rowno >= 0; rowno--)
                    {
                        if (GetCellAt(rowno, oldIndex) is Cell removeCell)
                            Cells.Remove(removeCell);

                        for (int colno = ColDefs.Count; colno >= oldIndex + 1; colno--)
                        {
                            if (GetCellAt(rowno, colno) is Cell rightCell)
                                rightCell.ColNo -= 1;
                        }
                    }
                }

                oldIndex--;
                this.CallRequestInvalidateVisual();
            }
        }



        ColDefsChanged?.Invoke(this);

    }

    private void RowDefs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
                                
                AddDefaultCellsToNewRowDef(rowIndex);

            }

            this.CallRequestInvalidateVisual();

        }

        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            int numRemoved = e.OldItems!.Count;
            int oldIndex = e.OldStartingIndex + numRemoved - 1;

            //foreach (RowDefinition rdef in e.OldItems)
            for (int itemNo = e.OldItems.Count - 1; itemNo >= 0; itemNo--)
            {
                if (e.OldItems[itemNo] is RowDefinition rdef)
                {
                    // shift all cells up one row from insertion point, *after* removing old cell at remove point
                    for (int colno = ColDefs.Count - 1; colno >= 0; colno--)
                    {
                        if (GetCellAt(oldIndex, colno) is Cell removeCell)
                            Cells.Remove(removeCell);

                        for (int rowno = RowDefs.Count; rowno >= oldIndex + 1; rowno--)
                        {
                            if (GetCellAt(rowno, colno) is Cell lowerCell)
                                lowerCell.RowNo -= 1;
                        }
                    }
                }

                oldIndex--;
                this.CallRequestInvalidateVisual();
            }
        }


        RowDefsChanged?.Invoke(this);

    }

    bool _internalChange = false;

    private void Cells_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
                    catch (Exception ex) { Debug.WriteLine($"Error trying to redefine cell: {existingCell.ColNo}, {ex.Message}"); }
                    finally { _internalChange = false; }
                }

                cell.OwningTable = this;
                cell.IsAttachedToDocument = this.IsAttachedToDocument;
            }
        }
                
    }

    internal override Table PropertyClone()
    {
        bool keepDisableUndoStack = DisableUndoStack;
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
            OwningTableId = this.OwningTableId,
            OwningCellId = this.OwningCellId,
            MyFlowDoc = this.MyFlowDoc
        };

        //OwningTable & OwningCell of Paragraphs are assigned in CellBlocks.CollectionChanged
        newTable.Cells.AddRange(this.Cells.Select(c => c.PropertyClone(newTable)));

        DisableUndoStack = keepDisableUndoStack;

        return newTable;
    }

  
    internal override Table FullClone(bool keepId)
    {
        bool keepDisableUndoStack = DisableUndoStack;
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
            OwningTableId = this.OwningTableId,
            OwningCellId = this.OwningCellId,
            MyFlowDoc = this.MyFlowDoc
        };

        if (keepId)
            newTable.Id = this.Id;

        //OwningTable & OwningCell of Paragraphs are assigned in CellBlocks.CollectionChanged
        newTable.Cells.AddRange(this.Cells.Select(c => c.FullClone(newTable, keepId)));

        DisableUndoStack = keepDisableUndoStack;

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

    internal int GetParagraphCount()
    {
        int parCount = 0;
        foreach (var c in Cells) 
            parCount += c.CellBlocks.Count;
        return parCount;
    }

   

}



