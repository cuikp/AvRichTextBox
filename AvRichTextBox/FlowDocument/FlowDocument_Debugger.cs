
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

            IEditable? startInline = Selection.GetStartInline();
            IEditable? endInline = Selection.GetEndInline();
            int thisRunIndex = p.Inlines.IndexOf(ied);
            ied.InlineVP.IsTableCellInline = p.IsTableCellBlock;
            ied.InlineVP.IsStartInline = ied == startInline;
            ied.InlineVP.IsEndInline = ied == endInline;
            ied.InlineVP.IsWithinSelectionInline =
               startInline != null &&
               endInline != null &&
               thisRunIndex > p.Inlines.IndexOf(startInline) && thisRunIndex < p.Inlines.IndexOf(endInline);
         }
      }


   }

#endif

}

