using Avalonia;
using System.Linq;

namespace AvRichTextBox;

public partial class RichTextBox
{

   public void CloseDocument()
   {

      FlowDoc.Blocks.Clear();

      for (int tRangeNo = FlowDoc.TextRanges.Count - 1; tRangeNo >=0; tRangeNo--)
      {
         if (!FlowDoc.TextRanges[tRangeNo].Equals(FlowDoc.Selection))
         FlowDoc.TextRanges[tRangeNo].Dispose();
      }
         

      FlowDoc.Undos.Clear();

      rtbVM.RTBScrollOffset = new Vector(0, 0);
      this.UpdateLayout();
      Paragraph newpar = new();
      EditableRun newerun = new("");
      newpar.Inlines.Add(newerun);
      FlowDoc.Blocks.Add(newpar);

      InitializeDocument();


   }

   private void InitializeDocument()
   {
      this.Focus();

#if DEBUG
      RunDebugger.DataContext = FlowDoc;
#else
      RunDebugger.DataContext = null;
#endif


      InitializeParagraphs();
      
      this.UpdateLayout();
      this.InvalidateVisual();

      FlowDoc.Selection.Start = 0;
      FlowDoc.Selection.CollapseToStart();

   }

}


