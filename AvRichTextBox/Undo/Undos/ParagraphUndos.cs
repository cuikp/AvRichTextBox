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


internal class InsertInlineAtUndo(int parId, int inlineId, FlowDocument flowDoc) : IUndo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;

            if ( flowDoc.GetBlockFromId(parId) is Paragraph par)
            {
                if (par.Inlines.FirstOrDefault(il => il.Id == inlineId) is IEditable ied)
                    par.Inlines.Remove(ied);
            }
            flowDoc.disableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at inlineId: {inlineId}\n{ex.Message}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

internal class InsertInlinesAtUndo(int parId, List<int> inlineIds, FlowDocument flowDoc) : IUndo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;

            if ( flowDoc.GetBlockFromId(parId) is Paragraph par)
            {
                foreach (int id in inlineIds)
                {
                    if (par.Inlines.FirstOrDefault(il => il.Id == id) is IEditable ied)
                        par.Inlines.Remove(ied);
                }
            }
            flowDoc.disableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} with inlineIds: {inlineIds.Count}\n{ex.Message}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

internal class RemoveInlineUndo(int parId, int origInlineIndex, IEditable removedInlineClone, FlowDocument flowDoc) : IUndo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableUndoStack = true;

            if (flowDoc.GetBlockFromId(parId) is Paragraph par)
                par.Inlines.Insert(origInlineIndex, removedInlineClone);

            flowDoc.disableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at inline index: {origInlineIndex}\n{ex.Message}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}
