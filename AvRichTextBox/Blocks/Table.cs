using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvRichTextBox;

public partial class Table : Block
{
    public Thickness BorderThickness { get; set { field = value; NotifyPropertyChanged(nameof(BorderThickness)); } } = new(1);
    public ISolidColorBrush BorderBrush { get; set { field = value; NotifyPropertyChanged(nameof(BorderBrush)); } } = Brushes.Black;
    
    public ObservableCollection<Cell> Cells { get; set; } = [];
    public ColumnDefinitions ColDefs { get; set; } = [];
    public RowDefinitions RowDefs { get; set; } = [];
    public double Height { get; set; } = 450;
    public double Width { get; set; } = 500;
    public HorizontalAlignment TableAlignment { get; set; } = HorizontalAlignment.Left;

    internal IBrush SelectionBrush = Brushes.LightSteelBlue;

    public Table() { }

    public Table(FlowDocument flowDoc) { MyFlowDoc = flowDoc; Id = ++FlowDocument.TableIdCounter; SelectionBrush = flowDoc.SelectionBrush; }


    public Table(int cols, int rows, FlowDocument flowDoc) : this(flowDoc)
    {
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


public class Cell : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

    public ObservableCollection<Block> CellBlocks { get; set; } = [];

    public Cell(Table owningTable) 
    { 
        OwningTable = owningTable;
        CellBlocks.CollectionChanged += CellBlocks_CollectionChanged;
    
    }

    private void CellBlocks_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is Paragraph p)
                {
                    p.IsTableCellBlock = true;
                    p.OwningTable = OwningTable;
                    p.OwningCell = this;
                }
            }
        }
        
    }

    internal Table OwningTable = null!;
    public Table GetOwningTable => OwningTable;

    public Thickness BorderThickness { get; set { field = value; NotifyPropertyChanged(nameof(BorderThickness)); } } = new(1);
    public ISolidColorBrush BorderBrush { get; set { field = value; NotifyPropertyChanged(nameof(BorderBrush)); } } = Brushes.Black;
    public ISolidColorBrush CellBackground { get; set { field = value; NotifyPropertyChanged(nameof(CellBackground)); } } = null!;
    public VerticalAlignment CellVerticalAlignment { get; set { field = value; NotifyPropertyChanged(nameof(CellVerticalAlignment)); } } = VerticalAlignment.Top;
    public Thickness Padding { get; set { field = value; NotifyPropertyChanged(nameof(Padding)); } } = new(5);
    
    public int ColNo { get; set; }
    public int RowNo { get; set; }
    public int ColSpan { get; set; } = 1;
    public int RowSpan { get; set; } = 1;

    public bool Selected { get; set { field = value; NotifyPropertyChanged(nameof(Selected)); } } = false;

    public IBrush SelectionBrush => OwningTable.SelectionBrush;

    //public double Width { get; set; } = 100;
    public double Height { get; set; } = 60;
    public bool vmerged = false;
   

}

