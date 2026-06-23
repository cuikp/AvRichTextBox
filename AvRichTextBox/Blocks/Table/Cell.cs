using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static AvRichTextBox.FlowDocument;

namespace AvRichTextBox;

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
                if (item is Block b)
                {
                    b.IsTableCellBlock = true;
                    b.OwningTable = OwningTable;
                    b.OwningCell = this;
                }
            }
        }
    }

    internal Table OwningTable = null!;
    public Table GetOwningTable => OwningTable;

    public Thickness BorderThickness { get; set { field = value; NotifyPropertyChanged(nameof(BorderThickness)); } } = new(1);
    public IBrush BorderBrush { get; set { field = value; NotifyPropertyChanged(nameof(BorderBrush)); } } = Brushes.Black;
    public IBrush CellBackground { get; set { field = value; NotifyPropertyChanged(nameof(CellBackground)); } } = null!;
    public VerticalAlignment CellVerticalAlignment { get; set { field = value; NotifyPropertyChanged(nameof(CellVerticalAlignment)); } } = VerticalAlignment.Top;
    public Thickness Padding { get; set { field = value; NotifyPropertyChanged(nameof(Padding)); } } = new(5);
    
    public int ColNo { get; set; }
    public int RowNo { get; set; }
    public int ColSpan { get; set; } = 1;
    public int RowSpan { get; set; } = 1;

    public bool Selected { get; set { field = value; NotifyPropertyChanged(nameof(Selected)); } } = false;

    public IBrush SelectionBrush => OwningTable.SelectionBrush;

    //public double Width { get; set; } = 100;
    public double Height { get; set; } = 60;  // arbitrary default
    public bool vmerged = false;


    internal Cell FullClone(Table owningTable)
    {
        Cell newCell = new(owningTable)
        {
            //Id = this.Id,   // in future if Cell needs Id 
            RowNo = this.RowNo,
            ColNo = this.ColNo,
            ColSpan = this.ColSpan,
            RowSpan = this.RowSpan,
            vmerged = this.vmerged,
            Height = this.Height,
            BorderThickness = this.BorderThickness,
            BorderBrush = CloneBrush(this.BorderBrush) ?? Brushes.Black,
            CellBackground = CloneBrush(this.CellBackground) ?? null!,
            Padding = this.Padding,
            CellVerticalAlignment = this.CellVerticalAlignment
        };

        // to trigger CellBlock.CollectionChanged after ctor:
        newCell.CellBlocks = new ObservableCollection<Block>(this.CellBlocks.Select(cb => cb.FullClone()));

        return newCell;
    }


}

