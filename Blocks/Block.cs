using Avalonia;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace AvRichTextBox;

public class Block : INotifyPropertyChanged
{
   public event PropertyChangedEventHandler? PropertyChanged;
   internal void NotifyPropertyChanged([CallerMemberName] String propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   public Thickness Margin => new Thickness(0, 10, 0, 0);

   public string Text
   {
      get
      {
         string returnText = "";

         switch (this.GetType())
         {
            case Type t when t == typeof(Paragraph):
               returnText = string.Join("", ((Paragraph)this).Inlines.ToList().ConvertAll(ied => ied.InlineText));
               break;
               //case Type t when t == typeof(Table):
               //   returnText = "$";
               //   break;
         }
         return returnText;
      }
   }

   public bool IsParagraph => this.GetType() == typeof(Paragraph);
   //public bool IsTable => this.GetType() == typeof(Table);

   internal int SelectionLength => SelectionEndInBlock - SelectionStartInBlock;
   public int BlockLength => this.IsParagraph ? ((Paragraph)this).Inlines.ToList().Sum(il => il.InlineLength) + 1 : 1;  //Add one for paragraph itself

   private int _StartInDoc = 0;
   internal int StartInDoc
   {
      get => _StartInDoc;
      set { if (_StartInDoc != value) { _StartInDoc = value; NotifyPropertyChanged(nameof(StartInDoc)); } }
   }

   internal int EndInDoc => StartInDoc + BlockLength;

   private int _SelectionStartInBlock;
   internal int SelectionStartInBlock
   {
      get => _SelectionStartInBlock;
      set { if (_SelectionStartInBlock != value) { _SelectionStartInBlock = value; NotifyPropertyChanged(nameof(SelectionStartInBlock)); } }
   }

   private int _SelectionEndInBlock;
   internal int SelectionEndInBlock
   {
      get => _SelectionEndInBlock;
      set { if (_SelectionEndInBlock != value) { _SelectionEndInBlock = value; NotifyPropertyChanged(nameof(SelectionEndInBlock)); } }
   }

   public bool IsFocusable => false;

   internal void ClearSelection() { this.SelectionStartInBlock = 0; this.SelectionEndInBlock = 0; }
   internal void CollapseToStart() { if (SelectionStartInBlock != SelectionEndInBlock) SelectionEndInBlock = SelectionStartInBlock; }
   internal void CollapseToEnd() { if (SelectionStartInBlock != SelectionEndInBlock) SelectionStartInBlock = SelectionEndInBlock; }



}
