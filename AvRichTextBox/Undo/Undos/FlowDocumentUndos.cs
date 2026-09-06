
namespace AvRichTextBox; 

internal class FlowDocumentPagePaddingChangedUndo(Thickness oldPagePadding, FlowDocument flowDoc) : IUndo
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
}

