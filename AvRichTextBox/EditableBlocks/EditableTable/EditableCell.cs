using Avalonia.Controls;
using Avalonia.Remote.Protocol;
using Avalonia.Threading;

namespace AvRichTextBox;

public class EditableCell : Border
{
    internal delegate void MouseMoveHandler(EditableCell sender, Point cellPoint);
    internal event MouseMoveHandler? MouseMove;

    internal delegate void MouseLeaveHandler(EditableCell sender);
    internal event MouseLeaveHandler? MouseLeave;

    public EditableCell()
    {
        this.SizeChanged += EditableCell_SizeChanged;
        this.PropertyChanged += EditableCell_PropertyChanged;
    }

    private void EditableCell_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (this.DataContext is not Cell thisCell) return;

        switch (e.Property.Name)
        {
            case "BorderThickness":
                this.UpdateLayout();
                thisCell.OwningTable.UpdateColAndRowPoints();
                break;
        }
    }

    private void EditableCell_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (this.DataContext is not Cell thisCell) return;
        thisCell.Height = this.Bounds.Height;

        Dispatcher.UIThread.Post(() =>
        {
            //thisCell.OwningTable.Width = thisCell.OwningTable.ColDefs.Sum(cd => cd.Width.Value);
            //thisCell.OwningTable.Height = thisCell.OwningTable.RowDefs.Sum(rdef => rdef.Height.Value);
            thisCell.OwningTable.Height = thisCell.OwningTable.RowDefs.Sum(rdef => rdef.Height.Value) + thisCell.OwningTable.BorderThickness.Top + thisCell.OwningTable.BorderThickness.Bottom;
        });

    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        MouseLeave?.Invoke(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        MouseMove?.Invoke(this, e.GetPosition(this));
    }

}


