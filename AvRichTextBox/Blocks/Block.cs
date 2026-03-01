using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AvRichTextBox;

public class Block : INotifyPropertyChanged
{
   public event PropertyChangedEventHandler? PropertyChanged;
   public void NotifyPropertyChanged([CallerMemberName] String propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   internal int Id = 0;

   internal bool IsTableCellBlock = false;
   internal Table OwningTable = null!;
   internal Cell OwningCell = null!;

   internal FlowDocument MyFlowDoc
   {
      get;
      set
      {
         field = value;
         if (this is Paragraph p)
         {
            foreach (IEditable ied in p.Inlines)
               ied.MyFlowDoc = value;
         }
      }
   } = null!;

   public Thickness Margin { get; set { field = value; NotifyPropertyChanged(nameof(Margin)); } }

   public string Text
   {
      get
      {
         switch (this)
         {
            case Paragraph p:

               var sb = new StringBuilder();
               foreach (var i in p.Inlines)
                  sb.Append(i.InlineText);

               if (p.IsTableCellBlock)
                  sb.Append((char)7);

               return sb.ToString();

            case Table t:

               var sbTable = new StringBuilder();
               foreach (Cell c in t.Cells)
                  sbTable.Append(c.CellContent.Text);
               return sbTable.ToString();

            default:
               return "";
         }
      }
   }

   public int TextLength 
   { 
      get
      {
         switch (this)
         {
            case Paragraph p:
               int len = 0;
               foreach (var i in p.Inlines)
                  len += i.InlineText?.Length ?? 0;

               if (p.IsTableCellBlock)
                  len += 1;

               return len;

            case Table t:

               int lenTable = 0;
               foreach (Cell c in t.Cells)
                  lenTable += c.CellContent.Text.Length;
               return lenTable;

            default:

               return 0;
         }
      } 
   }

   internal int SelectionLength => SelectionEndInBlock - SelectionStartInBlock;
   
   public int BlockLength  
   {
      get
      {
         int returnLength = 0;
         switch (this)
         {
            case Paragraph p:
               returnLength = p.Inlines.ToList().Sum(il => il.InlineLength) + 1;
               
               if (p.IsTableCellBlock)
                  returnLength += 1;  //char7

               break;

            case Table t:

               foreach (Cell c in t.Cells)
               {
                  if (c.CellContent is Paragraph cellPar)
                     returnLength += cellPar.BlockLength;
               }
               returnLength += 0; // need table final char? 
               break;
         }

         return returnLength;
      }
      
   }

   internal int StartInDoc { get; set { if (field != value) { field = value; NotifyPropertyChanged(nameof(StartInDoc)); } } }
   internal int EndInDoc => StartInDoc + BlockLength;

   //Bound to SelectableTextBlock SelectionStart/SelectionEnd (visual selection):
   public int SelectionStartInBlock 
   { 
      get; 
      set 
      { 
         if (field != value) 
         { 
            field = value; 
            NotifyPropertyChanged(nameof(SelectionStartInBlock)); 
            if (this.IsTableCellBlock)
               this.OwningCell.Selected = SelectionStartInBlock == 0 && SelectionEndInBlock == BlockLength - 1;  // later add chr(7) for cells
            
         } 
      }
   }

   public int SelectionEndInBlock 
   { 
      get; 
      set 
      {  
         if (field != value) 
         { 
            field = value; 
            NotifyPropertyChanged(nameof(SelectionEndInBlock));
            if (this.IsTableCellBlock)
               this.OwningCell.Selected = SelectionStartInBlock == 0 && SelectionEndInBlock == BlockLength - 1;  // later add chr(7) for cells
         }  
      }  
   }
   

   public static bool IsFocusable => false;

   internal void ClearSelection()
   {
      this.SelectionStartInBlock = 0;
      this.SelectionEndInBlock = 0;
      if (this is Paragraph p)
      {
         foreach (EditableInlineUIContainer iuc in p.Inlines.OfType<EditableInlineUIContainer>())
            iuc.IsSelected = false;
      }
      
   }

   internal void CollapseToStart() { if (SelectionStartInBlock != SelectionEndInBlock) SelectionEndInBlock = SelectionStartInBlock; }
   internal void CollapseToEnd() { if (SelectionStartInBlock != SelectionEndInBlock) SelectionStartInBlock = SelectionEndInBlock; }



}
