using Avalonia.Media;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AvRichTextBox;

public class Block : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public void NotifyPropertyChanged([CallerMemberName] String propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

    internal int Id = 0;

    internal bool IsTableCellBlock = false;
    public bool IsCellBlock => IsTableCellBlock;
    internal Table OwningTable = null!;
    internal Cell OwningCell = null!;
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

    public Thickness Margin { get; set { field = value; NotifyPropertyChanged(nameof(Margin)); } }

    public string Text
    {
        get
        {
            switch (this)
            {
                case Paragraph p:

                    var sb = new StringBuilder();
                    foreach (var i in p.Inlines)
                    {
                       sb.Append(i.InlineText);
                    }

                    sb.Append((p.IsTableCellBlock ? Environment.NewLine : "\n"));  // Environment.NewLine adds "\r\n" (Non-Unix) or "\n" (Unix) to end of paragraph text

                    return sb.ToString();

                case Table t:

                    var sbTable = new StringBuilder();
                    foreach (Cell c in t.Cells)
                        foreach (Block b in c.CellBlocks)
                            sbTable.Append(b.Text);  //recursive since CellBlocks is a Block (Paragraph)
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
                    {
                        //if (i is EditableLineBreak)
                        //    len += 0;
                        //else
                            len += i.InlineText?.Length ?? 0;
                    }

                    // paragraph CR
                    len += 1;  // ????????
                    

                    return len;

                case Table t:

                    int lenTable = 0;
                    foreach (Cell c in t.Cells)
                        foreach (Block b in c.CellBlocks)
                            lenTable += b.Text.Length;
                    return lenTable;

                default:

                    return 0;
            }
        }
    }

    internal int SelectionLength => SelectionEndInBlock - SelectionStartInBlock;

    public int BlockLength
    {
        get
        {
            int returnLength = 0;
            switch (this)
            {
                case Paragraph p:
                    //returnLength = p.Inlines.ToList().Sum(il => il.InlineLength) + 1;  // extra for paragraph CR
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

    internal int StartInDoc { get; set { if (field != value) { field = value; NotifyPropertyChanged(nameof(StartInDoc)); } } }
    internal int EndInDoc => StartInDoc + BlockLength;

    //Updated on FlowDoc_Selection_Changed
    public int SelectionStartInBlock
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                NotifyPropertyChanged(nameof(SelectionStartInBlock));
                if (this.IsTableCellBlock)
                    this.OwningCell.Selected = BlockLength > 0 && SelectionStartInBlock == 0 && SelectionEndInBlock == BlockLength;  

            }
        }
    }

    public int SelectionEndInBlock
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                NotifyPropertyChanged(nameof(SelectionEndInBlock));
                if (this.IsTableCellBlock)
                    this.OwningCell.Selected = BlockLength > 0 && SelectionStartInBlock == 0 && SelectionEndInBlock == BlockLength;  
            }
        }
    }

    public static bool IsFocusable => false;

    internal virtual Block FullClone()
    {
        return new Block() 
        { 
            Id = this.Id,
            IsTableCellBlock = this.IsTableCellBlock,
            OwningTable = this.OwningTable,
            OwningCell = this.OwningCell
        };
    }

}
