using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AvRichTextBox;

public class Block : INotifyPropertyChanged
{
   public event PropertyChangedEventHandler? PropertyChanged;
   public void NotifyPropertyChanged([CallerMemberName] String propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   public Thickness Margin { get; set { field = value; NotifyPropertyChanged(nameof(Margin)); } }

   //public string TextOLD
   //{
   //   get
   //   {
                  
   //      string returnText = "";

   //      switch (this.GetType())
   //      {
   //         case Type t when t == typeof(Paragraph):
   //            returnText = string.Join("", ((Paragraph)this).Inlines.ToList().ConvertAll(ied => ied.InlineText));
   //            break;
   //            //case Type t when t == typeof(Table):
   //            //   returnText = "$";
   //            //   break;
   //      }
   //      return returnText;
   //   }
   //}

   public string Text
   {
      get
      {
         switch (this)
         {
            case Paragraph p:
               //count length first to size stringbuilder
               int len = 0;
               foreach (var i in p.Inlines)
                  len += i.InlineText?.Length ?? 0;

               var sb = new StringBuilder(len);
               foreach (var i in p.Inlines)
                  sb.Append(i.InlineText);
               
               return sb.ToString(); 
               
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

               return len;

            default:

               return 0;
         }
      } 
   }

   public bool IsParagraph => this.GetType() == typeof(Paragraph);
   //public bool IsTable => this.GetType() == typeof(Table);

   internal int SelectionLength => SelectionEndInBlock - SelectionStartInBlock;
   public int BlockLength => this.IsParagraph ? ((Paragraph)this).Inlines.ToList().Sum(il => il.InlineLength) + 1 : 1;  //Add one for paragraph itself

   internal int StartInDoc { get; set { if (field != value) { field = value; NotifyPropertyChanged(nameof(StartInDoc)); } } }
   internal int EndInDoc => StartInDoc + BlockLength;

   public int SelectionStartInBlock { get; set { if (field != value) { field = value; NotifyPropertyChanged(nameof(SelectionStartInBlock)); } } }
   public int SelectionEndInBlock { get; set {  if (field != value) { field = value; NotifyPropertyChanged(nameof(SelectionEndInBlock)); }  }  } 

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
