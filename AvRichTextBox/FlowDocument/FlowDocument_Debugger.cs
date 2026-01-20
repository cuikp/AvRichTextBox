using System.Collections.ObjectModel;

namespace AvRichTextBox;

public partial class FlowDocument 
{

   internal bool ShowDebugger = false;
   internal ObservableCollection<Paragraph> SelectionParagraphs { get; set; } = []; // for DebuggerPanel

   private void UpdateDebuggerSelectionParagraphs()
   {

#if DEBUG

      //SelectionParagraphs.Clear();
      //SelectionParagraphs.AddRange(Blocks.Where(p => p.StartInDoc + p.BlockLength > Selection.Start && p.StartInDoc <= Selection.End).ToList().ConvertAll(bb => (Paragraph)bb));

      //Visuals for DebuggerPanel
      foreach (Paragraph p in SelectionParagraphs)
      {
         foreach (IEditable ied in p.Inlines)
         {
            IEditable startInline = Selection.GetStartInline();
            IEditable endInline = Selection.GetEndInline();
            int thisRunIndex = p.Inlines.IndexOf(ied);
            ied.IsStartInline = ied == startInline;
            ied.IsEndInline = ied == endInline;
            ied.IsWithinSelectionInline =
            startInline != null && endInline != null && thisRunIndex > p.Inlines.IndexOf(startInline) && thisRunIndex < p.Inlines.IndexOf(endInline);
         }
      }

#endif

   }


}

