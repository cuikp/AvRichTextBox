using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvRichTextBox;

public class BindableGrid : Grid
{
   public BindableGrid()
   {
      ColDefs.CollectionChanged += ColDefs_CollectionChanged;
      RowDefs.CollectionChanged += RowDefs_CollectionChanged;

      this.Loaded += BindableGrid_Loaded;
      
   }

   private void BindableGrid_Loaded(object? sender, RoutedEventArgs e)
   {
      RowDefinitions = RowDefs;
      ColumnDefinitions = ColDefs;
           
      this.UpdateLayout();
   }

   public static readonly StyledProperty<RowDefinitions> RowDefsProperty = AvaloniaProperty.Register<BindableGrid, RowDefinitions>(nameof(RowDefs), defaultValue: new RowDefinitions());
   public static readonly StyledProperty<ColumnDefinitions> ColDefsProperty = AvaloniaProperty.Register<BindableGrid, ColumnDefinitions>(nameof(ColDefs), defaultValue: new ColumnDefinitions());
   public RowDefinitions RowDefs { get => GetValue(RowDefsProperty); set => SetValue(RowDefsProperty, value); } 
   public ColumnDefinitions ColDefs { get => GetValue(ColDefsProperty); set => SetValue(ColDefsProperty, value); }

   private void ColDefs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
   {
      if (e.NewItems != null)
         foreach (ColumnDefinition cdef in e.NewItems)
            ColumnDefinitions.Add(cdef);
      if (e.OldItems != null)
         foreach (ColumnDefinition cdef in e.OldItems)
            ColumnDefinitions.Remove(cdef);
   }

   private void RowDefs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
   {
      if (e.NewItems != null)
         foreach (RowDefinition rdef in e.NewItems)
            RowDefinitions.Add(rdef);

      if (e.OldItems != null)
         foreach (RowDefinition rdef in e.OldItems)
            RowDefinitions.Remove(rdef);
   }

}
