using Avalonia.Controls;

namespace AvRichTextBox; 

internal class EditableUIContainerChildEditDo(int parId, int uicId, Control? oldChild, Control? newChild, FlowDocument flowDoc) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(parId) is Paragraph p && p.Inlines.FirstOrDefault(il=> il.Id == uicId) is EditableInlineUIContainer eIUC)
            {
                flowDoc.disableUndoStack = true;
                eIUC.SetChild(oldChild);
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at uicId: {uicId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(parId) is Paragraph p && p.Inlines.FirstOrDefault(il => il.Id == uicId) is EditableInlineUIContainer eIUC)
            {
                flowDoc.disableUndoStack = true;
                eIUC.SetChild(newChild);
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at uicId: {uicId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

