namespace AvRichTextBox;

/// <summary>A paragraph-sized custom element using the editor's existing block and selection model.</summary>
public sealed class CustomBlock : Paragraph
{
    internal CustomBlock() { }

    public CustomBlock(CustomContent definition)
    {
        Inlines.Add(new CustomInline(definition));
    }

    public CustomInline? Content => Inlines.OfType<CustomInline>().FirstOrDefault();

    internal override CustomBlock PropertyClone()
    {
        bool previous = DisableUndoStack;
        try
        {
            DisableUndoStack = true;
            var clone = new CustomBlock();
            clone.CopyPropertiesFromParagraph(this);
            clone.OwningTableId = OwningTableId;
            clone.OwningCellId = OwningCellId;
            clone.StartInDoc = StartInDoc;
            clone.LeftIndent = LeftIndent;
            clone.FirstLineIndent = FirstLineIndent;
            clone.ListMarker = ListMarker;
            return clone;
        }
        finally { DisableUndoStack = previous; }
    }

    internal override CustomBlock FullClone(bool keepId)
    {
        bool previous = DisableUndoStack;
        try
        {
            DisableUndoStack = true;
            var clone = PropertyClone();
            if (keepId) clone.Id = Id;
            foreach (var inline in Inlines) clone.Inlines.Add(inline.CloneWithId());
            return clone;
        }
        finally { DisableUndoStack = previous; }
    }
}
