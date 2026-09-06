using Avalonia.Media;

namespace AvRichTextBox;

internal class ParagraphTextAlignmentChangeUndo(int parId, TextAlignment oldTextAlign, FlowDocument flowDoc) : IUndo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;

            if (flowDoc.GetBlockFromId(parId) is Paragraph par)
                par.TextAlignment = oldTextAlign;



            flowDoc.disableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at parId: {parId}\n{ex.Message}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}


internal class ParagraphLineHeightChangeUndo(int parId, double oldLineHeight, FlowDocument flowDoc) : IUndo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;

            if (flowDoc.GetBlockFromId(parId) is Paragraph par)
                par.LineHeight = oldLineHeight;

            flowDoc.disableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at parId: {parId}\n{ex.Message}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}