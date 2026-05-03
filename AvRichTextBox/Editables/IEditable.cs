
namespace AvRichTextBox;

public interface IEditable
{
   internal int MyParagraphId { get; set; }
   internal FlowDocument MyFlowDoc { get; set; }
   internal int Id { get; set; }
   internal bool IsLastInlineOfParagraph { get; set; }
   internal int TextPositionOfInlineInParagraph { get; set; }

   internal bool IsEmpty { get; }
   internal bool IsTableCellInline { get; set; }  // set when editable is created
   internal bool IsRun => this is EditableRun;
   internal bool IsUIContainer => this is EditableInlineUIContainer;
   internal bool IsLineBreak => this is EditableLineBreak;
   internal bool IsHyperlink => this is EditableHyperlink;

   public string InlineText { get; set; }
   public int InlineLength { get; }
   public double InlineHeight { get; }

   internal IEditable Clone();
   internal IEditable CloneWithId();

   internal IEditable? PreviousInline { get; set; }
   internal IEditable? NextInline { get; set;}


#if DEBUG
   // FOR DEBUGGER PANEL
   public string InlineToolTip { get; }
   public InlineVisualizationProperties InlineVP { get; set; }
   public string DisplayInlineText { get; }
#endif

}

