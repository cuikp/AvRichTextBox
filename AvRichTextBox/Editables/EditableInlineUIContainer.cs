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
   public Paragraph? MyParagraph { get; set; }
   public int TextPositionOfInlineInParagraph { get; set; }
   public string InlineText { get; set; } = "@";
   public string DisplayInlineText { get => "<UICONTAINER> => " + (this.Child != null && this.Child.GetType() == typeof(Image) ? "Image" : "NoChild"); }
   public string FontName => "---";
   public int InlineLength => 1;
   public bool IsEmpty => false;
   public bool IsLastInlineOfParagraph { get; set; }
   //public double InlineHeight => (this.Child != null && this.Child.GetType() == typeof(Image) ? : this.Child.Bounds.Height;
   public double InlineHeight => Child == null ? 0 : this.Child.Bounds.Height;
   

   public int ImageNo;

   public IEditable Clone() { return new EditableInlineUIContainer(this.Child){ MyParagraph = this.MyParagraph }; }

   //for DebuggerPanel 
   public bool IsStartInline { get; set { field= value; NotifyPropertyChanged(nameof(BackBrush)); NotifyPropertyChanged(nameof(InlineSelectedBorderThickness)); } } = false;
   public bool IsEndInline { get; set { field = value; NotifyPropertyChanged(nameof(BackBrush)); } } = false;
   public bool IsWithinSelectionInline { get; set { field = value; NotifyPropertyChanged(nameof(BackBrush)); } } = false;
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

   public bool IsSelected { get; set { field = value; this.Child.Opacity = value ? 0.2 : 1; } } = false;

}


