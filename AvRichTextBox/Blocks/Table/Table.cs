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
    public Thickness BorderThickness { get; set { field = value; NotifyPropertyChanged(nameof(BorderThickness)); } } = new(1);
    public IBrush BorderBrush { get; set { field = value; NotifyPropertyChanged(nameof(BorderBrush)); } } = Brushes.Black;

    internal void CallRequestInvalidateVisual() { RequestInvalidateVisual = true; RequestInvalidateVisual = false; }
    internal bool RequestInvalidateVisual { get; set { field = value; NotifyPropertyChanged(nameof(RequestInvalidateVisual)); } } = false;

    public delegate void ColDefsChangedHandler(Table sender);
    public event ColDefsChangedHandler? ColDefsChanged;
    public delegate void RowDefsChangedHandler(Table sender);
    public event RowDefsChangedHandler? RowDefsChanged;


    public ObservableCollection<Cell> Cells { get; set; } = [];
    public ColumnDefinitions ColDefs { get; set; } = [];
    public RowDefinitions RowDefs { get; set; } = [];
    public double Height { get; set { field = value; NotifyPropertyChanged(nameof(Height)); } } = 50;
    public double Width { get; set { field = value; NotifyPropertyChanged(nameof(Width)); }} = 500;
    public HorizontalAlignment TableAlignment { get; set { field = value; NotifyPropertyChanged(nameof(TableAlignment)); } } = HorizontalAlignment.Left;

    internal IBrush SelectionBrush = Brushes.LightSteelBlue;

    public Table() { }

    public Table(FlowDocument flowDoc) 
    { 
        MyFlowDoc = flowDoc; 
        Id = ++FlowDocument.BlockIdCounter; 
        SelectionBrush = flowDoc.SelectionBrush;

        ColDefs.CollectionChanged += ColDefs_CollectionChanged;
        RowDefs.CollectionChanged += RowDefs_CollectionChanged;
        Cells.CollectionChanged += Cells_CollectionChanged;
    }

    internal void UpdateColAndRowPoints()
    {
        
        Dispatcher.UIThread.Post(() =>
        {
            ColDefsChanged?.Invoke(this);
            RowDefsChanged?.Invoke(this);
            this.Width = ColDefs.Sum(cd => cd.Width.Value);
            this.CallRequestInvalidateVisual();
        });

    }

    private void ColDefs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {

        ColDefsChanged?.Invoke(this);
        this.Width = ColDefs.Sum(cd => cd.Width.Value);

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

        this.CallRequestInvalidateVisual();

    }

    private void Cells_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {        
        if (Cells.FirstOrDefault() is Cell c)
        {
            if (c.CellBlocks.FirstOrDefault() is Paragraph p)
            {
                MyFlowDoc.UpdateBlockAndInlineStarts(p);
            }
                
        }
      
        //this.Width = ColDefs.Sum(cd => cd.Width.Value);

    }

    internal override Table PropertyClone()
    {
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
            Margin = this.Margin,
            OwningTable = this.OwningTable,
            OwningCell = this.OwningCell
        };

        //OwningTable & OwningCell of Paragraphs are assigned in CellBlocks.CollectionChanged
        newTable.Cells = new ObservableCollection<Cell>(this.Cells.Select(c => c.PropertyClone(newTable)));

        return newTable;
    }

  
    internal override Table FullClone()
    {
        Table newTable = new(this.MyFlowDoc)
        {
            Id = this.Id,
            ColDefs = CloneColDefs(this.ColDefs),   // copied RowDefs and ColDefs must be cloned to be free of previously bound BindableGrid 
            RowDefs = CloneRowDefs(this.RowDefs),
            IsTableCellBlock = this.IsTableCellBlock,
            Height = this.Height,
            Width = this.Width,
            TableAlignment = this.TableAlignment,
            SelectionBrush = CloneBrush(this.SelectionBrush) ?? Brushes.LightSteelBlue,
            BorderBrush = CloneBrush(this.BorderBrush) ?? Brushes.Black,
            BorderThickness = this.BorderThickness,
            Margin = this.Margin,
            OwningTable = this.OwningTable,
            OwningCell = this.OwningCell
        };

        //OwningTable & OwningCell of Paragraphs are assigned in CellBlocks.CollectionChanged
        newTable.Cells = new ObservableCollection<Cell>(this.Cells.Select(c => c.FullClone(newTable)));

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

    public void RemoveCellAt(int rowno,  int colno)
    {
        if (Cells.FirstOrDefault(c => c.RowNo == rowno && c.ColNo == colno) is Cell toRemoveCell)
            Cells.Remove(toRemoveCell);
    }

    public void InsertColumns(int insertColumnIndex, int count)
    {
        int origSelectionStart = MyFlowDoc.Selection.Start;
        List<int> addedCellIds = [];

        if (insertColumnIndex > ColDefs.Count) return;

        for (int insertCol = 0; insertCol < count;  insertCol++)
        {            
            //double newWidth = ColDefs[insertColumnIndex].Width.Value / 2D;  // only halve if table is at some max size
            double newWidth = ColDefs[insertColumnIndex].Width.Value;
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


        //UpdateFlowDoc( count);

    }

    //internal void UpdateFlowDoc(int fromCharIndex, int lengthOffset)
    //{
    //    //Auto update blocks and ranges when collection changed
    //    MyFlowDoc.AllParagraphs = [.. MyFlowDoc.GetAllParagraphs];  //update collection of all paragraphs
    //    MyFlowDoc.UpdateBlockAndInlineStarts(Math.Max(0, MyFlowDoc.Blocks.IndexOf(OwningTable)));

    //    //if (CellBlocks.Count > 0 && e.NewStartingIndex > -1)
    //    //{
    //    //    int lengthOffset = 0;
    //    //    if (e.NewItems != null)
    //    //    {
    //    //        foreach (Block b in e.NewItems)
    //    //            lengthOffset += b.BlockLength;
    //    //    }

    //    //    if (e.OldItems != null)
    //    //    {
    //    //        foreach (Block b in e.OldItems)
    //    //            lengthOffset -= b.BlockLength;
    //    //    }

        
    //    MyFlowDoc.UpdateTextRanges(fromCharIndex, lengthOffset);
    //    //}

    //}

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
        if (GetCellAt(rowNo, colNo) is Cell firstCell)
        {
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
        }

        
    }

    public void MergeCellsDown(int rowNo, int colNo, int numberCellsToMerge = 1)
    {
        if (GetCellAt(rowNo, colNo) is Cell firstCell)
        {
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
        }


    }

    internal int GetParagraphCount()
    {
        int parCount = 0;
        foreach (var c in Cells) 
            parCount += c.CellBlocks.Count;
        return parCount;
    }

}



