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
   public string FontName => "---";
   public int InlineLength => 1;
   public bool IsEmpty => false;
   public bool IsLastInlineOfParagraph { get; set; }
   //public double InlineHeight => (this.Child != null && this.Child.GetType() == typeof(Image) ? : this.Child.Bounds.Height;
   public double InlineHeight => Child == null ? 0 : this.Child.Bounds.Height;
   

   public int ImageNo;

   public IEditable Clone() { return new EditableInlineUIContainer(this.Child){ myParagraph = this.myParagraph }; }

   //for DebuggerPanel 
   private bool _IsStartInline = false;
   public bool IsStartInline { get => _IsStartInline; set { _IsStartInline = value; NotifyPropertyChanged(nameof(BackBrush)); NotifyPropertyChanged(nameof(InlineSelectedBorderThickness)); } }
   private bool _IsEndInline = false;
   public bool IsEndInline { get => _IsEndInline; set { _IsEndInline = value; NotifyPropertyChanged(nameof(BackBrush)); } }
   private bool _IsWithinSelectionInline = false;
   public bool IsWithinSelectionInline { get => _IsWithinSelectionInline; set { _IsWithinSelectionInline = value; NotifyPropertyChanged(nameof(BackBrush)); } }
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

   private bool _IsSelected = false;
   public bool IsSelected { get => _IsSelected; set { _IsSelected = value; this.Child.Opacity = value ? 0.2 : 1; } }

}


