using Avalonia.Media;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AvRichTextBox;

public class Block : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    internal void NotifyPropertyChanged([CallerMemberName] String propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

    internal int Id = 0;
    public int GetID => Id;

    internal bool IsTableCellBlock = false;
    internal Table OwningTable = null!;
    internal Cell OwningCell = null!;
    internal bool IsAttachedToDocument = false;

    public bool IsCellBlock => IsTableCellBlock;
    public Cell GetOwningCell => OwningCell;

    internal FlowDocument MyFlowDoc
    {
        get;
        set
        {
            field = value;
            if (this is Paragraph p)
            {
                foreach (IEditable ied in p.Inlines)
                    ied.MyFlowDoc = value;
            }
        }
    } = null!;

    public Thickness Margin 
    { 
        get; 
        set 
        {
            Thickness oldMargin = field;
            field = value;

            if (!MyFlowDoc.disableUndoStack)
                MyFlowDoc.Undos.Add(new BlockMarginChangedUndo(this.Id, oldMargin, value, MyFlowDoc));

            NotifyPropertyChanged(nameof(Margin));

            MyFlowDoc.InvokeSelectionChanged();
            
            
        }
    }

    public IBrush BorderBrush
    {
        get;
        set
        {
            IBrush oldBrush = field;

            field = value;

            if (!IsAttachedToDocument) return;

            if (!MyFlowDoc.disableUndoStack)
                MyFlowDoc.Undos.Add(new BlockBorderBrushChangedUndo(this.Id, oldBrush, value, MyFlowDoc));

            NotifyPropertyChanged(nameof(BorderBrush));
        }
    } = Brushes.Black;

    public Thickness BorderThickness
    {
        get;
        set
        {
            Thickness oldThickness = field;
            field = value;
            
            if (!IsAttachedToDocument) return;

            if (!MyFlowDoc.disableUndoStack)
                MyFlowDoc.Undos.Add(new BlockBorderThicknessChangedUndo(this.Id, oldThickness, value, MyFlowDoc));

            if (this is Table t)
                t.UpdateColAndRowPoints();

            NotifyPropertyChanged(nameof(BorderThickness));
        }
    } = new(0);

    public IBrush Background 
    { 
        get; 
        set 
        {
            IBrush oldBrush = field;

            field = value;
            if (!IsAttachedToDocument) return;


            if (!MyFlowDoc.disableUndoStack)
                MyFlowDoc.Undos.Add(new BlockBackgroundChangedUndo(this.Id, oldBrush, value, MyFlowDoc));

            NotifyPropertyChanged(nameof(Background)); 
        } 
    } = new SolidColorBrush(Colors.Transparent);

    public FontFamily FontFamily 
    { 
        get; 
        set 
        {
            FontFamily oldFontFamily = field;

            field = value;

            if (!IsAttachedToDocument) return;

            if (!MyFlowDoc.disableUndoStack)
                MyFlowDoc.Undos.Add(new BlockFontFamilyChangedUndo(this.Id, oldFontFamily, value, MyFlowDoc));

            NotifyPropertyChanged(nameof(FontFamily)); 
        } 
    
    } = new("Meiryo");
    
    public double FontSize 
    { 
        get; 
        set 
        {
            double oldFontSize = field;
            field = value;

            if (!MyFlowDoc.disableUndoStack)
                MyFlowDoc.Undos.Add(new BlockFontSizeChangedUndo(this.Id, oldFontSize, value, MyFlowDoc));

            NotifyPropertyChanged(nameof(FontSize)); 
        } 
    } = 16D;

    public FontWeight FontWeight 
    { 
        get; 
        set 
        {
            FontWeight oldFontWeight = field;
            field = value;

            if (!IsAttachedToDocument) return;

            if (!MyFlowDoc.disableUndoStack)
                MyFlowDoc.Undos.Add(new BlockFontWeightChangedUndo(this.Id, oldFontWeight, value, MyFlowDoc));

            NotifyPropertyChanged(nameof(FontWeight)); 
        } 
    } = FontWeight.Normal;

    public FontStyle FontStyle 
    { 
        get; 
        set 
        {
            FontStyle oldFontStyle = field;
            field = value;

            if (!MyFlowDoc.disableUndoStack)
                MyFlowDoc.Undos.Add(new BlockFontStyleChangedUndo(this.Id, oldFontStyle, value, MyFlowDoc));

            NotifyPropertyChanged(nameof(FontStyle)); 
        } 
    } = FontStyle.Normal;



    public string Text
    {
        get
        {
            switch (this)
            {
                case Paragraph p:     

                    var sb = new StringBuilder();
                    foreach (var i in p.Inlines)
                       sb.Append(i.InlineText);

                    bool endOfTableCell = (p.IsTableCellBlock && p == p.OwningCell.CellBlocks.Last());
                    //sb.Append(endOfTableCell ? (char)7 : Environment.NewLine);  //  Environment.NewLine adds "\r\n" (Non-Unix) or "\n" (Unix) to end of paragraph text
                    sb.Append(endOfTableCell ? (char)7 : "\r");

                    return sb.ToString();

                case Table t:

                    var sbTable = new StringBuilder();
                    foreach (Cell c in t.Cells)
                    {
                        foreach (Block b in c.CellBlocks)
                            sbTable.Append(b.Text);  //recursive since each of CellBlocks is a Block
                    }
                    
                    return sbTable.ToString();

                default:
                    return "";
            }
        }
    }

    public int TextLength
    {
        get
        {
            switch (this)
            {
                case Paragraph p:
                    int len = 0;
                    foreach (var i in p.Inlines)
                       len += i.InlineText?.Length ?? 0;

                    // paragraph CR
                    len += 1;  
                    

                    return len;

                case Table t:

                    int lenTable = 0;
                    foreach (Cell c in t.Cells)
                    {
                        foreach (Block b in c.CellBlocks)
                            lenTable += b.Text.Length;
                    }
                        
                    return lenTable;

                default:

                    return 0;
            }
        }
    }

    public int SelectionLength => SelectionEndInBlock - SelectionStartInBlock;

    public int BlockLength
    {
        get
        {
            int returnLength = 0;
            switch (this)
            {
                case Paragraph p:
                    returnLength = p.Inlines.ToList().Sum(il => il.InlineText?.Length ?? 0) + 1;  // extra for paragraph CR
                    break;

                case Table t:

                    foreach (Cell c in t.Cells)
                    {
                        foreach (Block b in c.CellBlocks)
                            returnLength += b.BlockLength;
                    }
                    returnLength += 0; // need table final char? 
                    break;
            }

            return returnLength;
        }

    }

    public int GetStartInDoc => StartInDoc;
    public int GetEndInDoc => EndInDoc;
    public int GetSelectionStartInBlock => SelectionStartInBlock;
    public int GetSelectionEndInBlock => SelectionEndInBlock;

    internal int StartInDoc { get; set { if (field != value) { field = value; NotifyPropertyChanged(nameof(StartInDoc)); } } }
    internal int EndInDoc => StartInDoc + BlockLength - 1; // changed to reflect revised logic for paragraph end navigation

    //Updated on FlowDoc_Selection_Changed
    internal int SelectionStartInBlock
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                NotifyPropertyChanged(nameof(SelectionStartInBlock));

                if (this.IsTableCellBlock)
                    this.OwningCell?.Selected = BlockLength > 0 && SelectionStartInBlock == 0 && SelectionEndInBlock == BlockLength;
            }
        }
    }

    internal int SelectionEndInBlock
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                NotifyPropertyChanged(nameof(SelectionEndInBlock));
                if (this.IsTableCellBlock)
                    this.OwningCell?.Selected = BlockLength > 0 && SelectionStartInBlock == 0 && SelectionEndInBlock == BlockLength;
            }
        }
    }

    internal static bool IsFocusable => false;

    internal virtual Block PropertyClone()
    {
        MyFlowDoc.disableUndoStack = true;
        
        Block newBlock = new () 
        { 
            IsTableCellBlock = this.IsTableCellBlock,
            Margin = this.Margin,
            MyFlowDoc = this.MyFlowDoc
            //OwningTable & OwningCell are assigned in CellBlocks.CollectionChanged
        };

        MyFlowDoc.disableUndoStack = false;

        return newBlock;
    }
    
    internal virtual Block FullClone(bool keepId)
    {
        MyFlowDoc.disableUndoStack = true;

        Block newBlock = PropertyClone();
        if (keepId)
            newBlock.Id = this.Id;

        MyFlowDoc.disableUndoStack = false;

        return newBlock;
    }

}
