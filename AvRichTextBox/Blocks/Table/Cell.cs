using Avalonia.Layout;
using Avalonia.Media;
using DynamicData;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using static AvRichTextBox.FlowDocument;

namespace AvRichTextBox;

public class Cell : AvaloniaObject, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

    public int StartInDoc => this.CellBlocks?.FirstOrDefault()?.StartInDoc ?? 10000000; // $$$$$$$$$temp
    
    internal ObservableCollection<Block> CellBlocks { get; } = [];
    public IEnumerable<Block> GetCellBlocks => CellBlocks;
    
    public void InsertBlockAt(int index, Block block) { InsertCellBlockIntoCollectionAt(index, block); }
    public void RemoveBlockAt(int index) { RemoveCellBlockFromCollectionAt(CellBlocks, index); }
    public void RemoveBlock(Block block) { RemoveCellBlockFromCollection(CellBlocks, block); }

    internal void InsertCellBlockIntoCollectionAt(int insertIdx, Block blockToInsert)
    {
        if (insertIdx < 0 || insertIdx > CellBlocks.Count)
            throw new Exception("Block index is out of bounds of the block collection.");

        DisableUndoStack = true;

        CellBlocks.Insert(insertIdx, blockToInsert);
        blockToInsert.IsAttachedToDocument = this.IsAttachedToDocument;
        blockToInsert.OwningCellId = this.Id;
        
        if (OwningTable != null && OwningTable.MyFlowDoc != null)
        {
            int tableId = OwningTable.Id;
            int cellId = this.Id;

            int updateIdx = OwningTable.MyFlowDoc.Blocks.IndexOf(OwningTable);
            bool addUndo = OwningTable.IsAttachedToDocument;
            if (addUndo)
                OwningTable.MyFlowDoc.Undos.Add(new InsertBlockUndo(OwningTable.MyFlowDoc, blockToInsert.Id, blockToInsert.BlockLength, true, tableId, cellId));
        }

        DisableUndoStack = false;
    }


    internal void RemoveCellBlockFromCollectionAt(ObservableCollection<Block> blockCollection, int removeAtIndex)
    {
        if (removeAtIndex < 0 || removeAtIndex >= blockCollection.Count)
            throw new Exception("Block index is out of bounds of the block collection.");

        if (blockCollection.Count == 1 && blockCollection[0].Text == "")
            throw new Exception("Cannot remove default empty paragraph in the collection.");

        Block blockToRemove = blockCollection[removeAtIndex];
        RemoveCellBlockFromCollection(blockCollection, blockToRemove);

        if (blockCollection.Count == 0)
            AddDefaultParagraph(blockCollection);
    }

    internal void RemoveCellBlockFromCollection(ObservableCollection<Block> blockCollection, Block? blockToRemove)
    {
        if (blockToRemove == null)
            throw new Exception("Block to remove must not be null.");
        if (!blockCollection.Contains(blockToRemove)) return;

        DisableUndoStack = true;
        
        blockCollection.Remove(blockToRemove);

        if (OwningTable != null && OwningTable.MyFlowDoc != null)
        {
            Block removedBlockClone = blockToRemove.FullClone(true);

            int tableId = blockToRemove.OwningTableId;
            int cellId = blockToRemove.OwningCellId;
            int removeAtIdx = blockCollection.IndexOf(blockToRemove);

            bool addUndo = !blockToRemove.IsCellBlock || blockToRemove.OwningTable!.IsAttachedToDocument;

            if (addUndo)
                OwningTable.MyFlowDoc.Undos.Add(new RemoveBlockUndo(OwningTable.MyFlowDoc, removeAtIdx, removedBlockClone, blockToRemove.BlockLength, blockToRemove.IsCellBlock, tableId, cellId));
        }

        DisableUndoStack = false;

    }


    //internal int OwningTableId = -1;
    public Table OwningTable = null!;
    [JsonIgnore]
    public Table GetOwningTable => OwningTable;


    public Cell()
    {
        CellBlocks.CollectionChanged += CellBlocks_CollectionChanged;
        Id = ++FlowDocument.TableCellIdCounter;
    }

    /// <summary>
    /// Retained for backwards compatibility. Prefer <see cref="Cell()"/>.
    /// </summary>
    [Obsolete("Use the parameterless Cell() constructor instead.")]

    public Cell(Table owningTable) : this() { } 

    internal int Id = 0;

    internal bool IsAttachedToDocument = false;
        

    internal void ResizeCellBlocks()
    {
        foreach (Paragraph p in CellBlocks.OfType<Paragraph>())
             p.CallRequestSizeChanged();
    }

    private void CellBlocks_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (!this.IsAttachedToDocument) return;

        if (e.NewItems != null)
        {
            foreach (Block b in e.NewItems.OfType<Block>())
            {
                b.IsTableCellBlock = true;
                b.OwningTableId = OwningTable.Id;
                b.OwningCellId = this.Id;
                b.MyFlowDoc = OwningTable.MyFlowDoc;
                b.IsAttachedToDocument = this.IsAttachedToDocument;
            }
        }

        if (IsClonedCell) return;

        //Auto update blocks and ranges when collection changed, unless this is a Clone
        OwningTable.MyFlowDoc.AllParagraphs = [.. OwningTable.MyFlowDoc.GetAllParagraphs];  //update collection of all paragraphs
        OwningTable.MyFlowDoc.UpdateBlockAndInlineStarts(Math.Max(0, OwningTable.MyFlowDoc.Blocks.IndexOf(OwningTable)));

        ////if (CellBlocks.Count > 0 && e.NewStartingIndex > -1)
        //int lengthOffset = 0;
        //if (e.NewStartingIndex > -1 && e.NewItems != null)
        //{
        //    foreach (Block b in e.NewItems)
        //        lengthOffset += b.BlockLength;

        //    OwningTable.MyFlowDoc.UpdateTextRanges(CellBlocks[e.NewStartingIndex].StartInDoc, lengthOffset);
        //}
        //if (e.OldStartingIndex > -1 && e.OldItems != null)
        //{
        //    foreach (Block b in e.OldItems)
        //        lengthOffset -= b.BlockLength;
        //    if (e.OldStartingIndex < CellBlocks.Count)
        //        OwningTable.MyFlowDoc.UpdateTextRanges(CellBlocks[e.OldStartingIndex].StartInDoc, lengthOffset);
        //}
        //if (e.Action == NotifyCollectionChangedAction.Reset)
        //{
        //    if (oldCellBlocks.Count > 0)
        //        OwningTable.MyFlowDoc.UpdateTextRanges(oldCellBlocks[0].StartInDoc, oldCellBlocks.Sum(cb=> cb.BlockLength));
        //}
        //oldCellBlocks = [.. CellBlocks];

        OwningTable.UpdateCellParagraphSizes();
    }

    //private List<Block> oldCellBlocks = [];

    public Thickness BorderThickness 
    { 
        get; 
        set 
        {
            Thickness oldThickness = field;
            field = value;

            if (!IsAttachedToDocument) return;

            if (!DisableUndoStack)
                OwningTable.MyFlowDoc.Undos.Add(new CellBorderThicknessChangedUndo(OwningTable.Id, this.Id, oldThickness, field, OwningTable.MyFlowDoc));

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

            if (!DisableUndoStack)
                OwningTable.MyFlowDoc.Undos.Add(new CellBorderBrushChangedUndo(OwningTable.Id, this.Id, oldBrush, field, OwningTable.MyFlowDoc));

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

            if (!DisableUndoStack)
                OwningTable.MyFlowDoc.Undos.Add(new CellBackgroundChangedUndo(OwningTable.Id, this.Id, oldBrush, field, OwningTable.MyFlowDoc));

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

            if (!DisableUndoStack)
                OwningTable.MyFlowDoc.Undos.Add(new CellVerticalAlignmentChangedUndo(OwningTable.Id, this.Id, oldVAlign, field, OwningTable.MyFlowDoc));

            NotifyPropertyChanged(nameof(CellVerticalAlignment));

            OwningTable?.MyFlowDoc?.UpdateCaret();

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

            if (!DisableUndoStack)
                OwningTable.MyFlowDoc.Undos.Add(new CellPaddingChangedUndo(OwningTable.Id, this.Id, oldPadding, field, OwningTable.MyFlowDoc));

            NotifyPropertyChanged(nameof(Padding)); 
        } 
    } = new(5);
       

    /// <summary>
    /// Do not set ColNo directly on Cells in code-behind, it is set automatically with Add/RemoveColumns() and MergeCellsRight()
    /// </summary>
    public int ColNo { get; set { field = value; NotifyPropertyChanged(nameof(ColNo)); } }
    /// <summary>
    /// Do not set RowNo directly on Cells in code-behind, it is set automatically with Add/RemoveRows() and MergeCellsDown()
    /// </summary>
    public int RowNo { get; set { field = value; NotifyPropertyChanged(nameof(RowNo)); } }
    /// <summary>
    /// Do not set ColSpan directly on Cells in code-behind, it is set automatically with Add/RemoveColumns() and MergeCellsRight()
    /// </summary>
    public int ColSpan { get; set { field = value; NotifyPropertyChanged(nameof(ColSpan)); } } = 1;
    /// <summary>
    /// Do not set RowSpan directly on Cells in code-behind, it is set automatically with Add/RemoveRows() and MergeCellsDown()
    /// </summary>
    public int RowSpan { get; set { field = value; NotifyPropertyChanged(nameof(RowSpan)); } } = 1;

    public bool Selected { get; set { field = value; NotifyPropertyChanged(nameof(Selected)); } } = false;

    internal IBrush SelectionBrush => OwningTable?.SelectionBrush ?? Brushes.Transparent;

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
        bool keepDisableUndoStack = DisableUndoStack;
        DisableUndoStack =  true;

        Cell newCell = new()
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
            OwningTable = owningTable
        };

        // OwningTable and OwningCell set in CellBlocks_CollectionChanged event
        //newCell.CellBlocks.AddRange(this.CellBlocks.Select(cb => cb.FullClone()));

        DisableUndoStack = keepDisableUndoStack;

        return newCell;
    }


    internal Cell FullClone(Table owningTable, bool keepId)
    {
        bool keepDisableUndoStack = DisableUndoStack;
        DisableUndoStack =  true;

        Cell newCell = new()
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
            OwningTable = owningTable
        };

        if (keepId)
            newCell.Id = this.Id;

        // OwningTable and OwningCell set in CellBlocks_CollectionChanged event
        newCell.CellBlocks.AddRange(this.CellBlocks.Select(cb => cb.FullClone(keepId)));

        DisableUndoStack =  keepDisableUndoStack;

        return newCell;
    }

    public string GetText => string.Join('\n', this.CellBlocks.ToList().ConvertAll(cb => cb.Text));

}

