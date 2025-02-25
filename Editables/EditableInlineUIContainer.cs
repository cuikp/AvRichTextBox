using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvRichTextBox;


public class EditableInlineUIContainer : InlineUIContainer, IEditable, INotifyPropertyChanged
{
   public new event PropertyChangedEventHandler? PropertyChanged;
   private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   public EditableInlineUIContainer(Control c) { Child = c; }

   public Inline BaseInline => this;
   public Paragraph? myParagraph { get; set; }
   public int TextPositionOfInlineInParagraph { get; set; }
   public string InlineText { get; set; } = "@";
   public string DisplayInlineText { get => "<UICONTAINER> => " + (this.Child != null && this.Child.GetType() == typeof(Image) ? "Image" : "NoChild"); }
   public int InlineLength => 1;

   private bool _IsStartInline = false;
   public bool IsStartInline { get => _IsStartInline; set { _IsStartInline = value; NotifyPropertyChanged(nameof(BackBrush)); NotifyPropertyChanged(nameof(InlineSelectedBorderThickness)); } }
   private bool _IsEndInline = false;
   public bool IsEndInline { get => _IsEndInline; set { _IsEndInline = value; NotifyPropertyChanged(nameof(BackBrush)); } }
   public SolidColorBrush BackBrush =>
       IsStartInline ? new SolidColorBrush(Colors.LawnGreen) : (IsEndInline ? new SolidColorBrush(Colors.Pink) : new SolidColorBrush(Colors.Transparent));
   public Thickness InlineSelectedBorderThickness => (IsStartInline || IsEndInline) ? new Thickness(3) : new Thickness(1);
      

   public bool IsLastInlineOfParagraph { get; set;}

   public int ImageNo;

   public IEditable Clone()
   {
      return new EditableInlineUIContainer(this.Child){ myParagraph = this.myParagraph };

   }

}


