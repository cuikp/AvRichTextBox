using Avalonia.Controls.Documents;

namespace AvRichTextBox;

public class EditableLineBreak : LineBreak, IEditable
{
   public EditableLineBreak() { Id = ++FlowDocument.InlineIdCounter; }
   
   public FlowDocument MyFlowDoc { get; set; } = null!;

   public int Id { get; set; }
   public int MyParagraphId { get; set; }
   
   public int TextPositionOfInlineInParagraph { get; set; }
   public bool IsTableCellInline { get; set; } = false;

   public string InlineText { get; set; } = @"\n"; //make literal to count as 2 characters
   public int InlineLength => 2;  //because LineBreak acts as a double character in TextBlock? - anyway don't use LineBreak, use \n instead
   public double InlineHeight => FontSize;

   internal IEditable PreviousInline { get; set; } = null!;
   internal IEditable NextInline { get; set; } = null!;

   [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
   public string FontName => "---";

   public bool IsEmpty => false;
   public bool IsLastInlineOfParagraph { get; set; }

   public IEditable Clone() => new EditableLineBreak() { MyParagraphId = this.MyParagraphId, MyFlowDoc = this.MyFlowDoc, }; 

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

   IEditable IEditable.PreviousInline { get => PreviousInline; set => PreviousInline = value; }
   IEditable IEditable.NextInline { get => NextInline; set => NextInline = value; }

#endif

}

