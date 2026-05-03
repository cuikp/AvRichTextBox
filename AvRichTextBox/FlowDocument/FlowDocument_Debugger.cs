
namespace AvRichTextBox;

public partial class FlowDocument 
{

#if DEBUG

   internal bool ShowDebugger = false;

   private void UpdateDebuggerSelectionParagraphs()
   {

      //Visuals for DebuggerPanel
      foreach (Paragraph p in SelectionParagraphs.OfType<Paragraph>())
      {
         foreach (IEditable ied in p.Inlines)
         {
            //Debug.WriteLine("inlineDisplayText = " + ied.InlineText + " --- " + ied.DisplayInlineText + " ---");

            int thisRunIndex = p.Inlines.IndexOf(ied);
            ied.InlineVP.IsTableCellInline = p.IsTableCellBlock;
            ied.InlineVP.IsStartInline = ied == Selection.StartInline;
            ied.InlineVP.IsEndInline = ied == Selection.EndInline;
            ied.InlineVP.IsWithinSelectionInline =
               Selection.StartInline != null &&
               Selection.EndInline != null &&
               thisRunIndex > p.Inlines.IndexOf(Selection.StartInline) && thisRunIndex < p.Inlines.IndexOf(Selection.EndInline);
         }
      }


   }

#endif

}

