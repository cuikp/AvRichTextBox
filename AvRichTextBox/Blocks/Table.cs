using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AvRichTextBox;

public partial class Table : Block
{

   public Thickness BorderThickness { get; set; } = new(1);
   public ObservableCollection<Cell> Cells { get; set; } = [];
   public ColumnDefinitions ColDefs { get; set; } = [];
   public RowDefinitions RowDefs { get; set; } = [];
   public double Height { get; set; } = 500;
   public double Width { get; set; } = 500;
   public HorizontalAlignment TableAlignment { get; set; } = HorizontalAlignment.Left;

   public Table() { }

   public Table(int cols, int rows) 
   {
      double eqWidth = Width / cols;
      double eqHeight = Height / rows;

      int cellno = 0;

      for (int rowno = 0; rowno < rows; rowno++)
      {
         RowDefs.Add(new RowDefinition(eqHeight, GridUnitType.Pixel));
         
         for (int colno = 0; colno < cols; colno++)
         {
            ColDefs.Add(new ColumnDefinition(eqWidth, GridUnitType.Pixel));
            Paragraph newPar = new();
            newPar.Inlines.Add(new EditableRun("col:" + colno));
            newPar.Inlines.Add(new EditableLineBreak());
            newPar.Inlines.Add(new EditableRun("row:" + rowno));
            
            Cell newCell = new()
            {
               ColNo = colno,
               RowNo = rowno,
               BorderThickness = new(3),
               BorderBrush = Brushes.Red,
               CellContent = newPar
            };
            Cells.Add(newCell);
            cellno++;
         }
      }



   }


   internal void InsertColumn(int idx)
   {

   }

 
}


public class Cell 
{
   public Cell() { }

   public Block CellContent { get; set; } = new Paragraph();
   public Thickness BorderThickness { get; set; } = new (1);
   public IBrush BorderBrush { get; set; } = null!;
   public int ColNo { get; set; }
   public int RowNo { get; set; }

   public double Width { get; set; } = 100;
   public double Height { get; set; } = 60;
   
}

