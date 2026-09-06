using Avalonia.Layout;
using Avalonia.Media;

namespace AvRichTextBox; 

internal class CellBorderThicknessChangedUndo(int tableId, int cellId, Thickness oldThickness, FlowDocument flowDoc) : IUndo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(tableId) is Table t && t.Cells.FirstOrDefault(c=> c.Id == cellId) is Cell foundCell)
            {
                flowDoc.disableUndoStack = true;
                foundCell.BorderThickness = oldThickness;
                flowDoc.disableUndoStack = false;
            }

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at cellId: {cellId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

internal class CellBorderBrushChangedUndo(int tableId, int cellId, IBrush oldBrush, FlowDocument flowDoc) : IUndo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(tableId) is Table t && t.Cells.FirstOrDefault(c=> c.Id == cellId) is Cell foundCell)
            {
                flowDoc.disableUndoStack = true;
                foundCell.BorderBrush = oldBrush;
                flowDoc.disableUndoStack = false;
            }

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at cellId: {cellId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

internal class CellBackgroundChangedUndo(int tableId, int cellId, IBrush oldBrush, FlowDocument flowDoc) : IUndo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(tableId) is Table t && t.Cells.FirstOrDefault(c => c.Id == cellId) is Cell foundCell)
            {
                flowDoc.disableUndoStack = true;
                foundCell.CellBackground = oldBrush;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at cellId: {cellId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

internal class CellVerticalAlignmentChangedUndo(int tableId, int cellId, VerticalAlignment oldVAlign, FlowDocument flowDoc) : IUndo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(tableId) is Table t && t.Cells.FirstOrDefault(c => c.Id == cellId) is Cell foundCell)
            {
                flowDoc.disableUndoStack = true;
                foundCell.CellVerticalAlignment = oldVAlign;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at cellId: {cellId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}

internal class CellPaddingChangedUndo(int tableId, int cellId, Thickness oldPadding, FlowDocument flowDoc) : IUndo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        try
        {
            if (flowDoc.GetBlockFromId(tableId) is Table t && t.Cells.FirstOrDefault(c => c.Id == cellId) is Cell foundCell)
            {
                flowDoc.disableUndoStack = true;
                foundCell.Padding = oldPadding;
                flowDoc.disableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at cellId: {cellId}"); }
        finally { flowDoc.disableUndoStack = false; }
    }
}
