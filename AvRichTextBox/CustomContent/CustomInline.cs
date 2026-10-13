using Avalonia.Controls;

namespace AvRichTextBox;

/// <summary>An atomic inline whose application data survives cloning and XAML serialization.</summary>
public sealed class CustomInline : EditableInlineUIContainer
{
    private CustomContent _definition;

    public CustomInline(CustomContent definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definition = definition;
        RefreshContent();
    }

    public CustomContent Definition
    {
        get => _definition;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_definition == value) return;
            var old = _definition;
            var control = CreateControl(value);
            _definition = value;
            SetDisplay(control);
            if (MyFlowDoc != null && IsAttachedToDocument && !DisableUndoStack)
            {
                MyFlowDoc.Undos.Add(new DefinitionEdit(MyFlowDoc, MyParagraphId, Id, old, value));
                MyFlowDoc.Redos.Clear();
            }
        }
    }

    protected override void OnFlowDocumentChanged() => RefreshContent();

    internal void RefreshContent() => SetDisplay(CreateControl(_definition));

    private Control CreateControl(CustomContent definition)
    {
        var control = MyFlowDoc?.CreateCustomControl(definition) ?? new TextBlock { Text = definition.FallbackText };
        if (control.Parent != null)
            throw new InvalidOperationException("A custom content factory must return a new, unparented control for each call.");
        return control;
    }

    private void SetDisplay(Control control)
    {
        bool previous = DisableUndoStack;
        try
        {
            DisableUndoStack = true;
            SetChild(control);
        }
        finally { DisableUndoStack = previous; }
    }

    public override CustomInline Clone() => new(_definition)
    {
        MyParagraphId = MyParagraphId,
        MyFlowDoc = MyFlowDoc,
        TextPositionOfInlineInParagraph = TextPositionOfInlineInParagraph,
        IsLastInlineOfParagraph = IsLastInlineOfParagraph,
        IsTableCellInline = IsTableCellInline,
        FontFamily = FontFamily,
        FontSize = FontSize,
        BaselineAlignment = BaselineAlignment
    };

    public override CustomInline CloneWithId()
    {
        var clone = Clone();
        clone.Id = Id;
        return clone;
    }

    private sealed class DefinitionEdit(FlowDocument document, int paragraphId, int inlineId,
        CustomContent before, CustomContent after) : IEditDo
    {
        public int EditOffset { get; set; }
        public int UpdateTextRangesFromCharIdx { get; set; }
        public bool UpdateTextRanges => false;
        public bool DoNextUndo => false;
        public bool DoNextRedo => false;
        public void PerformUndo() => Set(before);
        public void PerformRedo() => Set(after);

        private void Set(CustomContent definition)
        {
            var inline = document.AllParagraphs.First(paragraph => paragraph.Id == paragraphId)
                .Inlines.OfType<CustomInline>().First(item => item.Id == inlineId);
            inline.Definition = definition;
        }
    }
}
