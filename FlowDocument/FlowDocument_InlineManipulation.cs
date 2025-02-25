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
      if (startPar == null | endPar == null) return [];

      //Create clones of all inlines
      List<IEditable> AllSelectedInlines = Blocks.Where(b => b.IsParagraph).SelectMany( b =>
            ((Paragraph)b).Inlines.Where(iline => 
            {
               double absInlineStart = b.StartInDoc + iline.TextPositionOfInlineInParagraph;
               double absInlineEnd = b.StartInDoc + iline.TextPositionOfInlineInParagraph + iline.InlineLength;
               iline.IsLastInlineOfParagraph = iline == ((Paragraph)b).Inlines[^1];
               return absInlineEnd > trange.Start && absInlineStart < trange.End;
            })
      ).ToList().ConvertAll(il => 
      {
         IEditable clonedInline = il.Clone();
         if (il.IsLastInlineOfParagraph)  //replace paragraph ends with \r char
            clonedInline.InlineText += "\r";
         return clonedInline; 
      });

      //Edge case
      if (AllSelectedInlines.Count == 0)
         AllSelectedInlines = Blocks.Where(b => b.IsParagraph).SelectMany(b =>
            ((Paragraph)b).Inlines.Where(iline => b.StartInDoc + iline.TextPositionOfInlineInParagraph + iline.InlineLength >= trange.Start &&
             b.StartInDoc + iline.TextPositionOfInlineInParagraph < trange.End)).ToList().ConvertAll(il => il.Clone());

      IEditable firstInline = AllSelectedInlines[0];
      int firstInlineSplitIndex = Math.Min(trange.Start - startPar!.StartInDoc - firstInline.TextPositionOfInlineInParagraph, firstInline.InlineText.Length);

      if (AllSelectedInlines.Count == 1)
      {
         int lastInlineSplitIndex = trange.End - endPar!.StartInDoc - firstInline.TextPositionOfInlineInParagraph;
         firstInline.InlineText = firstInline.InlineText[firstInlineSplitIndex..lastInlineSplitIndex];
      }
      else
      {
         IEditable lastInline = AllSelectedInlines[^1];
         int lastInlineSplitIndex = trange.End - endPar!.StartInDoc - lastInline.TextPositionOfInlineInParagraph;
         firstInline.InlineText = firstInline.InlineText[firstInlineSplitIndex ..];
         lastInline.InlineText = lastInline.InlineText[..lastInlineSplitIndex];
      }
      return AllSelectedInlines;

   }

   

   private List<IEditable> CreateNewInlinesForRange(TextRange trange)
   {
      
      Paragraph? startPar = trange.GetStartPar();
      Paragraph? endPar = trange.GetEndPar();
      if (startPar == null | endPar == null) return [];

      List<IEditable> AllSelectedInlines = Blocks.Where(b => b.IsParagraph).SelectMany(b =>
         ((Paragraph)b).Inlines.Where(iline => b.StartInDoc + iline.TextPositionOfInlineInParagraph + iline.InlineLength > trange.Start &&
             b.StartInDoc + iline.TextPositionOfInlineInParagraph < trange.End)).ToList();
      
      //Edge case
      if (AllSelectedInlines.Count == 0)
         AllSelectedInlines = Blocks.Where(b => b.IsParagraph).SelectMany(b =>
            ((Paragraph)b).Inlines.Where(iline => b.StartInDoc + iline.TextPositionOfInlineInParagraph + iline.InlineLength >= trange.Start &&
             b.StartInDoc + iline.TextPositionOfInlineInParagraph < trange.End)).ToList();

      if (AllSelectedInlines.Count == 0)
         return [];

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
            insertLastInline.InlineText = lastInlineText[..lastInlineSplitIndex];
            lastInline.InlineText = lastInlineText[lastInlineSplitIndex..];
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
            insertFirstInline.InlineText = firstInlineText[..firstInlineSplitIndex];
            insertLastInline.InlineText = firstInlineText[firstInlineSplitIndex..];

            startPar.Inlines.Insert(indexOfLastInline, insertFirstInline);
         }
      }
      else
      {
         //split last run and remove trailing excess run from list
         if (!RangeEndsAtInlineEnd)
         {
            lastInline.InlineText = lastInlineText[..lastInlineSplitIndex];
            AllSelectedInlines.Add(lastInline);

            insertLastInline.InlineText = lastInlineText[lastInlineSplitIndex..];
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
            firstInline.InlineText = firstInlineText[..firstInlineSplitIndex];
            insertFirstInline.InlineText = firstInlineText[firstInlineSplitIndex..];
            AllSelectedInlines.Remove(firstInline);
            AllSelectedInlines.Insert(0, insertFirstInline);
            
            startPar.Inlines.Insert(indexOfFirstInline + 1, insertFirstInline);
         }
      }

      startPar.CallRequestInlinesUpdate();
      endPar.CallRequestInlinesUpdate();
      UpdateBlockAndInlineStarts(Blocks.IndexOf(startPar));
 
    
      return AllSelectedInlines;

   }

   internal void RemoveEmptyParagraphs(int upToParNo)
   {
      for (int idx = Blocks.Count - 1; idx >= upToParNo; idx--)
      {
         Paragraph p = (Paragraph)Blocks[idx];
         if (p.Inlines.Count == 0 || (p.Inlines.Count == 1 && p.Inlines[0].InlineLength == 0))
            Blocks.Remove(p);
      }
   }

   internal void RemoveEmptyInlines(List<int> processParIndexes)
   {
      List<IEditable> toDeleteRuns = [];
      //for (int idx = 0; idx < processParIndexes.Count; idx ++)
      for (int idx = processParIndexes.Count - 1; idx >=0; idx --)
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

      //splitPos = Math.Min(splitPos, inlineToSplit.InlineLength);

      string part2Text = inlineToSplit.InlineText[splitPos..];
      inlineToSplit.InlineText = inlineToSplit.InlineText[..splitPos];
      IEditable insertInline = inlineToSplit.Clone();
      insertInline.InlineText = part2Text;
      if (insertInline.InlineText != "")
         inlines.Insert(runIdx + 1, insertInline);

      return [inlineToSplit, insertInline];
   }


}