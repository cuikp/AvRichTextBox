using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvRichTextBox;


public class EditableRun : Run, IEditable, INotifyPropertyChanged
{
   public new event PropertyChangedEventHandler? PropertyChanged;
   private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   public EditableRun(string text)
   {
      this.Text = text;
      FontFamily = "Inter";
      FontSize = 16;
   }

   public Inline BaseInline => this;
   public Paragraph? myParagraph { get; set; }
   public int TextPositionOfInlineInParagraph { get; set; }
   public string InlineText { get => Text!; set => Text = value; }
   public string DisplayInlineText { get => Text!.Length == 0 ? "{>EMPTY<}" : (Text.Length == 1 ? Text.Replace(" ", "{>SPACE<}").Replace("\v", "{>LineFeed<}").Replace("\t", "{>TAB<}") : Text!.Replace("\t", "{>TAB<}").Replace("\v", "{>LineFeed<}")); }
   public int InlineLength => InlineText == "\v" ? 1 : InlineText.Length;

   private bool _IsStartInline = false;
   public bool IsStartInline { get => _IsStartInline; set { _IsStartInline = value; NotifyPropertyChanged(nameof(BackBrush)); NotifyPropertyChanged(nameof(InlineSelectedBorderThickness)); } }
   private bool _IsEndInline = false;
   public bool IsEndInline { get => _IsEndInline; set { _IsEndInline = value; NotifyPropertyChanged(nameof(BackBrush)); NotifyPropertyChanged(nameof(InlineSelectedBorderThickness)); } }
   public SolidColorBrush BackBrush =>
       IsStartInline ? new SolidColorBrush(Colors.LawnGreen) : (IsEndInline ? new SolidColorBrush(Colors.Pink) : new SolidColorBrush(Colors.Transparent));

   public bool IsLineBreak => this.Text == "\v";

   public Thickness InlineSelectedBorderThickness => (IsStartInline || IsEndInline) ? new Thickness(3) : new Thickness(0.7);

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
         myParagraph = this.myParagraph
      };
   }

}

