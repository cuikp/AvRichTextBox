using Avalonia.Layout;
using Avalonia.Media;

namespace AvRichTextBox; 

internal class CellBorderThicknessChangedUndo(int tableId, int cellId, Thickness oldThickness, Thickness newThickness, FlowDocument flowDoc) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    public void PerformUndo()
    {
        DoThicknessChange(oldThickness);
    }

    public void PerformRedo()
    {
        DoThicknessChange(newThickness);
    }

    private void DoThicknessChange(Thickness targetThickness)
    {
        try
        {
            if (flowDoc.GetBlockFromId(tableId) is Table t && t.Cells.FirstOrDefault(c => c.Id == cellId) is Cell foundCell)
            {
                DisableUndoStack = true;
                foundCell.BorderThickness = targetThickness;
                DisableUndoStack = false;
            }

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at cellId: {cellId}"); }
        finally { DisableUndoStack = false; }
    }
}

internal class CellBorderBrushChangedUndo(int tableId, int cellId, IBrush oldBrush, IBrush newBrush, FlowDocument flowDoc) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    public void PerformUndo()
    {
        DoBorderBrushChange(oldBrush);
    }

    public void PerformRedo()
    {
        DoBorderBrushChange(newBrush);
    }

    private void DoBorderBrushChange(IBrush targetBrush)
    {
        try
        {
            if (flowDoc.GetBlockFromId(tableId) is Table t && t.Cells.FirstOrDefault(c => c.Id == cellId) is Cell foundCell)
            {
                DisableUndoStack = true;
                foundCell.BorderBrush = targetBrush;
                DisableUndoStack = false;
            }

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at cellId: {cellId}"); }
        finally { DisableUndoStack = false; }
    }
}

internal class CellBackgroundChangedUndo(int tableId, int cellId, IBrush oldBrush, IBrush newBrush, FlowDocument flowDoc) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    public void PerformUndo()
    {
        DoBackgroundChange(oldBrush);
    }

    public void PerformRedo()
    {
        DoBackgroundChange(newBrush);
    }

    private void DoBackgroundChange(IBrush targetBrush)
    {
        try
        {
            if (flowDoc.GetBlockFromId(tableId) is Table t && t.Cells.FirstOrDefault(c => c.Id == cellId) is Cell foundCell)
            {
                DisableUndoStack = true;
                foundCell.CellBackground = targetBrush;
                DisableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at cellId: {cellId}"); }
        finally { DisableUndoStack = false; }
    }
}

internal class CellVerticalAlignmentChangedUndo(int tableId, int cellId, VerticalAlignment oldVAlign, VerticalAlignment newVAlign, FlowDocument flowDoc) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    public void PerformUndo()
    {
        DoCellVerticalAlignmentChange(oldVAlign);
    }

    public void PerformRedo()
    {
        DoCellVerticalAlignmentChange(newVAlign);
    }

    private void DoCellVerticalAlignmentChange(VerticalAlignment targetVAlign)
    {
        try
        {
            if (flowDoc.GetBlockFromId(tableId) is Table t && t.Cells.FirstOrDefault(c => c.Id == cellId) is Cell foundCell)
            {
                DisableUndoStack = true;
                foundCell.CellVerticalAlignment = targetVAlign;
                DisableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at cellId: {cellId}"); }
        finally { DisableUndoStack = false; }
    }
}

internal class CellPaddingChangedUndo(int tableId, int cellId, Thickness oldPadding, Thickness newPadding, FlowDocument flowDoc) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    public void PerformUndo()
    {
        DoCellPaddingChange(oldPadding);
    }

    public void PerformRedo()
    {
        DoCellPaddingChange(newPadding);
    }

    private void DoCellPaddingChange(Thickness targetPadding)
    {
        try
        {
            if (flowDoc.GetBlockFromId(tableId) is Table t && t.Cells.FirstOrDefault(c => c.Id == cellId) is Cell foundCell)
            {
                DisableUndoStack = true;
                foundCell.Padding = targetPadding;
                DisableUndoStack = false;
            }
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at cellId: {cellId}"); }
        finally { DisableUndoStack = false; }
    }
}
