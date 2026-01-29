using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvRichTextBox;

public interface IEditable
{   
   Inline BaseInline { get; }

   internal int MyParagraphId { get; set; }
   internal int Id { get; set; }
   internal bool IsLastInlineOfParagraph { get; set; }
   internal int TextPositionOfInlineInParagraph { get; set; }
   public string InlineText { get; set; }
   public bool IsEmpty { get; }
   public int InlineLength { get; }
   public double InlineHeight { get; }
   public IEditable Clone();
   public IEditable CloneWithId();
   public bool IsRun => this.GetType() == typeof(EditableRun);
   public bool IsUIContainer => this.GetType() == typeof(EditableInlineUIContainer);
   public bool IsLineBreak => this.GetType() == typeof(EditableLineBreak);


#if DEBUG
   // FOR DEBUGGER PANEL
   public InlineVisualizationProperties InlineVP { get; set; }
#endif

}

public class InlineVisualizationProperties : INotifyPropertyChanged
{
   //CLASS FOR DEBUGGER PANEL
   public event PropertyChangedEventHandler? PropertyChanged;
   private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

   public bool IsStartInline { get; set { field = value; NotifyPropertyChanged(nameof(BackBrush)); NotifyPropertyChanged(nameof(InlineSelectedBorderThickness)); } }
   public bool IsEndInline { get; set { field = value; NotifyPropertyChanged(nameof(BackBrush)); NotifyPropertyChanged(nameof(InlineSelectedBorderThickness)); } }
   public bool IsWithinSelectionInline { get; set { field = value; NotifyPropertyChanged(nameof(BackBrush)); } }
   public Thickness InlineSelectedBorderThickness => (IsStartInline || IsEndInline) ? new Thickness(3) : new Thickness(1);
   readonly SolidColorBrush startInlineBrush = new(Colors.LawnGreen);
   readonly SolidColorBrush endInlineBrush = new(Colors.Pink);
   readonly SolidColorBrush withinSelectionInlineBrush = new(Colors.LightGray);
   readonly SolidColorBrush transparentBrush = new(Colors.Transparent);

   public SolidColorBrush BackBrush
   {
      get
      {
         if (IsStartInline) return startInlineBrush;
         else if (IsEndInline) return endInlineBrush;
         else if (IsWithinSelectionInline) return withinSelectionInlineBrush;
         else return transparentBrush;
      }
   }

}
