using Avalonia.Controls.Documents;

namespace AvRichTextBox;

public class EditableCellBreak : IEditable
{
   public EditableCellBreak() { Id = ++FlowDocument.InlineIdCounter; }
   
   public FlowDocument MyFlowDoc { get; set; } = null!;

   public int Id { get; set; }
   public int MyParagraphId { get; set; }
   
   public int TextPositionOfInlineInParagraph { get; set; }
   public bool IsTableCellInline { get; set; } = true;

   internal string InlineText { get; private set; } = @"\a"; // (char)7
   string IEditable.InlineText { get => InlineText; set { InlineText = value; } }

   public int InlineLength => 1;  
   public double InlineHeight => 16; // arbitrary 

   public IEditable? PreviousInline { get; set; }
   public IEditable? NextInline { get; set; }

   public bool IsEmpty => true;
   public bool IsLastInlineOfParagraph { get; set; }

   public IEditable Clone() => new EditableCellBreak() { MyParagraphId = this.MyParagraphId, MyFlowDoc = this.MyFlowDoc, }; 

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
   public string DisplayInlineText => "{>CELLBREAK<}";

#endif

}

