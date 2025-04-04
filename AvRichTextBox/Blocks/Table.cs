//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Media;
//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using System.Runtime.CompilerServices;

//namespace AvRichTextBox;

//public partial class Table : Block, INotifyPropertyChanged
//{
//   public new event PropertyChangedEventHandler? PropertyChanged;
//   private new void NotifyPropertyChanged([CallerMemberName] string propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

//   public Thickness BorderThickness => new Thickness(1);

//   public ObservableCollection<Border> Cells { get; set; } = [];
//   public ColumnDefinitions ColDefs { get; set; } = [];
//   public RowDefinitions RowDefs { get; set; } = [];


//   public Table(int noCols, int noRows, double TableWidth)
//   {
//      double eqSpacedColWidth = 10D / (double)noCols;

//      for (int rowno = 0; rowno < noRows; rowno++)
//         this.RowDefs.Add(new RowDefinition());
//      for (int colno = 0; colno < noCols; colno++)
//         this.ColDefs.Add(new ColumnDefinition(eqSpacedColWidth, GridUnitType.Star));

//      for (int rowno = 0; rowno < noRows; rowno++)
//      {
//         for (int colno = 0; colno < noCols; colno++)
//         {
//            Border cellborder = new() { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.5) };
//            SelectableTextBlock seltb = new() { Text = "this is some text, with wrapping as well." };
//            seltb.Background = Brushes.White;
//            seltb.TextWrapping = TextWrapping.Wrap;
//            cellborder.Child = seltb;
//            Cells.Add(cellborder);
//            Grid.SetRow(cellborder, rowno);
//            Grid.SetColumn(cellborder, colno);
//         }
//      }


//   }





//}

