
namespace AvRichTextBox;

public interface IEditable
{   
   internal int MyParagraphId { get; set; }
   internal FlowDocument MyFlowDoc { get; set; }
   internal int Id { get; set; }
   internal bool IsLastInlineOfParagraph { get; set; }
   internal int TextPositionOfInlineInParagraph { get; set; }
   internal bool IsTableCellInline { get; set; } 

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
   public string DisplayInlineText { get; }
#endif

}

