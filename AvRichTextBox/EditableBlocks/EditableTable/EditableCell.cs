using Avalonia.Controls;
using Avalonia.Threading;

namespace AvRichTextBox;

public class EditableCell : Border
{
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
            thisCell.OwningTable.Height = thisCell.OwningTable.RowDefs.Sum(rdef => rdef.Height.Value);
        });

    }


}


