using Avalonia.Controls;

namespace AvRichTextBox; 

internal class EditableUIContainerChildUndo(int parId, int uicId, Control? oldChildClone, FlowDocument flowDoc) : IUndo
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
                eIUC.SetChild(oldChildClone);
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at uicId: {uicId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

