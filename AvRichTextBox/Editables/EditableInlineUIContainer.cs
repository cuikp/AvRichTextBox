using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace AvRichTextBox;

public class EditableInlineUIContainer : InlineUIContainer, IEditable
{
    public EditableInlineUIContainer() { Id = ++FlowDocument.InlineIdCounter; }

    public EditableInlineUIContainer(Control c) { Child = c; Id = ++FlowDocument.InlineIdCounter; }

    internal int Id { get; set; }
    int IEditable.Id { get => Id; set => Id = value; }

    internal int MyParagraphId { get; set; }
    int IEditable.MyParagraphId { get => MyParagraphId; set => MyParagraphId = value; }

    public FlowDocument MyFlowDoc { get; set; } = null!;
    public int TextPositionOfInlineInParagraph { get; set; }

    internal string InlineText { get; private set; } = "@";
    string IEditable.InlineText { get => InlineText; set { InlineText = value; } }

    bool IEditable.IsFirstInlineOfParagraph { get; set; }

    internal bool IsLastInlineOfParagraph { get; set; }
    bool IEditable.IsLastInlineOfParagraph { get => IsLastInlineOfParagraph; set => IsLastInlineOfParagraph = value; }

    internal bool IsTableCellInline { get; set; }
    bool IEditable.IsTableCellInline { get => IsTableCellInline; set => IsTableCellInline = value; }

    public IEditable? PreviousInline { get; set; } = null!;
    public IEditable? NextInline { get; set; } = null!;


    public object Tag { get; set; } = null!;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
    public string FontName => "---";

    public int InlineLength => 1;
    public bool IsEmpty => false;
    
    
    public double InlineHeight => Child == null ? 0 : this.Child.Bounds.Height;

    
    internal int ImageNo;

    public IEditable Clone()
    {
        MyFlowDoc.disableUndoStack = true;

        EditableInlineUIContainer eIUC = new(this.Child)
        {
            MyParagraphId = this.MyParagraphId,
            MyFlowDoc = this.MyFlowDoc,
            IsLastInlineOfParagraph = this.IsLastInlineOfParagraph,
            IsTableCellInline = this.IsTableCellInline,
        };

        MyFlowDoc.disableUndoStack = false;

        return eIUC;

    }

    public IEditable CloneWithId()
    {
        IEditable IdClone = this.Clone();
        IdClone.Id = this.Id;
        IdClone.TextPositionOfInlineInParagraph = this.TextPositionOfInlineInParagraph;  //necessary because clone is produced when calculating range inline positions
        return IdClone;

    }

    public bool IsSelected { get; set; } = false;

#if DEBUG
    // FOR DEBUGGER PANEL
    public InlineVisualizationProperties InlineVP { get; set; } = new();
    public string InlineToolTip => "";
    public string DisplayInlineText { get => $"<UICONTAINER> => {(this.Child != null && this.Child.GetType() == typeof(Image) ? "Image" : "NoChild")}"; }
#endif


}


