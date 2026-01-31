using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;

namespace AvRichTextBox;

public partial class EditableTable : Grid, IEditableBlock
{
   public bool IsEditable { get; set; } = true;

   public EditableTable()
   {
      Loaded += EditableTable_Loaded;
   }

   private void EditableTable_Loaded(object? sender, RoutedEventArgs e)
   {    
      this.UpdateLayout();
   }

   public static readonly StyledProperty<ObservableCollection<EditableCell>> CellsProperty = AvaloniaProperty.Register<EditableTable, ObservableCollection<EditableCell>>(nameof(Cells), defaultValue: []);
   public ObservableCollection<EditableCell> Cells { get => GetValue(CellsProperty); set => SetValue(CellsProperty, value); }

   private void Cells_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
   {
      this.UpdateLayout();
   }
  
   ////public string GetText => string.Join("", ((Table)this.DataContext).Inlines.ToList().ConvertAll(edinline => edinline.InlineText));

}


