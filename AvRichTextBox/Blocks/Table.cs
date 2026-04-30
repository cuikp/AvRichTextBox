using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvRichTextBox;

public partial class Table : Block
{
   public Thickness BorderThickness { get; set; } = new(1);
   public ISolidColorBrush BorderBrush { get; set; } = Brushes.Black;
   public ObservableCollection<Cell> Cells { get; set; } = [];
   public ColumnDefinitions ColDefs { get; set; } = [];
   public RowDefinitions RowDefs { get; set; } = [];
   public double Height { get; set; } = 450;
   public double Width { get; set; } = 500;
   public HorizontalAlignment TableAlignment { get; set; } = HorizontalAlignment.Left;

   internal IBrush SelectionBrush = Brushes.LightSteelBlue;

   public Table() {  }
   
   public Table(FlowDocument flowDoc) { MyFlowDoc = flowDoc; Id = ++FlowDocument.TableIdCounter; SelectionBrush = flowDoc.SelectionBrush; }


   public Table(int cols, int rows, FlowDocument flowDoc) : this(flowDoc)
   {

      double eqWidth = Math.Truncate(Width / cols);
      double eqHeight = Math.Truncate(Height / rows);

      for (int colno = 0; colno < cols; colno++)
         ColDefs.Add(new ColumnDefinition(eqWidth, GridUnitType.Pixel));

      int cellno = 0;

      for (int rowno = 0; rowno < rows; rowno++)
      {
         RowDefs.Add(new RowDefinition(eqHeight, GridUnitType.Pixel));
         
         for (int colno = 0; colno < cols; colno++)
         {
       
            Paragraph newPar = new(flowDoc) ;

            Cell newCell = new(this)
            {
               ColNo = colno,
               RowNo = rowno,
               BorderThickness = new(1),
               BorderBrush = Brushes.Red,
               CellContent = newPar,
               Padding = new(5)
            };

            //bool skip = GetMergedTestCell2(newPar, newCell, rowno, colno);
            bool skip = false;
         
            if (!skip)
               Cells.Add(newCell);
                        
            
            newPar.IsTableCellBlock = true;
            newPar.OwningTable = this;


            //temporary text for verification
            newPar.Inlines.Add(new EditableRun("c:" + colno));
            newPar.Inlines.Add(new EditableLineBreak());
            newPar.Inlines.Add(new EditableRun("r:" + rowno));
            newPar.TextAlignment = TextAlignment.Center;
            newPar.VerticalAlignment = newCell.CellVerticalAlignment;

            cellno++;
         }
      }

      Debug.WriteLine("total cells : " + Cells.Count);


   }

   internal Cell? GetContainingCell(int charIndex)
   {
      return Cells.LastOrDefault(c => c.CellContent.StartInDoc <= charIndex);
   }

   internal void InsertColumn(int idx)
   {

   }


   static bool GetMergedTestCell(Paragraph newPar, Cell newCell, int rowno, int colno)
   {
      //Add merged cells:
      if (rowno == 0 && colno == 0)
      {
         newCell.ColSpan = 2;
         newCell.RowSpan = 2;
         newCell.CellVerticalAlignment = VerticalAlignment.Center;
      }

      if (rowno == 0 && colno == 2)
      {
         newCell.RowSpan = 2;
         newCell.CellVerticalAlignment = VerticalAlignment.Center;
      }

      if (rowno == 1 && colno == 3)
      {
         newCell.RowSpan = 3;
         newCell.CellVerticalAlignment = VerticalAlignment.Center;
      }

      if (rowno == 2 && colno == 0)
      {
         newCell.ColSpan = 3;
         newCell.CellVerticalAlignment = VerticalAlignment.Center;
      }

      if (rowno == 0 && colno == 4)
      {
         newCell.RowSpan = 4;
         newCell.CellVerticalAlignment = VerticalAlignment.Center;
      }

      return 
         (rowno == 0 && colno == 1) ||
         (rowno == 1 && colno == 0) ||
         (rowno == 1 && colno == 1) ||
         (rowno == 1 && colno == 2) ||
         (rowno == 2 && colno == 3) ||
         (rowno == 3 && colno == 3) ||
         (rowno == 2 && colno == 1) ||
         (rowno == 2 && colno == 2) ||

         (rowno == 1 && colno == 4) ||
         (rowno == 2 && colno == 4) ||
         (rowno == 3 && colno == 4);

   }


   static bool GetMergedTestCell2(Paragraph newPar, Cell newCell, int rowno, int colno)
   {
      //Add merged cells:
      if (rowno == 0 && colno == 0)
      {
         newCell.ColSpan = 2;
         newCell.RowSpan = 2;
         newCell.CellVerticalAlignment = VerticalAlignment.Top;
      }

      if (rowno == 0 && colno == 2)
      {
         newCell.RowSpan = 2;
         newCell.CellVerticalAlignment = VerticalAlignment.Center;
      }

      if (rowno == 1 && colno == 3)
      {
         newCell.RowSpan = 3;
         newCell.CellVerticalAlignment = VerticalAlignment.Center;
         newCell.CellBackground = Brushes.LightSteelBlue;
      }

      if (rowno == 2 && colno == 0)
      {
         newCell.ColSpan = 3;
         newCell.CellVerticalAlignment = VerticalAlignment.Center;
      }

      if (rowno == 1 && colno == 4)
      {
         newCell.RowSpan = 3;
         newCell.CellVerticalAlignment = VerticalAlignment.Center;
      }
      
      if (rowno == 1 && colno == 3)
      {
         newCell.ColSpan = 2;
         newCell.RowSpan = 2;
         newCell.CellVerticalAlignment = VerticalAlignment.Center;
      }

      if (rowno == 3 && colno == 2)
      {
         newCell.ColSpan = 3;
         newCell.CellVerticalAlignment = VerticalAlignment.Center;
      }

      return
         (rowno == 0 && colno == 1) ||
         (rowno == 1 && colno == 0) ||
         (rowno == 1 && colno == 1) ||
         (rowno == 1 && colno == 2) ||
         (rowno == 2 && colno == 3) ||
         (rowno == 2 && colno == 1) ||
         (rowno == 2 && colno == 2) ||
         (rowno == 3 && colno == 3) ||
         (rowno == 3 && colno == 4) ||

         (rowno == 1 && colno == 4) ||
         (rowno == 2 && colno == 4);
         

   }

}


public class Cell(Table owningTable): INotifyPropertyChanged
{
   public event PropertyChangedEventHandler? PropertyChanged;
   private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   public Block CellContent 
   { 
      get; 
      set
      {
         field = value;
         if (value is Paragraph p)
         {
            p.VerticalAlignment = this.CellVerticalAlignment;
            p.IsTableCellBlock = true;
            p.OwningTable = OwningTable;
            p.OwningCell = this;
         }
      }
   
   } = null!;

   internal Table OwningTable => owningTable;
   public Thickness BorderThickness { get; set; } = new (1);
   public ISolidColorBrush BorderBrush { get; set; } = Brushes.Black;
   public ISolidColorBrush CellBackground { get; set; } = null!;
   public Thickness Padding { get; set; } = new(5);
   public int ColNo { get; set; }
   public int RowNo { get; set; }
   public int ColSpan { get; set; } = 1;
   public int RowSpan { get; set; } = 1;

   public bool Selected {  get; set { field = value;  NotifyPropertyChanged(nameof(Selected)); } } = false;

   public IBrush SelectionBrush => OwningTable.SelectionBrush;

   //public double Width { get; set; } = 100;
   public double Height { get; set; } = 60;
   public bool vmerged = false;
   public VerticalAlignment CellVerticalAlignment 
   { 
      get;
      set
      {
         field = value;
         if (CellContent is Paragraph p)
            p.VerticalAlignment = value;
      } 
   }  = VerticalAlignment.Top;
   
}

