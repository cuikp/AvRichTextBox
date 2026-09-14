
namespace AvRichTextBox; 

internal class FlowDocumentPagePaddingChangedEditDo(Thickness oldPagePadding, Thickness newPagePadding, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack =  true;
            flowDoc.PagePadding = oldPagePadding;
            DisableUndoStack =  false;
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} for PagePadding: { oldPagePadding }"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack =  true;
            flowDoc.PagePadding = newPagePadding;
            DisableUndoStack =  false;
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} for PagePadding: {oldPagePadding}"); }
        finally { { DisableUndoStack =  false; } }
    }
}

