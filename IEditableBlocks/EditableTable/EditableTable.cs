using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;

namespace AvRichTextBox;


public partial class EditableTable : Grid, IEditableBlock
{
   ////public bool IsEditable { get; set; } = true;
   ////SolidColorBrush cursorBrush = new SolidColorBrush(Colors.Cyan, 0.55);

   //public static readonly StyledProperty<ObservableCollection<Border>> CellsProperty = AvaloniaProperty.Register<EditableTable, ObservableCollection<Border>>(nameof(Cells));
   //public static readonly StyledProperty<ColumnDefinitions> ColDefsProperty = AvaloniaProperty.Register<EditableTable, ColumnDefinitions>(nameof(ColDefs));
   //public static readonly StyledProperty<RowDefinitions> RowDefsProperty = AvaloniaProperty.Register<EditableTable, RowDefinitions>(nameof(RowDefs));
   
   //public ColumnDefinitions ColDefs
   //{
   //   get => GetValue(ColDefsProperty);
   //   set  { SetValue(ColDefsProperty, value); }
   //}

   //public RowDefinitions RowDefs
   //{
   //   get => GetValue(RowDefsProperty);
   //   set { SetValue(RowDefsProperty, value); }
   //}

   //public ObservableCollection<Border> Cells
   //{
   //   get => GetValue(CellsProperty);
   //   set => SetValue(CellsProperty, value);
   //}

   //public EditableTable()
   //{
   //   this.Loaded += EditableTable_Loaded;
      
   //}

   //private void Cells_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
   //{
   //   foreach (Border newCell in e.NewItems)
   //      this.Children.Add(newCell);
   //}

   //private void ColDefs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
   //{
   //   foreach (ColumnDefinition cdef in e.NewItems)
   //      ColumnDefinitions.Add(cdef);
   //   foreach (ColumnDefinition cdef in e.OldItems)
   //      ColumnDefinitions.Remove(cdef);

   //}

   //private void RowDefs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
   //{
   //   foreach (RowDefinition rdef in e.NewItems)
   //      RowDefinitions.Add(rdef);
   //   foreach (RowDefinition rdef in e.OldItems)
   //      RowDefinitions.Remove(rdef);

   //}

   //private void EditableTable_Loaded(object? sender, RoutedEventArgs e)
   //{
   //   //ColDefs.CollectionChanged += ColDefs_CollectionChanged;
   //   //RowDefs.CollectionChanged += RowDefs_CollectionChanged;
   //   Cells.CollectionChanged += Cells_CollectionChanged;

   //   RowDefinitions = RowDefs;
   //   ColumnDefinitions = ColDefs;

   //   foreach (Border b in Cells)
   //   {
   //      this.Children.Add(b);
   //      //Debug.WriteLine(Grid.GetRow(b));
   //   }
      
   //   //Debug.WriteLine("cellcount=" + Cells.Count);

   //   this.UpdateLayout();
   //}

   ////public string Text => string.Join("", ((Table)this.DataContext).Inlines.ToList().ConvertAll(edinline => edinline.InlineText));



}

