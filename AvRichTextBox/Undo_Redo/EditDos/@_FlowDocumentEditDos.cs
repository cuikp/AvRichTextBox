
namespace AvRichTextBox; 

internal class FlowDocumentPagePaddingChangedEditDo(Thickness oldPagePadding, Thickness newPagePadding, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;
            flowDoc.PagePadding = oldPagePadding;
            flowDoc.disableUndoStack = false;
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} for PagePadding: { oldPagePadding }"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            flowDoc.disableUndoStack = true;
            flowDoc.PagePadding = newPagePadding;
            flowDoc.disableUndoStack = false;
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} for PagePadding: {oldPagePadding}"); }
        finally { { flowDoc.disableUndoStack = false; } }
    }
}

