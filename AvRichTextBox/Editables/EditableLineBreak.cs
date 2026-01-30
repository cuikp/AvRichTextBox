using Avalonia.Controls.Documents;
using static AvRichTextBox.IEditable;

namespace AvRichTextBox;

public class EditableLineBreak : LineBreak, IEditable
{
   public EditableLineBreak() { Id = ++FlowDocument.InlineIdCounter; }
   
   public int Id { get; set; }
   public Inline BaseInline => this;
   public int MyParagraphId { get; set; }
   public int TextPositionOfInlineInParagraph { get; set; }
   public string InlineText { get; set; } = @"\n"; //make literal to count as 2 characters
   public int InlineLength => 2;  //because LineBreak acts as a double character in TextBlock? - anyway don't use LineBreak, use \v instead
   public double InlineHeight => FontSize;
   
   [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
   public string FontName => "---";

   public bool IsEmpty => false;
   public bool IsLastInlineOfParagraph { get; set; }

   public IEditable Clone() => new EditableLineBreak() { MyParagraphId = this.MyParagraphId }; //, Id = this.Id };

   public IEditable CloneWithId()
   {
      IEditable IdClone = this.Clone();
      IdClone.Id = this.Id;
      return IdClone;
   }

#if DEBUG
   // FOR DEBUGGER PANEL
   public InlineVisualizationProperties InlineVP { get; set; } = new();
   public string InlineToolTip => "";
   public string DisplayInlineText => "{>LINEBREAK<}";
#endif

}

