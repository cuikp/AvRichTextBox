using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace AvRichTextBox;

public class EditableRun : Run, IEditable
{ 
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
   public int MyParagraphId { get; set; }
   public int TextPositionOfInlineInParagraph { get; set; }
   public string InlineText { get => Text!; set => Text = value; }

   public int InlineLength => InlineText.Length;
   public double InlineHeight => FontSize;
   public bool IsEmpty => InlineText.Length == 0;
   public string FontName => FontFamily?.Name == null ? "" : FontFamily?.Name!;

   public bool IsLastInlineOfParagraph { get; set; }
    
   public IEditable Clone() => 

      new EditableRun(this.Text!)
      {
         FontStyle = this.FontStyle,
         FontWeight = this.FontWeight,
         TextDecorations = this.TextDecorations,
         FontSize = this.FontSize,
         FontFamily = this.FontFamily,
         Background = this.Background,
         MyParagraphId = this.MyParagraphId,
         TextPositionOfInlineInParagraph = this.TextPositionOfInlineInParagraph,  //necessary because clone is produced when calculating range inline positions
         IsLastInlineOfParagraph = this.IsLastInlineOfParagraph,
         BaselineAlignment = this.BaselineAlignment,
         Foreground = this.Foreground,
      };
   
   public IEditable CloneWithId()
   {
      IEditable IdClone = this.Clone();
      IdClone.Id = this.Id;
      return IdClone;
   }


#if DEBUG
   // FOR DEBUGGER PANEL
   public InlineVisualizationProperties InlineVP { get; set; } = new();
   public string InlineToolTip => $"Background: {Background}\nForeground: {Foreground}\nFontFamily: {FontFamily}";
   public string DisplayInlineText => IsEmpty ? "{>EMPTY<}" : (InlineText.Length == 1 ? Text!.Replace(" ", "{>SPACE<}").Replace("\t", "{>TAB<}") : Text!.Replace("\t", "{>TAB<}"));
#endif



}




