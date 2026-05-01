namespace AvRichTextBox;

public interface IUndo
{
   public void PerformUndo();
   public int UndoEditOffset { get; }
   public bool UpdateTextRanges { get; }
}

internal class EditablePropertyAssociation
{
   internal int InlineId { get; set; }
   internal int BlockId { get; set; }
   internal object PropertyValue { get; set; }
   //internal FlowDocument.FormatRunAction? FormatRuns { get; set; }  
   internal FlowDocument.FormatRunsAction? FormatRuns { get; set; }  

   internal EditablePropertyAssociation(int blockId, int inlineId, FlowDocument.FormatRunsAction formatRunsAction, object propertyValue)
   {
      BlockId = blockId;
      InlineId = inlineId;
      FormatRuns = formatRunsAction;
      PropertyValue = propertyValue;
   }
}