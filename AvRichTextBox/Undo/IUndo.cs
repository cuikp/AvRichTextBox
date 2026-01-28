namespace AvRichTextBox;

public interface IUndo
{
   public void PerformUndo();
   public int UndoEditOffset { get; }
   public bool UpdateTextRanges { get; }
}

internal class IEditablePropertyAssociation
{
   internal int InlineId { get; set; }
   internal int BlockId { get; set; }
   internal object PropertyValue { get; set; }
   internal FlowDocument.FormatRunAction? FormatRun { get; set; }  

   internal IEditablePropertyAssociation(int blockId, int inlineId, FlowDocument.FormatRunAction formatRunAction, object propertyValue)
   {
      BlockId = blockId;
      InlineId = inlineId;
      FormatRun = formatRunAction;
      PropertyValue = propertyValue;
   }
}