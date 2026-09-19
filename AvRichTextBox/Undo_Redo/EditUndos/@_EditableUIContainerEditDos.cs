using Avalonia.Controls;

namespace AvRichTextBox; 

internal class EditableUIContainerChildEditDo(int parId, int uicId, Control? oldChild, Control? newChild, FlowDocument flowDoc) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(parId) is Paragraph p && p.Inlines.FirstOrDefault(il=> il.Id == uicId) is EditableInlineUIContainer eIUC)
            {
                DisableUndoStack =  true;
                eIUC.SetChild(oldChild);
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at uicId: {uicId}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(parId) is Paragraph p && p.Inlines.FirstOrDefault(il => il.Id == uicId) is EditableInlineUIContainer eIUC)
            {
                DisableUndoStack =  true;
                eIUC.SetChild(newChild);
                DisableUndoStack =  false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at uicId: {uicId}"); }
        finally { DisableUndoStack =  false; }
    }
}

