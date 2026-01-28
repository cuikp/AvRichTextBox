using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AvRichTextBox;

public partial class FlowDocument
{

   internal List<IEditable> GetRangeInlines(TextRange trange)
   {
      if (trange.GetStartPar() is not Paragraph startPar) return [];
      if (trange.GetEndPar() is not Paragraph endPar) return [];

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
         if (il.IsLastInlineOfParagraph)  //replace paragraph ends with \r\n sequence
            clonedInline.InlineText += Environment.NewLine;
         return clonedInline; 
      });

      //Edge case
      if (AllSelectedInlines.Count == 0)
         AllSelectedInlines = Blocks.Where(b => b.IsParagraph).SelectMany(b =>
            ((Paragraph)b).Inlines.Where(iline => b.StartInDoc + iline.TextPositionOfInlineInParagraph + iline.InlineLength >= trange.Start &&
             b.StartInDoc + iline.TextPositionOfInlineInParagraph < trange.End)).ToList().ConvertAll(il => il.Clone());

      IEditable firstInline = AllSelectedInlines[0];
      int firstInlineSplitIndex = Math.Min(trange.Start - startPar.StartInDoc - firstInline.TextPositionOfInlineInParagraph, firstInline.InlineText.Length);

      if (AllSelectedInlines.Count == 1)
      {
         int lastInlineSplitIndex = trange.End - endPar.StartInDoc - firstInline.TextPositionOfInlineInParagraph;
         //firstInline.InlineText = firstInline.InlineText[firstInlineSplitIndex..lastInlineSplitIndex];
         firstInline.InlineText = firstInline.IsEmpty ? "" : firstInline.InlineText[firstInlineSplitIndex..lastInlineSplitIndex];
      }
      else
      {
         IEditable lastInline = AllSelectedInlines[^1];
         int lastInlineSplitIndex = trange.End - endPar.StartInDoc - lastInline.TextPositionOfInlineInParagraph;
         firstInline.InlineText = firstInline.InlineText[firstInlineSplitIndex ..];
         lastInline.InlineText = lastInline.InlineText[..lastInlineSplitIndex];
      }

      return AllSelectedInlines;

   }

   
   internal List<IEditable> GetRangeInlinesAndAddToDoc(TextRange trange, out (int idLeft, int idRight) edgeIds)
   {
      edgeIds = new();

      List<IEditable> AllSelectedInlines = [.. Blocks.OfType<Paragraph>().SelectMany(p => p.Inlines.Where(iline =>
      {
         var ilineAbsoluteStart = p.StartInDoc + iline.TextPositionOfInlineInParagraph;
         return ilineAbsoluteStart + iline.InlineLength > trange.Start && ilineAbsoluteStart < trange.End;
      }
      ))];

      //Edge case where range length is 0 and starts at inline end
      if (AllSelectedInlines.Count == 0)
         AllSelectedInlines = [.. Blocks.OfType<Paragraph>().SelectMany(p => p.Inlines.Where(iline => 
         {
            var ilineAbsoluteStart = p.StartInDoc + iline.TextPositionOfInlineInParagraph;
            return ilineAbsoluteStart + iline.InlineLength >= trange.Start && ilineAbsoluteStart < trange.End; 
         }
         ))];
            

      if (AllSelectedInlines.Count == 0 ||
         trange.GetStartPar() is not Paragraph startPar ||
         trange.GetEndPar() is not Paragraph endPar) 
         return [];

      //Debug.WriteLine("\ntouched inlines=\n" + string.Join("\n", AllSelectedInlines.ConvertAll(il => il.InlineText + " :: " + il.Id)));

      IEditable firstInline = AllSelectedInlines[0];
      IEditable lastInline = AllSelectedInlines[^1];
      IEditable insertLastInline = lastInline.Clone();
      IEditable insertFirstInline = firstInline.Clone();

      edgeIds.idLeft = firstInline.Id;
      edgeIds.idRight = lastInline.Id;

      int lastInlineSplitIndex = trange.End - endPar.StartInDoc - lastInline.TextPositionOfInlineInParagraph;
      int firstInlineSplitIndex = trange.Start - startPar.StartInDoc - firstInline.TextPositionOfInlineInParagraph;
      bool RangeEndsAtInlineEnd = lastInlineSplitIndex >= lastInline.InlineLength;

      string lastInlineText = lastInline.InlineText;
      string firstInlineText = firstInline.InlineText;
      int indexOfLastInline = endPar.Inlines.IndexOf(lastInline);

      if (AllSelectedInlines.Count == 1)
      {  // Range contained within one inline
         
         if (!RangeEndsAtInlineEnd)
         {
            insertLastInline.InlineText = lastInlineText[..lastInlineSplitIndex];
            lastInline.InlineText = lastInlineText[lastInlineSplitIndex..];
            AllSelectedInlines.Remove(lastInline);
            AllSelectedInlines.Add(insertLastInline);
            endPar.Inlines.Insert(indexOfLastInline, insertLastInline);

            firstInlineText = insertLastInline.InlineText;
            insertFirstInline = insertLastInline.Clone();
            
            firstInlineSplitIndex = Math.Min(firstInlineSplitIndex, firstInlineText.Length);
         }
         
         bool RangeStartsAtInlineStart = firstInlineSplitIndex <= 0;

         if (!RangeStartsAtInlineStart)
         {
            insertFirstInline.InlineText = firstInlineText[..firstInlineSplitIndex];
            insertLastInline.InlineText = firstInlineText[firstInlineSplitIndex..];
            startPar.Inlines.Insert(indexOfLastInline, insertFirstInline);
            edgeIds.idLeft = insertFirstInline.Id;

            if (RangeEndsAtInlineEnd)
               lastInline.InlineText = firstInlineText[firstInlineSplitIndex..];
         }
      }
      else
      {
         //split last run and remove trailing excess run from list
         if (!RangeEndsAtInlineEnd)
         {
            insertLastInline.InlineText = lastInlineText[..lastInlineSplitIndex];
            lastInline.InlineText = lastInlineText[lastInlineSplitIndex..];
            AllSelectedInlines.Remove(lastInline);
            AllSelectedInlines.Add(insertLastInline);
            endPar.Inlines.Insert(indexOfLastInline, insertLastInline);

            firstInlineSplitIndex = Math.Min(firstInlineSplitIndex, firstInlineText.Length);
         }
                  
         int indexOfFirstInline = startPar.Inlines.IndexOf(firstInline);
                  
         bool RangeStartsAtInlineStart = firstInlineSplitIndex <= 0;

         // split first run and remove initial excess run from list
         if (!RangeStartsAtInlineStart)
         {
            firstInline.InlineText = firstInlineText[..firstInlineSplitIndex];
            insertFirstInline.InlineText = firstInlineText[firstInlineSplitIndex..];
            AllSelectedInlines.Remove(firstInline);
            AllSelectedInlines.Insert(0, insertFirstInline);
            
            startPar.Inlines.Insert(indexOfFirstInline + 1, insertFirstInline);
            //if (RangeEndsAtInlineEnd)
               //lastInline.InlineText = firstInlineText[firstInlineSplitIndex..];
               
         }

         //Debug.WriteLine("\nInlines to convert=\n" + string.Join("\n", AllSelectedInlines.ConvertAll(il => il.InlineText + " :: " + il.Id)));

      }

      startPar.CallRequestInlinesUpdate();
      endPar.CallRequestInlinesUpdate();
      UpdateBlockAndInlineStarts(Blocks.IndexOf(startPar));
 
    
      return AllSelectedInlines;

   }

   internal int RemoveEmptyParagraphs(int upToParNo)
   {
      int removedParCount = 0;
      for (int idx = Blocks.Count - 1; idx >= upToParNo; idx--)
      {
         Paragraph p = (Paragraph)Blocks[idx];
         if (p.Inlines.Count == 0 || (p.Inlines.Count == 1 && p.Inlines[0].InlineLength == 0))
         {
            Blocks.Remove(p);
            removedParCount++;
         }
      }
      return removedParCount;
   }

   //internal void RemoveEmptyInlines(List<int> processParIndexes)
   //{
   //   List<IEditable> toDeleteRuns = [];
   //   //for (int idx = 0; idx < processParIndexes.Count; idx ++)
   //   for (int idx = processParIndexes.Count - 1; idx >=0; idx --)
   //   {
   //      Paragraph p = (Paragraph)Blocks[processParIndexes[idx]];
   //      for (int iedno = p.Inlines.Count - 1; iedno >= 0; iedno -= 1)
   //         if (p.Inlines[iedno].InlineText == "" && p.Inlines.Count > 1)
   //            p.Inlines.RemoveAt(iedno);

   //      if (p.Inlines.Count == 0 && p != Blocks[processParIndexes[0]])
   //         Blocks.Remove(p);
   //   }

   //   UpdateBlockAndInlineStarts(processParIndexes[0]);

   //}

   internal List<IEditable> SplitRunAtPos(int charPos, IEditable inlineToSplit, int splitPos)
   {
      //if (inlineToSplit.IsUIContainer)
      //   return [new EditableRun(""), inlineToSplit];

      ObservableCollection<IEditable> inlines = GetContainingParagraph(charPos).Inlines;
      int runIdx = inlines.IndexOf(inlineToSplit);

      //splitPos = Math.Min(splitPos, inlineToSplit.InlineLength);

      string part2Text = inlineToSplit.InlineText[splitPos..];


      inlineToSplit.InlineText = inlineToSplit.InlineText[..splitPos];
      IEditable insertInline = inlineToSplit.Clone();
      insertInline.InlineText = part2Text;
      inlines.Insert(runIdx + 1, insertInline);

      return [inlineToSplit, insertInline];
   }

   internal Paragraph? GetNextParagraph(Paragraph par)
   {
      int myindex = Blocks.IndexOf(par);
      return myindex == Blocks.Count - 1 ? null : (Paragraph)Blocks[myindex + 1];
   }
   
   internal Paragraph? GetPreviousParagraph(Paragraph par)
   {
      int myindex = Blocks.IndexOf(par);
      return myindex == 0 ? null : (Paragraph)Blocks[myindex - 1];
   }

   internal IEditable? GetNextInline(IEditable inline)
   {
      IEditable? returnIED = null;

      int myindex = inline.MyParagraph!.Inlines.IndexOf(inline);
     
      if (myindex < inline.MyParagraph.Inlines.Count - 1)
         returnIED = inline.MyParagraph!.Inlines[myindex + 1];
      else
      {
         Paragraph? nextPar = GetNextParagraph(inline.MyParagraph);
         if (nextPar == null)
            return null;
         else
            if (nextPar.Inlines.Count > 0)
               returnIED = nextPar.Inlines[0];
      }
      return returnIED;
   }

   internal IEditable? GetPreviousInline(IEditable inline) 
   {
      IEditable? returnIED = null;
      int myindex = inline.MyParagraph!.Inlines.IndexOf(inline);

      if (myindex > 0)
         returnIED = inline.MyParagraph!.Inlines[myindex - 1];
      else
      {
         Paragraph? prevPar = GetPreviousParagraph(inline.MyParagraph);
         if (prevPar == null)
            return null;
         else
         {
            if (prevPar.Inlines.Count > 0)
               returnIED = prevPar.Inlines.Last();
         }
      }
      return returnIED;
   }


}