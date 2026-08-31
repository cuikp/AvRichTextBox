using Avalonia.Layout;
using Avalonia.Media;
using DynamicData;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using static AvRichTextBox.FlowDocument;

namespace AvRichTextBox;

public class Cell : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

    public ObservableCollection<Block> CellBlocks { get; set; } = [];

    internal int Id = 0;

    public Cell(Table owningTable) 
    { 
        OwningTable = owningTable;
        CellBlocks.CollectionChanged += CellBlocks_CollectionChanged;
        Id = ++FlowDocument.TableCellIdCounter;

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
                    b.MyFlowDoc = OwningTable.MyFlowDoc;
                }
            }
        }

        int lengthOffset = 0;
        if (e.NewItems != null)
        {
            foreach (Block b in e.NewItems)
                lengthOffset += b.BlockLength;
        }

        if (e.OldItems != null)
        {
            foreach (Block b in e.OldItems)
                lengthOffset -= b.BlockLength;
        }

        OwningTable.MyFlowDoc.AllParagraphs = [.. OwningTable.MyFlowDoc.GetAllParagraphs];  //update collection of all paragraphs

        //Auto update blocks and ranges when collection changed
        OwningTable.MyFlowDoc.UpdateBlockAndInlineStarts(Math.Max(0, OwningTable.MyFlowDoc.Blocks.IndexOf(OwningTable)));
        
        if (CellBlocks.Count > 0 && e.NewStartingIndex > -1)
            OwningTable.MyFlowDoc.UpdateTextRanges(CellBlocks[e.NewStartingIndex].StartInDoc, lengthOffset);


    }


    internal Table OwningTable = null!;
    [JsonIgnore]
    public Table GetOwningTable => OwningTable;

    public Thickness BorderThickness { get; set { field = value; NotifyPropertyChanged(nameof(BorderThickness)); } } = new(1);
    public IBrush BorderBrush { get; set { field = value; NotifyPropertyChanged(nameof(BorderBrush)); } } = Brushes.Black;
    public IBrush CellBackground { get; set { field = value; NotifyPropertyChanged(nameof(CellBackground)); } } = null!;
    public VerticalAlignment CellVerticalAlignment { get; set { field = value; NotifyPropertyChanged(nameof(CellVerticalAlignment)); } } = VerticalAlignment.Top;
    public Thickness Padding { get; set { field = value; NotifyPropertyChanged(nameof(Padding)); } } = new(5);
    
    public int ColNo { get; set { field = value; NotifyPropertyChanged(nameof(ColNo)); } }
    public int RowNo { get; set { field = value; NotifyPropertyChanged(nameof(RowNo)); } }
    public int ColSpan { get; set { field = value; NotifyPropertyChanged(nameof(ColSpan)); } } = 1;
    public int RowSpan { get; set { field = value; NotifyPropertyChanged(nameof(RowSpan)); } } = 1;

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
            CellVerticalAlignment = this.CellVerticalAlignment,
        };

        newCell.CellBlocks.AddRange(this.CellBlocks.Select(cb => cb.FullClone()));

        return newCell;
    }

    
}

