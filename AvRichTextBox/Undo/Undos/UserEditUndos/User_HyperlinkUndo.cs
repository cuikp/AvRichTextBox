using Avalonia.Threading;
using DynamicData;
using System.Collections.ObjectModel;

namespace AvRichTextBox; 

/// <summary>
/// Undo for hyperlink insert, update (text/URL), and remove operations.
/// Restores the full set of affected paragraph(s) from clones taken before the edit.
/// </summary>
internal class HyperlinkParagraphUndo : IEditDo
{
    // Convenience constructor for single-paragraph operations (update / remove)
    internal HyperlinkParagraphUndo(Paragraph parClone, int parIndex, FlowDocument flowDoc, int origSelectionStart, int undoEditOffset)
       : this([parClone], parIndex, flowDoc, origSelectionStart, undoEditOffset, false) { }

    private readonly List<Block> blockClones;
    private readonly int parIndex;
    private readonly FlowDocument flowDoc;
    private readonly int origSelectionStart;
    private readonly bool firstOrLastParWasDeleted;
    private readonly bool firstParWasDeleted;
    private readonly bool lastParWasDeleted;

    public int UndoEditOffset { get; }
    // UpdateTextRanges is handled inside PerformUndo via Dispatcher.UIThread.Post;
    // returning false prevents Undo() from issuing a second, conflicting UpdateTextRanges call.
    public bool UpdateTextRanges => false;

    internal HyperlinkParagraphUndo(
       List<Block> blockClones,
       int parIndex,
       FlowDocument flowDoc,
       int origSelectionStart,
       int undoEditOffset,
       bool firstParWasDeleted = false,
       bool lastParWasDeleted = false
       )
    {
        this.blockClones = blockClones;
        this.parIndex = parIndex;
        this.flowDoc = flowDoc;
        this.origSelectionStart = origSelectionStart;
        this.firstParWasDeleted = firstParWasDeleted;
        this.lastParWasDeleted = lastParWasDeleted;
        UndoEditOffset = undoEditOffset;
    }

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableRunTextUndo = true;

            int lengthBefore = flowDoc.Text.Length;
            flowDoc.RestoreDeletedBlocks(blockClones, parIndex, firstParWasDeleted, lastParWasDeleted, flowDoc.Blocks, parIndex); //$$$$$$$$$$$$$$$$$
            flowDoc.disableRunTextUndo = false;
            int lengthAfter = flowDoc.Text.Length;
            flowDoc.UpdateTextRanges(blockClones[0].StartInDoc, lengthAfter - lengthBefore);

            Dispatcher.UIThread.Post(() =>
            {
                flowDoc.Selection.Start = origSelectionStart;
                flowDoc.Selection.End = origSelectionStart;
                flowDoc.UpdateSelection();
            });
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at parIndex: " + parIndex); }
    }

    public void PerformRedo()
    {
    }

}



