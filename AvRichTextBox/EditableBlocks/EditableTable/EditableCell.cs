using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvRichTextBox;

public class EditableCell : Border
{
    public delegate void MouseMoveHandler(EditableCell sender, Point cellPoint);
    public event MouseMoveHandler? MouseMove;

    public delegate void MouseLeaveHandler(EditableCell sender);
    public event MouseLeaveHandler? MouseLeave;

    public EditableCell()
   {
      this.SizeChanged += EditableCell_SizeChanged;
   }

   private void EditableCell_SizeChanged(object? sender, SizeChangedEventArgs e)
   {
      if (this.DataContext is not Cell thisCell) return;
      thisCell.Height = this.Bounds.Height;

        Dispatcher.UIThread.Post(() =>
        {
            //thisCell.OwningTable.Width = thisCell.OwningTable.ColDefs.Sum(cd => cd.Width.Value);
            thisCell.OwningTable.Height = thisCell.OwningTable.RowDefs.Sum(rdef => rdef.Height.Value);
            
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


