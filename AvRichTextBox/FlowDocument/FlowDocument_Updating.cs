using DynamicData;
using System.Reactive.Linq;

namespace AvRichTextBox;

public partial class FlowDocument
{

   internal void UpdateBlockAndInlineStarts(int fromBlockIndex)
   {
      if (fromBlockIndex >= Blocks.Count) return;

      int blockSum = fromBlockIndex == 0 ? 0 : Blocks[fromBlockIndex - 1].StartInDoc + Blocks[fromBlockIndex - 1].BlockLength;
      for (int blockIndex = fromBlockIndex; blockIndex < Blocks.Count; blockIndex++)
      {
         Blocks[blockIndex].StartInDoc = blockSum;

         switch (Blocks[blockIndex])
         {
            case Paragraph thisPar:
               thisPar.UpdateEditableRunPositions();
               break;

            case Table thisTable:
               int innerSum = 0;

               foreach (Cell c in thisTable.Cells)
               {
                  if (c.CellContent is Paragraph cellPar)
                  {
                     cellPar.StartInDoc = blockSum + innerSum;
                     cellPar.UpdateEditableRunPositions();
                     innerSum += cellPar.BlockLength;
                  }
               }
               break;
         }

         blockSum += (Blocks[blockIndex].BlockLength);

      }
   }

   internal void UpdateBlockAndInlineStarts(Paragraph thisPar)
   {
      int fromBlockIndex = -1;

      if (thisPar.IsTableCellBlock)
         fromBlockIndex = Blocks.IndexOf(thisPar.OwningTable);
      else
         fromBlockIndex = Blocks.IndexOf(thisPar);


      if (fromBlockIndex > -1)
         UpdateBlockAndInlineStarts(fromBlockIndex);

   }

   internal void UpdateSelectedParagraphs()
   {
      SelectionParagraphs.Clear();
      SelectionParagraphs.AddRange(AllParagraphs.Where(p => p.StartInDoc + p.BlockLength > Selection.Start && p.StartInDoc <= Selection.End));


#if DEBUG
      if (ShowDebugger)
         UpdateDebuggerSelectionParagraphs();
#endif

   }

   internal void UpdateTextRanges(int editCharIndexStart, int offset)
   {
      List<TextRange> toRemoveRanges = [];
      
      int editCharIndexEnd = offset == 1 ? editCharIndexStart : editCharIndexStart - offset;

      foreach (TextRange trange in TextRanges)
      {
         if (trange.Equals(this.Selection)) continue;  //Don't update the selection range

         if (trange.Start >= editCharIndexStart && trange.End <= editCharIndexEnd)
            { toRemoveRanges.Add(trange); continue; }

         if (trange.Start >= editCharIndexStart)
         {
            if (trange.Start >= editCharIndexEnd)
               trange.Start += offset;
            else
               trange.Start = editCharIndexStart;
         }
            
         if (trange.End >= editCharIndexStart)
         {
            if (trange.End >= editCharIndexEnd)
               trange.End += offset;
            else
               trange.End = editCharIndexStart;
         }

         if (trange.Start > trange.End)
            trange.End = trange.Start;
      }

      for (int trangeNo = toRemoveRanges.Count - 1; trangeNo >=0; trangeNo--)
      {
         if (!toRemoveRanges[trangeNo].Equals(Selection))
            toRemoveRanges[trangeNo].Dispose();
      }
         

   }


}

