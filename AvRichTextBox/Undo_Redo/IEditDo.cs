namespace AvRichTextBox;

internal interface IEditDo
{
    public void PerformUndo();
    public void PerformRedo();
    public int UndoEditOffset { get; }
    public bool UpdateTextRanges { get; }
}

internal class EditablePropertyAssociation
{
    internal int InlineId { get; set; }
    internal int BlockId { get; set; }
    internal EditableRun keepERun = null!;
    internal object OrigPropertyValue { get; set; }
    internal object NewPropertyValue { get; set; } = null!;
    internal FlowDocument.FormatRunsAction? FormatRuns { get; set; }

    internal EditablePropertyAssociation(int blockId, int inlineId, FlowDocument.FormatRunsAction formatRuns, object origPropertyValue, object newPropertyValue)
    {
        BlockId = blockId;
        InlineId = inlineId;
        FormatRuns = formatRuns;
        OrigPropertyValue = origPropertyValue;
        NewPropertyValue = newPropertyValue;
    }
}