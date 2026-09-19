using Avalonia.Media;
using DynamicData;

namespace AvRichTextBox;

internal class ParagraphTextAlignmentChangeUndo(int parId, TextAlignment oldTextAlign, TextAlignment newTextAlign, FlowDocument flowDoc) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    public void PerformUndo()
    {
        DoTextAlignmentChange(oldTextAlign);
    }

    public void PerformRedo()
    {
        DoTextAlignmentChange(newTextAlign);
    }

    private void DoTextAlignmentChange(TextAlignment targetTAlign)
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.GetBlockFromId(parId) is Paragraph par)
                par.TextAlignment = targetTAlign;

            DisableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at parId: {parId}\n{ex.Message}"); }
        finally { DisableUndoStack = false; }

    }
}


internal class ParagraphLineHeightChangeUndo(int parId, double oldLineHeight, double newLineHeight, FlowDocument flowDoc) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    public void PerformUndo()
    {
        DoLineHeightChange(oldLineHeight);
    }

    public void PerformRedo()
    {
        DoLineHeightChange(newLineHeight);
    }

    private void DoLineHeightChange(double targetLineHeight)
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.GetBlockFromId(parId) is Paragraph par)
                par.LineHeight = targetLineHeight;

            DisableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at parId: {parId}\n{ex.Message}"); }
        finally { DisableUndoStack = false; }
    }
}


internal class InsertInlineAtUndo(int parId, int inlineId, FlowDocument flowDoc) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    int origInlineIndex = -1;
    IEditable origInline = null!;

    public void PerformUndo()
    {
        try
        {
            
            DisableUndoStack =  true;

            if ( flowDoc.GetBlockFromId(parId) is Paragraph par)
            {
                if (par.Inlines.FirstOrDefault(il => il.Id == inlineId) is IEditable ied)
                {
                    origInlineIndex = par.Inlines.IndexOf(ied);
                    origInline = ied;
                    par.Inlines.Remove(ied);
                }
                    
            }
            DisableUndoStack =  false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at inlineId: {inlineId}\n{ex.Message}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.GetBlockFromId(parId) is Paragraph par)
                par.Inlines.Insert(origInlineIndex, origInline);
            DisableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at inlineId: {inlineId}\n{ex.Message}"); }
        finally { DisableUndoStack = false; }
    }
}

internal class InsertInlinesAtUndo(int parId, List<int> inlineIds, FlowDocument flowDoc) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    int origInlineIndex = -1;
    List<IEditable> origInlines = [];

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack =  true;

            origInlines.Clear();

            if (flowDoc.GetBlockFromId(parId) is Paragraph par)
            {
                int inlineno = 0;
                foreach (int id in inlineIds)
                {
                    if (par.Inlines.FirstOrDefault(il => il.Id == id) is IEditable ied)
                    {
                        if (inlineno == 0)
                            origInlineIndex = par.Inlines.IndexOf(ied);

                        origInlines.Add(ied);
                        par.Inlines.Remove(ied);
                    }
                    inlineno++;
                }
            }
            DisableUndoStack =  false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} with inlineIds: {inlineIds.Count}\n{ex.Message}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.GetBlockFromId(parId) is Paragraph par)
                par.Inlines.AddOrInsertRange(origInlines, origInlineIndex);

            DisableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} with inlineIds: {inlineIds.Count}\n{ex.Message}"); }
        finally { DisableUndoStack = false; }
    }
}

internal class RemoveInlineUndo(int parId, int origInlineIndex, IEditable removedInlineClone, FlowDocument flowDoc) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    int removedInlineId = -1;

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack =  true;

            if (flowDoc.GetBlockFromId(parId) is Paragraph par)
            {
                removedInlineId = removedInlineClone.Id;
                par.Inlines.Insert(origInlineIndex, removedInlineClone);

            }
            DisableUndoStack =  false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at inline index: {origInlineIndex}\n{ex.Message}"); }
        finally { DisableUndoStack =  false; }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.GetBlockFromId(parId) is Paragraph par && par.Inlines.FirstOrDefault(il => il.Id == removedInlineId) is IEditable ied)
                par.Inlines.Remove(ied);

            DisableUndoStack = false;
        }
        catch (Exception ex) { Debug.WriteLine($"Failed {this.GetType().Name} at inline index: {origInlineIndex}\n{ex.Message}"); }
        finally { DisableUndoStack = false; }

    }
}
