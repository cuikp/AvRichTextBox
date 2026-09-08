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

    internal ObservableCollection<Block> CellBlocks { get; } = [];
    public IEnumerable<Block> GetCellBlocks => CellBlocks;
    
    public void InsertBlockAt(int index, Block block) { OwningTable.MyFlowDoc.InsertBlockIntoCollectionAt(CellBlocks, index, block); }
    public void RemoveBlockAt(int index) { OwningTable.MyFlowDoc.RemoveBlockFromCollectionAt(CellBlocks, index); }
    public void RemoveBlock(Block block) { OwningTable.MyFlowDoc.RemoveBlockFromCollection(CellBlocks, block); }


    internal int Id = 0;

    internal bool IsAttachedToDocument = false;

    public Cell(Table owningTable) 
    { 
        OwningTable = owningTable;
        CellBlocks.CollectionChanged += CellBlocks_CollectionChanged;
        Id = ++FlowDocument.TableCellIdCounter;

    }

    internal void ResizeCellBlocks()
    {
        foreach (Paragraph p in CellBlocks.OfType<Paragraph>())
             p.CallRequestSizeChanged();
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

        if (IsClonedCell) return;

        //Auto update blocks and ranges when collection changed, unless this is a Clone
        OwningTable.MyFlowDoc.AllParagraphs = [.. OwningTable.MyFlowDoc.GetAllParagraphs];  //update collection of all paragraphs
        OwningTable.MyFlowDoc.UpdateBlockAndInlineStarts(Math.Max(0, OwningTable.MyFlowDoc.Blocks.IndexOf(OwningTable)));

        if (CellBlocks.Count > 0 && e.NewStartingIndex > -1)
        {
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

            OwningTable.MyFlowDoc.UpdateTextRanges(CellBlocks[e.NewStartingIndex].StartInDoc, lengthOffset);
        }



    }


    internal Table OwningTable = null!;
    [JsonIgnore]
    public Table GetOwningTable => OwningTable;

    public Thickness BorderThickness 
    { 
        get; 
        set 
        {
            Thickness oldThickness = field;
            field = value;

            if (!IsAttachedToDocument) return;

            if (!OwningTable.MyFlowDoc.disableUndoStack)
                OwningTable.MyFlowDoc.Undos.Add(new CellBorderThicknessChangedUndo(OwningTable.Id, this.Id, oldThickness, OwningTable.MyFlowDoc));

            NotifyPropertyChanged(nameof(BorderThickness)); 
        } 
    } = new(1);

    public IBrush BorderBrush 
    { 
        get; 
        set 
        {
            IBrush oldBrush = field;
            field = value;

            if (!IsAttachedToDocument) return;

            if (!OwningTable.MyFlowDoc.disableUndoStack)
                OwningTable.MyFlowDoc.Undos.Add(new CellBorderBrushChangedUndo(OwningTable.Id, this.Id, oldBrush, OwningTable.MyFlowDoc));

            NotifyPropertyChanged(nameof(BorderBrush)); 
        } 
    } = Brushes.Black;

    public IBrush CellBackground 
    { 
        get; 
        set 
        {
            IBrush oldBrush = field;
            field = value;

            if (!IsAttachedToDocument) return;

            if (!OwningTable.MyFlowDoc.disableUndoStack)
                OwningTable.MyFlowDoc.Undos.Add(new CellBackgroundChangedUndo(OwningTable.Id, this.Id, oldBrush, OwningTable.MyFlowDoc));

            NotifyPropertyChanged(nameof(CellBackground)); 
        } 
    } = Brushes.Transparent;

    public VerticalAlignment CellVerticalAlignment 
    { 
        get; 
        set 
        {
            VerticalAlignment oldVAlign = field;
            field = value;

            if (!IsAttachedToDocument) return;

            if (!OwningTable.MyFlowDoc.disableUndoStack)
                OwningTable.MyFlowDoc.Undos.Add(new CellVerticalAlignmentChangedUndo(OwningTable.Id, this.Id, oldVAlign, OwningTable.MyFlowDoc));

            NotifyPropertyChanged(nameof(CellVerticalAlignment)); 
        } 
    } = VerticalAlignment.Top;

    public Thickness Padding 
    { 
        get; 
        set 
        {
            Thickness oldPadding = field;
            field = value;

            if (!IsAttachedToDocument) return;

            if (!OwningTable.MyFlowDoc.disableUndoStack)
                OwningTable.MyFlowDoc.Undos.Add(new CellPaddingChangedUndo(OwningTable.Id, this.Id, oldPadding, OwningTable.MyFlowDoc));

            NotifyPropertyChanged(nameof(Padding)); 
        } 
    } = new(5);
    
    internal int ColNo { get; set { field = value; NotifyPropertyChanged(nameof(ColNo)); } }
    internal int RowNo { get; set { field = value; NotifyPropertyChanged(nameof(RowNo)); } }
    internal int ColSpan { get; set { field = value; NotifyPropertyChanged(nameof(ColSpan)); } } = 1;
    internal int RowSpan { get; set { field = value; NotifyPropertyChanged(nameof(RowSpan)); } } = 1;

    public bool Selected { get; set { field = value; NotifyPropertyChanged(nameof(Selected)); } } = false;

    internal IBrush SelectionBrush => OwningTable.SelectionBrush;

    internal double Height { get; set; } = 60;  // arbitrary default
    internal bool vmerged = false;
    internal bool IsClonedCell = false;

    public Cell? GetNextCell()
    {
        int thisIdx = OwningTable.Cells.IndexOf(this);
        return thisIdx < OwningTable.Cells.Count - 1 ? OwningTable.Cells[thisIdx + 1] : null;
    }

    public Cell? GetPreviousCell()
    {
        int thisIdx = OwningTable.Cells.IndexOf(this);
        return thisIdx > 0 ? OwningTable.Cells[thisIdx - 1] : null;
    }

    internal Cell PropertyClone(Table owningTable)
    {
        OwningTable.MyFlowDoc.disableUndoStack = true;

        Cell newCell = new(owningTable)
        {
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
            IsClonedCell = true,
        };

        // OwningTable and OwningCell set in CellBlocks_CollectionChanged event
        //newCell.CellBlocks.AddRange(this.CellBlocks.Select(cb => cb.FullClone()));
        
        OwningTable.MyFlowDoc.disableUndoStack = false;

        return newCell;
    }


    internal Cell FullClone(Table owningTable, bool keepId)
    {
        OwningTable.MyFlowDoc.disableUndoStack = true;

        Cell newCell = new(owningTable)
        {
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
            IsClonedCell = true
        };

        if (keepId)
            newCell.Id = this.Id;

        // OwningTable and OwningCell set in CellBlocks_CollectionChanged event
        newCell.CellBlocks.AddRange(this.CellBlocks.Select(cb => cb.FullClone(keepId)));

        OwningTable.MyFlowDoc.disableUndoStack = false;

        return newCell;
    }


}

