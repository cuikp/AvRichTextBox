using Avalonia;
using System.Linq;

namespace AvRichTextBox;

public partial class RichTextBox
{

   public void CloseDocument()
   {

      rtbVM.FlowDoc.Blocks.Clear();

      for (int tRangeNo = rtbVM.FlowDoc.TextRanges.Count - 1; tRangeNo >=0; tRangeNo--)
      {
         if (!rtbVM.FlowDoc.TextRanges[tRangeNo].Equals(rtbVM.FlowDoc.Selection))
         rtbVM.FlowDoc.TextRanges[tRangeNo].Dispose();
      }
         

      rtbVM.FlowDoc.Undos.Clear();

      rtbVM.RTBScrollOffset = new Vector(0, 0);
      this.UpdateLayout();
      Paragraph newpar = new();
      EditableRun newerun = new("");
      newpar.Inlines.Add(newerun);
      rtbVM.FlowDoc.Blocks.Add(newpar);

      InitializeDocument();


   }

   private void InitializeDocument()
   {
      this.Focus();

#if DEBUG
      RunDebugger.DataContext = rtbVM.FlowDoc;
#else
      RunDebugger.DataContext = null;
#endif


      InitializeParagraphs();
      
      this.UpdateLayout();
      this.InvalidateVisual();

      rtbVM.FlowDoc.Selection.Start = 0;
      rtbVM.FlowDoc.Selection.CollapseToStart();

   }

}


