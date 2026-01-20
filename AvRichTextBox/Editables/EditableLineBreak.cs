using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvRichTextBox;

public class EditableLineBreak : LineBreak, IEditable, INotifyPropertyChanged
{
   public new event PropertyChangedEventHandler? PropertyChanged;
   private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   public EditableLineBreak() { }

   public Inline BaseInline => this;
   public Paragraph? MyParagraph { get; set; }
   public int TextPositionOfInlineInParagraph { get; set; }
   public string InlineText { get; set; } = @"\n"; //make literal to count as 2 characters
   public string DisplayInlineText => "{>LINEBREAK<}";
   public int InlineLength => 2;  //because LineBreak acts as a double character in TextBlock? - anyway don't use LineBreak, use \v instead
   public double InlineHeight => FontSize;
   public string FontName => "---";
   public bool IsEmpty => false;
   public bool IsLastInlineOfParagraph { get; set; }
 
   public IEditable Clone() => new EditableLineBreak() { MyParagraph = this.MyParagraph };


   //for DebuggerPanel 
   public bool IsStartInline { get; set { field = value; NotifyPropertyChanged(nameof(BackBrush)); NotifyPropertyChanged(nameof(InlineSelectedBorderThickness)); } }
   public bool IsEndInline { get; set { field = value; NotifyPropertyChanged(nameof(BackBrush)); NotifyPropertyChanged(nameof(InlineSelectedBorderThickness)); } }
   public bool IsWithinSelectionInline { get; set { field = value; NotifyPropertyChanged(nameof(BackBrush)); } }
   public Thickness InlineSelectedBorderThickness => (IsStartInline || IsEndInline) ? new Thickness(3) : new Thickness(1);

   public SolidColorBrush BackBrush
   {
      get
      {
         if (IsStartInline) return new SolidColorBrush(Colors.LawnGreen);
         else if (IsEndInline) return new SolidColorBrush(Colors.Pink);
         else if (IsWithinSelectionInline) return new SolidColorBrush(Colors.LightGray);
         else return new SolidColorBrush(Colors.Transparent);
      }
   }

   public string InlineToolTip => "";
   

}

