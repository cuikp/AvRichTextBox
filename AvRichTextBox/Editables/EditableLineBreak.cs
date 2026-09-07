using Avalonia.Controls.Documents;

namespace AvRichTextBox;

public class EditableLineBreak : LineBreak, IEditable
{
    public EditableLineBreak() { Id = ++FlowDocument.InlineIdCounter; }

    internal FlowDocument MyFlowDoc { get; set; } = null!;
    FlowDocument IEditable.MyFlowDoc { get => MyFlowDoc; set => MyFlowDoc = value; }

    internal int Id { get; set; }
    int IEditable.Id { get => Id; set => Id = value; }

    internal int MyParagraphId { get; set; }
    int IEditable.MyParagraphId { get => MyParagraphId; set => MyParagraphId = value; }
        
    string IEditable.InlineText { get => InlineText; set { InlineText = value; } }
    internal string InlineText { get; private set; } = @"\n"; //make literal to count as 2 characters
    public int InlineLength => 2;  //because LineBreak acts as a double character in TextBlock

    public double InlineHeight => FontSize;

    internal IEditable? PreviousInline { get; set; }
    IEditable? IEditable.PreviousInline { get => PreviousInline; set => PreviousInline = value; }
    internal IEditable? NextInline { get; set; }
    IEditable? IEditable.NextInline { get => NextInline; set => NextInline = value; }

    public IEditable? GetPreviousInline => PreviousInline;
    public IEditable? GetNextInline => NextInline;

    bool IEditable.IsFirstInlineOfParagraph { get; set; }

    internal bool IsLastInlineOfParagraph { get; set; }
    bool IEditable.IsLastInlineOfParagraph { get => IsLastInlineOfParagraph; set => IsLastInlineOfParagraph = value; }

    internal bool IsTableCellInline { get; set; }
    bool IEditable.IsTableCellInline { get => IsTableCellInline; set => IsTableCellInline = value; }

    int TextPositionOfInlineInParagraph { get; set; }
    int IEditable.TextPositionOfInlineInParagraph { get => TextPositionOfInlineInParagraph; set => TextPositionOfInlineInParagraph = value; }
    public int GetTextPositionOfInlineInParagraph => TextPositionOfInlineInParagraph;

    public bool IsEmpty => false;

    public IEditable Clone()
    {
        MyFlowDoc.disableUndoStack = true;

        EditableLineBreak eLB = new()
        {
            MyParagraphId = this.MyParagraphId,
            MyFlowDoc = this.MyFlowDoc,
            TextPositionOfInlineInParagraph = this.TextPositionOfInlineInParagraph,
            IsLastInlineOfParagraph = this.IsLastInlineOfParagraph,
            IsTableCellInline = this.IsTableCellInline,
        };

        MyFlowDoc.disableUndoStack = false;

        return eLB;

    }

    public IEditable CloneWithId()
    {
        IEditable IdClone = this.Clone();
        IdClone.Id = this.Id;
        return IdClone;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
    public string FontName => "---";


#if DEBUG
    // FOR DEBUGGER PANEL

    internal InlineVisualizationProperties InlineVP { get; set; } = new();
    InlineVisualizationProperties IEditable.InlineVP { get => InlineVP ; set => InlineVP = value; }
    public string InlineToolTip => "";
    public string DisplayInlineText => "{>LINEBREAK<}";

#endif

}

