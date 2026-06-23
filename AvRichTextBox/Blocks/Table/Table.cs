using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.ObjectModel;
using static AvRichTextBox.FlowDocument;

namespace AvRichTextBox;

public partial class Table : Block
{
    public Thickness BorderThickness { get; set { field = value; NotifyPropertyChanged(nameof(BorderThickness)); } } = new(1);
    public IBrush BorderBrush { get; set { field = value; NotifyPropertyChanged(nameof(BorderBrush)); } } = Brushes.Black;
    
    public ObservableCollection<Cell> Cells { get; set; } = [];
    public ColumnDefinitions ColDefs { get; set; } = [];
    public RowDefinitions RowDefs { get; set; } = [];
    public double Height { get; set { field = value; NotifyPropertyChanged(nameof(Height)); } } = 0;
    public double Width { get; set; } = 500;
    public HorizontalAlignment TableAlignment { get; set; } = HorizontalAlignment.Left;

    internal IBrush SelectionBrush = Brushes.LightSteelBlue;

    public Table() { }

    public Table(FlowDocument flowDoc) 
    { 
        MyFlowDoc = flowDoc; 
        Id = ++FlowDocument.TableIdCounter; 
        SelectionBrush = flowDoc.SelectionBrush; 
    }


    public Table(int cols, int rows, FlowDocument flowDoc) : this(flowDoc)
    {
        if (cols <= 0)
            throw new ArgumentOutOfRangeException(nameof(cols), cols, "Number of columns must be greater than zero.");
        if (rows <= 0)
            throw new ArgumentOutOfRangeException(nameof(rows), rows, "Number of rows must be greater than zero.");

        //Cells.CollectionChanged += Cells_CollectionChanged;

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
                    BorderBrush = Brushes.Red,
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


    }

    internal override Table FullClone()
    {
        Table newTable = new(this.MyFlowDoc)
        {
            Id = this.Id,
            ColDefs = CloneColDefs(this.ColDefs),
            RowDefs = CloneRowDefs(this.RowDefs),
            IsTableCellBlock = this.IsTableCellBlock,
            Height = this.Height,
            Width = this.Width,
            OwningCell = this.OwningCell,
            OwningTable = this.OwningTable,
            TableAlignment = this.TableAlignment,
            SelectionBrush = CloneBrush(this.SelectionBrush) ?? Brushes.LightSteelBlue,
            BorderBrush = CloneBrush(this.BorderBrush) ?? Brushes.Black,
            BorderThickness = this.BorderThickness,
            Margin = this.Margin,

        };

        newTable.Cells = new ObservableCollection<Cell>(this.Cells.Select(c => c.FullClone(newTable)));

        return newTable;
    }

    // RowDefs and ColDefs must be cloned to be free of previously bound BindableGrid 
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

    internal void InsertColumn(int idx)
    {

    }


}



