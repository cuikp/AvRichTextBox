using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace AvRichTextBox;

public class EditableInlineUIContainer : InlineUIContainer, IEditable
{
    public EditableInlineUIContainer() { Id = ++FlowDocument.InlineIdCounter; this.PropertyChanged += EditableInlineUIContainer_PropertyChanged;  }

    [Obsolete("Use SetChild()/GetChild() instead.", true)]
    public new Control? Child
    {
        get => base.Child;
        set 
        { 
            _internalChildChange = true; 
            base.Child = value ?? null!; 
            _internalChildChange = false; 
        }
    }

    public void SetChild(Control? control)
    {
        _internalChildChange = true;

        if (MyFlowDoc != null && !MyFlowDoc.disableUndoStack)
            MyFlowDoc.Undos.Add(new EditableUIContainerChildUndo(this.MyParagraphId, this.Id, GetChild(), MyFlowDoc));

        base.Child = control!;

        _internalChildChange = false;

    }

    public Control? GetChild() => base.Child;

    private bool _internalChildChange = false;


    private void EditableInlineUIContainer_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        //Debug.WriteLine($"ieditableUICont {e.Property.Name} set");

        switch (e.Property)
        {
            case AvaloniaProperty tp when tp == InlineUIContainer.ChildProperty:

                //Debug.WriteLine($"ieditableUICont {e.Property.Name} set");

                if (!_internalChildChange)
                    throw new InvalidOperationException("Use AddChild().");

                break;

            case AvaloniaProperty tp when tp == InlineUIContainer.FontWeightProperty:
        
                break;

            case AvaloniaProperty tp when tp == InlineUIContainer.FontFamilyProperty:
        
                break;
            
            case AvaloniaProperty tp when tp == InlineUIContainer.FontSizeProperty:
        
                break;
            
            case AvaloniaProperty tp when tp == InlineUIContainer.FontStretchProperty:
        
                break;
            
            case AvaloniaProperty tp when tp == InlineUIContainer.FontStyleProperty:
        
                break;
            
            case AvaloniaProperty tp when tp == InlineUIContainer.ForegroundProperty:
        
                break;
            
            case AvaloniaProperty tp when tp == InlineUIContainer.BackgroundProperty:
        
                break;

            case AvaloniaProperty tp when tp == InlineUIContainer.BaselineAlignmentProperty:
        
                break;

            case AvaloniaProperty tp when tp == InlineUIContainer.TextDecorationsProperty:
        
                break;
        }
        
    }

    public EditableInlineUIContainer(Control c) : this() { SetChild(c); }

        
    internal int Id { get; set; }
    int IEditable.Id { get => Id; set => Id = value; }

    internal int MyParagraphId { get; set; }
    int IEditable.MyParagraphId { get => MyParagraphId; set => MyParagraphId = value; }

    internal FlowDocument MyFlowDoc { get; set; } = null!;
    FlowDocument IEditable.MyFlowDoc { get => MyFlowDoc; set => MyFlowDoc = value; }

    public int TextPositionOfInlineInParagraph { get; set; }

    internal string InlineText { get; private set; } = "@";
    string IEditable.InlineText { get => InlineText; set { InlineText = value; } }

    bool IEditable.IsFirstInlineOfParagraph { get; set; }
    
    internal bool IsLastInlineOfParagraph { get; set; }
    bool IEditable.IsLastInlineOfParagraph { get => IsLastInlineOfParagraph; set => IsLastInlineOfParagraph = value; }

    internal bool IsTableCellInline { get; set; }
    bool IEditable.IsTableCellInline { get => IsTableCellInline; set => IsTableCellInline = value; }

    internal IEditable? PreviousInline { get; set; }
    IEditable? IEditable.PreviousInline { get => PreviousInline; set => PreviousInline = value; }
    internal IEditable? NextInline { get; set; }
    IEditable? IEditable.NextInline { get => NextInline; set => NextInline = value; }

    public IEditable? GetPreviousInline => PreviousInline;
    public IEditable? GetNextInline => NextInline;

    public object Tag { get; set; } = null!;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
    public string FontName => "---";

    public int InlineLength => 1;
    public bool IsEmpty => false;
    
    
    public double InlineHeight => base.Child == null ? 0 : base.Child.Bounds.Height;

    
    internal int ImageNo;

    public IEditable Clone()
    {
        MyFlowDoc.disableUndoStack = true;

        EditableInlineUIContainer eIUC = new(base.Child)
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

    internal bool IsSelected { get; set; } = false;

#if DEBUG
    // FOR DEBUGGER PANEL
    internal InlineVisualizationProperties InlineVP { get; set; } = new();
    InlineVisualizationProperties IEditable.InlineVP { get => InlineVP; set => InlineVP = value; }
    public string InlineToolTip => "";
    public string DisplayInlineText { get => $"<UICONTAINER> => {(base.Child != null && base.Child.GetType() == typeof(Image) ? "Image" : "NoChild")}"; }
#endif


}


