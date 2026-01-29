using System.Collections.ObjectModel;

namespace AvRichTextBox;

public partial class FlowDocument 
{

   internal bool ShowDebugger = false;
   internal ObservableCollection<Paragraph> SelectionParagraphs { get; set; } = []; // for DebuggerPanel

   private void UpdateDebuggerSelectionParagraphs()
   {

#if DEBUG

      //Visuals for DebuggerPanel
      foreach (Paragraph p in SelectionParagraphs)
      {
         foreach (IEditable ied in p.Inlines)
         {
            IEditable? startInline = Selection.GetStartInline();
            IEditable? endInline = Selection.GetEndInline();
            int thisRunIndex = p.Inlines.IndexOf(ied);
            ied.InlineVP.IsStartInline = ied == startInline;
            ied.InlineVP.IsEndInline = ied == endInline;
            ied.InlineVP.IsWithinSelectionInline =
               startInline != null &&
               endInline != null &&
               thisRunIndex > p.Inlines.IndexOf(startInline) && thisRunIndex < p.Inlines.IndexOf(endInline);
         }
      }

#endif

   }


}

