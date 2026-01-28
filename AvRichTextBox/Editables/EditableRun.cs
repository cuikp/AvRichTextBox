using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvRichTextBox;

public class EditableRun : Run, IEditable, INotifyPropertyChanged
{
   public new event PropertyChangedEventHandler? PropertyChanged;
   private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   public EditableRun() { Id = ++FlowDocument.InlineIdCounter; }

   public EditableRun(string text)
   {
      this.Text = text;
      //FontFamily = "Meiryo";
      FontSize = 16;
      BaselineAlignment = BaselineAlignment.Baseline;
      Id = ++FlowDocument.InlineIdCounter;
            
   }

   public int Id { get; set; }
   public Inline BaseInline => this;
   public Paragraph? MyParagraph { get; set; }
   public int TextPositionOfInlineInParagraph { get; set; }
   public string InlineText { get => Text!; set => Text = value; }
   public string DisplayInlineText => IsEmpty ? "{>EMPTY<}" : (InlineText.Length == 1 ? Text!.Replace(" ", "{>SPACE<}").Replace("\t", "{>TAB<}") : Text!.Replace("\t", "{>TAB<}"));
   

   public int InlineLength => InlineText.Length;
   public double InlineHeight => FontSize;
   public bool IsEmpty => InlineText.Length == 0;
   public string FontName => FontFamily?.Name == null ? "" : FontFamily?.Name!;

   public bool IsLastInlineOfParagraph { get; set; }
    
   public IEditable Clone() => 

      new EditableRun(this.Text!)
      {
         //Id = this.Id,
         FontStyle = this.FontStyle,
         FontWeight = this.FontWeight,
         TextDecorations = this.TextDecorations,
         FontSize = this.FontSize,
         FontFamily = this.FontFamily,
         Background = this.Background,
         MyParagraph = this.MyParagraph,
         TextPositionOfInlineInParagraph = this.TextPositionOfInlineInParagraph,  //necessary because clone is produced when calculating range inline positions
         IsLastInlineOfParagraph = this.IsLastInlineOfParagraph,
         BaselineAlignment = this.BaselineAlignment,
         Foreground = this.Foreground,
      };
   

   //For DebuggerPanel
   public bool IsStartInline { get; set { field = value; NotifyPropertyChanged(nameof(BackBrush)); NotifyPropertyChanged(nameof(InlineSelectedBorderThickness)); } }
   public bool IsEndInline { get; set { field = value; NotifyPropertyChanged(nameof(BackBrush)); NotifyPropertyChanged(nameof(InlineSelectedBorderThickness)); } }
   public bool IsWithinSelectionInline { get; set { field = value; NotifyPropertyChanged(nameof(BackBrush)); } }
   public Thickness InlineSelectedBorderThickness => (IsStartInline || IsEndInline) ? new Thickness(3) : new Thickness(0.7);

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

   public string InlineToolTip => $"Background: {Background}\nForeground: {Foreground}\nFontFamily: {FontFamily}";


}




