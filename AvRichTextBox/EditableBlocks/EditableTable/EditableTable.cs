using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;

namespace AvRichTextBox;

public partial class EditableTable : ItemsControl
{
   private const double ResizeGripSize = 5;
   private const double MinColumnWidth = 24;
   private const double MinRowHeight = 24;

   private Point _resizeStartPoint;
   private ResizeMode _resizeMode;
   private int _resizeIndex = -1;
   private double _resizeStartPrimarySize;
   private double _resizeStartSecondarySize;

   public bool IsEditable { get; set; } = true;

   public EditableTable()
   {
      Loaded += EditableTable_Loaded;
   }

   private void EditableTable_Loaded(object? sender, RoutedEventArgs e)
   {    
      this.UpdateLayout();
   }

   protected override void OnPointerMoved(PointerEventArgs e)
   {
      base.OnPointerMoved(e);

      if (DataContext is not Table table)
         return;

      Point position = e.GetPosition(this);
      if (_resizeMode != ResizeMode.None)
      {
         ResizeTable(table, position);
         e.Handled = true;
         return;
      }

      ResizeHit hit = GetResizeHit(table, position);
      Cursor = hit.Mode switch
      {
         ResizeMode.Column => new Cursor(StandardCursorType.SizeWestEast),
         ResizeMode.Row => new Cursor(StandardCursorType.SizeNorthSouth),
         _ => null
      };
   }

   protected override void OnPointerPressed(PointerPressedEventArgs e)
   {
      base.OnPointerPressed(e);

      if (!IsEditable || DataContext is not Table table)
         return;

      Point position = e.GetPosition(this);
      ResizeHit hit = GetResizeHit(table, position);
      if (hit.Mode == ResizeMode.None)
         return;

      _resizeMode = hit.Mode;
      _resizeIndex = hit.Index;
      _resizeStartPoint = position;
      if (_resizeMode == ResizeMode.Column)
      {
         _resizeStartPrimarySize = table.ColDefs[_resizeIndex].Width.Value;
         _resizeStartSecondarySize = table.ColDefs[_resizeIndex + 1].Width.Value;
      }
      else
      {
         _resizeStartPrimarySize = table.RowDefs[_resizeIndex].Height.Value;
         _resizeStartSecondarySize = table.RowDefs[_resizeIndex + 1].Height.Value;
      }

      e.Pointer.Capture(this);
      e.Handled = true;
   }

   protected override void OnPointerReleased(PointerReleasedEventArgs e)
   {
      base.OnPointerReleased(e);

      if (_resizeMode == ResizeMode.None)
         return;

      _resizeMode = ResizeMode.None;
      _resizeIndex = -1;
      e.Pointer.Capture(null);
      e.Handled = true;
   }

   private void ResizeTable(Table table, Point position)
   {
      if (_resizeMode == ResizeMode.Column)
      {
         double delta = position.X - _resizeStartPoint.X;
         double primaryWidth = Math.Max(MinColumnWidth, _resizeStartPrimarySize + delta);
         double secondaryWidth = Math.Max(MinColumnWidth, _resizeStartSecondarySize - (primaryWidth - _resizeStartPrimarySize));
         primaryWidth = _resizeStartPrimarySize + (_resizeStartSecondarySize - secondaryWidth);

         table.ColDefs[_resizeIndex].Width = new GridLength(primaryWidth, GridUnitType.Pixel);
         table.ColDefs[_resizeIndex + 1].Width = new GridLength(secondaryWidth, GridUnitType.Pixel);
      }
      else if (_resizeMode == ResizeMode.Row)
      {
         double delta = position.Y - _resizeStartPoint.Y;
         double primaryHeight = Math.Max(MinRowHeight, _resizeStartPrimarySize + delta);
         double secondaryHeight = Math.Max(MinRowHeight, _resizeStartSecondarySize - (primaryHeight - _resizeStartPrimarySize));
         primaryHeight = _resizeStartPrimarySize + (_resizeStartSecondarySize - secondaryHeight);

         table.RowDefs[_resizeIndex].Height = new GridLength(primaryHeight, GridUnitType.Pixel);
         table.RowDefs[_resizeIndex + 1].Height = new GridLength(secondaryHeight, GridUnitType.Pixel);
      }

      InvalidateMeasure();
      UpdateLayout();
   }

   private static ResizeHit GetResizeHit(Table table, Point position)
   {
      double x = 0;
      for (int index = 0; index < table.ColDefs.Count - 1; index++)
      {
         x += table.ColDefs[index].Width.Value;
         if (Math.Abs(position.X - x) <= ResizeGripSize)
            return new ResizeHit(ResizeMode.Column, index);
      }

      double y = 0;
      for (int index = 0; index < table.RowDefs.Count - 1; index++)
      {
         y += table.RowDefs[index].Height.Value;
         if (Math.Abs(position.Y - y) <= ResizeGripSize)
            return new ResizeHit(ResizeMode.Row, index);
      }

      return new ResizeHit(ResizeMode.None, -1);
   }

   public static readonly StyledProperty<ObservableCollection<EditableCell>> CellsProperty = AvaloniaProperty.Register<EditableTable, ObservableCollection<EditableCell>>(nameof(Cells), defaultValue: []);
   public ObservableCollection<EditableCell> Cells { get => GetValue(CellsProperty); set => SetValue(CellsProperty, value); }

   //private void Cells_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
   //{
   //   this.UpdateLayout();
   //}
  
   ////public string GetText => string.Join("", ((Table)this.DataContext).Inlines.ToList().ConvertAll(edinline => edinline.InlineText));

}

internal readonly record struct ResizeHit(ResizeMode Mode, int Index);

internal enum ResizeMode
{
   None,
   Column,
   Row
}


