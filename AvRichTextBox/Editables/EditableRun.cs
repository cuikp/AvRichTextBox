using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvRichTextBox;

public class EditableRun : Run, IEditable, INotifyPropertyChanged
{
   public new event PropertyChangedEventHandler? PropertyChanged;
   private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   public EditableRun() { }

   public EditableRun(string text)
   {
      this.Text = text;
      //FontFamily = "Meiryo";
      FontSize = 16;
      BaselineAlignment = BaselineAlignment.Baseline;
   }

   public Inline BaseInline => this;
   public Paragraph? myParagraph { get; set; }
   public int TextPositionOfInlineInParagraph { get; set; }
   public string InlineText { get => Text!; set => Text = value; }
   public string DisplayInlineText
   {
      get => IsEmpty ? "{>EMPTY<}" : (InlineText.Length == 1 ? Text!.Replace(" ", "{>SPACE<}").Replace("\t", "{>TAB<}") : Text!.Replace("\t", "{>TAB<}"));
   }

   public int InlineLength => InlineText.Length;
   public bool IsEmpty => InlineText.Length == 0;
   public string FontName => FontFamily?.Name == null ? "" : FontFamily?.Name!;

   public bool IsLastInlineOfParagraph { get; set; }
    
   public IEditable Clone()
   {
      return new EditableRun(this.Text!)
      {
         FontStyle = this.FontStyle,
         FontWeight = this.FontWeight,
         TextDecorations = this.TextDecorations,
         FontSize = this.FontSize,
         FontFamily = this.FontFamily,
         Background = this.Background,
         myParagraph = this.myParagraph,
         TextPositionOfInlineInParagraph = this.TextPositionOfInlineInParagraph,  //necessary because clone is produced when calculating range inline positions
         IsLastInlineOfParagraph = this.IsLastInlineOfParagraph,
         BaselineAlignment = this.BaselineAlignment,
         Foreground = this.Foreground,
      };
   }

   //For DebuggerPanel
   private bool _IsStartInline = false;
   public bool IsStartInline { get => _IsStartInline; set { _IsStartInline = value; NotifyPropertyChanged(nameof(BackBrush)); NotifyPropertyChanged(nameof(InlineSelectedBorderThickness)); } }
   private bool _IsEndInline = false;
   public bool IsEndInline { get => _IsEndInline; set { _IsEndInline = value; NotifyPropertyChanged(nameof(BackBrush)); NotifyPropertyChanged(nameof(InlineSelectedBorderThickness)); } }
   private bool _IsWithinSelectionInline = false;
   public bool IsWithinSelectionInline { get => _IsWithinSelectionInline; set { _IsWithinSelectionInline = value; NotifyPropertyChanged(nameof(BackBrush)); } }
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




