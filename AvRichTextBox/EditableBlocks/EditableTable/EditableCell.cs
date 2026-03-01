using Avalonia.Controls;

namespace AvRichTextBox;

public class EditableCell : Border
{

   public EditableCell()
   {
      this.SizeChanged += EditableCell_SizeChanged;
   }

   private void EditableCell_SizeChanged(object? sender, SizeChangedEventArgs e)
   {
      if (this.DataContext is not Cell cell) return;
      cell.Height = this.Bounds.Height;
   }
}


