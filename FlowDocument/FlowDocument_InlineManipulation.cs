using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace AvRichTextBox;

public partial class FlowDocument
{

   private List<IEditable> GetRangeInlines(TextRange trange)
   {
      Paragraph? startPar = trange.GetStartPar();
      Paragraph? endPar = trange.GetEndPar();
      if (startPar == null | endPar == null) return new List<IEditable>();

      //Create clones of all inlines
      List<IEditable> AllSelectedInlines = Blocks.Where(b => b.IsParagraph).SelectMany(b =>
         ((Paragraph)b).Inlines.Where(iline => b.StartInDoc + iline.TextPositionOfInlineInParagraph + iline.InlineLength > trange.Start &&
             b.StartInDoc + iline.TextPositionOfInlineInParagraph < trange.End)).ToList().ConvertAll(il=>il.Clone());

      //Edge case
      if (AllSelectedInlines.Count == 0)
         AllSelectedInlines = Blocks.Where(b => b.IsParagraph).SelectMany(b =>
            ((Paragraph)b).Inlines.Where(iline => b.StartInDoc + iline.TextPositionOfInlineInParagraph + iline.InlineLength >= trange.Start &&
             b.StartInDoc + iline.TextPositionOfInlineInParagraph < trange.End)).ToList().ConvertAll(il => il.Clone());

      if (AllSelectedInlines.Count == 1)
      {
         int lastInlineSplitIndex = trange.End - endPar!.StartInDoc - AllSelectedInlines[0].TextPositionOfInlineInParagraph;
         int firstInlineSplitIndex = Math.Min(trange.Start - startPar!.StartInDoc - AllSelectedInlines[0].TextPositionOfInlineInParagraph, AllSelectedInlines[0].InlineText.Length);
         AllSelectedInlines[0].InlineText = AllSelectedInlines[0].InlineText.Substring(firstInlineSplitIndex, lastInlineSplitIndex - firstInlineSplitIndex);
      }
      else
      {
         IEditable firstInline = AllSelectedInlines[0];
         IEditable lastInline = AllSelectedInlines[^1];
         int lastInlineSplitIndex = trange.End - endPar!.StartInDoc - lastInline.TextPositionOfInlineInParagraph;
         int firstInlineSplitIndex = Math.Min(trange.Start - startPar!.StartInDoc - firstInline.TextPositionOfInlineInParagraph, firstInline.InlineText.Length);

         firstInline.InlineText = firstInline.InlineText.Substring(firstInlineSplitIndex);
         lastInline.InlineText = lastInline.InlineText.Substring(0, lastInlineSplitIndex);
      }
      return AllSelectedInlines;

   }

   private List<IEditable> CreateNewInlinesForRange(TextRange trange)
   {
      
      Paragraph? startPar = trange.GetStartPar();
      Paragraph? endPar = trange.GetEndPar();
      if (startPar == null | endPar == null) return new List<IEditable>();

      List<IEditable> AllSelectedInlines = Blocks.Where(b => b.IsParagraph).SelectMany(b =>
         ((Paragraph)b).Inlines.Where(iline => b.StartInDoc + iline.TextPositionOfInlineInParagraph + iline.InlineLength > trange.Start &&
             b.StartInDoc + iline.TextPositionOfInlineInParagraph < trange.End)).ToList();
      
      //Edge case
      if (AllSelectedInlines.Count == 0)
         AllSelectedInlines = Blocks.Where(b => b.IsParagraph).SelectMany(b =>
            ((Paragraph)b).Inlines.Where(iline => b.StartInDoc + iline.TextPositionOfInlineInParagraph + iline.InlineLength >= trange.Start &&
             b.StartInDoc + iline.TextPositionOfInlineInParagraph < trange.End)).ToList();

      if (AllSelectedInlines.Count == 0)
         return new List<IEditable>();

      IEditable firstInline = AllSelectedInlines[0];
      IEditable lastInline = AllSelectedInlines[^1];
      IEditable insertLastInline = lastInline.Clone();

      int lastInlineSplitIndex = trange.End - endPar!.StartInDoc - lastInline.TextPositionOfInlineInParagraph;
      bool RangeEndsAtInlineEnd = lastInlineSplitIndex >= lastInline.InlineLength;

      string lastInlineText = lastInline.InlineText;
      int indexOfLastInline = endPar.Inlines.IndexOf(lastInline);

      if (AllSelectedInlines.Count == 1)
      {
         if (!RangeEndsAtInlineEnd)
         {
            insertLastInline.InlineText = lastInlineText.Substring(0, lastInlineSplitIndex);
            lastInline.InlineText = lastInlineText.Substring(lastInlineSplitIndex);
            AllSelectedInlines.RemoveAt(AllSelectedInlines.Count - 1);
            AllSelectedInlines.Add(insertLastInline);

            endPar.Inlines.Insert(indexOfLastInline, insertLastInline);

         }

         IEditable insertFirstInline = insertLastInline.Clone();
         string firstInlineText = insertLastInline.InlineText;
         int firstInlineSplitIndex = Math.Min(trange.Start - startPar!.StartInDoc - firstInline.TextPositionOfInlineInParagraph, firstInlineText.Length);

         bool RangeStartsAtInlineStart = firstInlineSplitIndex <= 0;

         if (!RangeStartsAtInlineStart)
         {
            insertFirstInline.InlineText = firstInlineText.Substring(0, firstInlineSplitIndex);
            insertLastInline.InlineText = firstInlineText.Substring(firstInlineSplitIndex);

            startPar.Inlines.Insert(indexOfLastInline, insertFirstInline);
         }
      }
      else
      {
         //split last run and remove trailing excess run from lithat
         if (!RangeEndsAtInlineEnd)
         {
            lastInline.InlineText = lastInlineText.Substring(0, lastInlineSplitIndex);
            AllSelectedInlines.Add(lastInline);

            insertLastInline.InlineText = lastInlineText.Substring(lastInlineSplitIndex);
            endPar.Inlines.Insert(indexOfLastInline + 1, insertLastInline);

            firstInline = AllSelectedInlines[0];
         }

         IEditable insertFirstInline = firstInline.Clone();
         string firstInlineText = firstInline.InlineText;
         int indexOfFirstInline = startPar!.Inlines.IndexOf(firstInline);

         // split first run and remove initial excess run from list
         int firstInlineSplitIndex = Math.Min(trange.Start - startPar.StartInDoc - firstInline.TextPositionOfInlineInParagraph, firstInlineText.Length);
         bool RangeStartsAtInlineStart = firstInlineSplitIndex <= 0;

         if (!RangeStartsAtInlineStart)
         {
            firstInline.InlineText = firstInlineText.Substring(0, firstInlineSplitIndex);
            insertFirstInline.InlineText = firstInlineText.Substring(firstInlineSplitIndex);
            AllSelectedInlines.Remove(firstInline);
            AllSelectedInlines.Insert(0, insertFirstInline);
            
            startPar.Inlines.Insert(indexOfFirstInline + 1, insertFirstInline);
         }
      }

      startPar.RequestInlinesUpdate = true;
      endPar.RequestInlinesUpdate = true;
      UpdateBlockAndInlineStarts(Blocks.IndexOf(startPar));
 
    
      return AllSelectedInlines;

   }

   internal void RemoveEmptyInlines(List<int> processParIndexes)
   {
      List<IEditable> toDeleteRuns = [];
      for (int idx = 0; idx < processParIndexes.Count; idx ++)
      //foreach (Paragraph p in processPars)
      {
         Paragraph p = (Paragraph)Blocks[processParIndexes[idx]];
         for (int iedno = p.Inlines.Count - 1; iedno >= 0; iedno -= 1)
            if (p.Inlines[iedno].InlineText == "")
               p.Inlines.RemoveAt(iedno);

         if (p.Inlines.Count == 0 && p != Blocks[processParIndexes[0]])
            Blocks.Remove(p);
      }

      UpdateBlockAndInlineStarts(processParIndexes[0]);

   }

   internal List<IEditable> SplitRunAtPos(TextRange tRange, IEditable inlineToSplit, int splitPos)
   {

      ObservableCollection<IEditable> inlines = GetContainingParagraph(tRange.Start).Inlines;
      int runIdx = inlines.IndexOf(inlineToSplit);

      string part2Text = inlineToSplit.InlineText.Substring(splitPos);
      inlineToSplit.InlineText = inlineToSplit.InlineText.Substring(0, splitPos);
      IEditable insertInline = inlineToSplit.Clone();
      insertInline.InlineText = part2Text;
      if (insertInline.InlineText != "")
         inlines.Insert(runIdx + 1, insertInline);

      return new List<IEditable>() { inlineToSplit, insertInline };
   }


}