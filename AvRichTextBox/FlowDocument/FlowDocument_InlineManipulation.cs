using DocumentFormat.OpenXml.Drawing.Diagrams;
using System.Collections.ObjectModel;

namespace AvRichTextBox;

public partial class FlowDocument
{
   internal int GetCharPosInInline(IEditable inline, int absPos)
   {
      if (AllParagraphs.FirstOrDefault(p => p.Id == inline.MyParagraphId) is not Paragraph inlinePar) return -1;
      return absPos - inlinePar.StartInDoc - inline.TextPositionOfInlineInParagraph;
   }
     
   
   internal (List<IEditable> createdInlines, (int idLeft, int idRight) edgeIds) GetTextRangeInlines(TextRange trange, bool addToDoc)
   {

      (int idLeft, int idRight) edgeIds = new(-1, -1);

      List<IEditable> AllSelectedInlines = [.. AllParagraphs.SelectMany(p => p.Inlines.Where(iline =>
      {
         int ilineAbsoluteStart = p.StartInDoc + iline.TextPositionOfInlineInParagraph;
         int ilineAbsoluteEnd = ilineAbsoluteStart + iline.InlineLength;

         //bool AtStart = (p.Inlines.IndexOf(iline) == 0) switch   //$$$$$$$$$$$$$$$$$$$$
         //{
         //   true => ilineAbsoluteEnd >= trange.Start,
         //   false => ilineAbsoluteEnd > trange.Start,
         //};

         bool AtStart = ilineAbsoluteEnd > trange.Start;
          bool withinRange = iline.IsLastInlineOfParagraph switch
         {
            true =>  AtStart && ilineAbsoluteStart <= trange.End,
            false => AtStart && ilineAbsoluteStart < trange.End
         };

         return withinRange;
      
      }
      ))];

      //Edge case where range length is 0 and starts at inline end
      if (AllSelectedInlines.Count == 0)
         AllSelectedInlines = [.. AllParagraphs.SelectMany(p => p.Inlines.Where(iline => 
         {
            var ilineAbsoluteStart = p.StartInDoc + iline.TextPositionOfInlineInParagraph;
            return (iline is EditableRun) && ilineAbsoluteStart + iline.InlineLength >= trange.Start && ilineAbsoluteStart < trange.End; 
         }
         ))];


      if (AllSelectedInlines.Count == 0 ||
         trange.GetStartPar() is not Paragraph startPar ||
         trange.GetEndPar() is not Paragraph endPar)
      {
         edgeIds.idRight = trange.StartInline?.Id ?? 0;
         return ([], edgeIds);
      }

      if (!addToDoc)
      {  // make clones when copying
         AllSelectedInlines = AllSelectedInlines.ConvertAll(il =>
         {
            IEditable clonedInline = il.Clone();
            if (il.IsLastInlineOfParagraph)  //replace paragraph ends with \r\n sequence
               clonedInline.InlineText += Environment.NewLine;  // TODO: Add paragraph properties here to create rtf \pard - IEditable will have list of paragraph properties
            return clonedInline;
         });
      }

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

      if (addToDoc)
      {
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
            {  //Debug.WriteLine("lastinlinesplitinex = " + lastInlineSplitIndex + "\ninlintext = " +  lastInlineText);

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
            if (!RangeStartsAtInlineStart && firstInlineText != "")
            {  //Debug.WriteLine("firstinline text = " + firstInlineText + ", " + firstInlineText.Length + ", splitidx = " + firstInlineSplitIndex);

               firstInline.InlineText = firstInlineText[..firstInlineSplitIndex];
               insertFirstInline.InlineText = firstInlineText[firstInlineSplitIndex..];
               AllSelectedInlines.Remove(firstInline);
               AllSelectedInlines.Insert(0, insertFirstInline);
               startPar.Inlines.Insert(indexOfFirstInline + 1, insertFirstInline);
            }
         }
         //Debug.WriteLine("\nInlines to convert=\n" + string.Join("\n", AllSelectedInlines.ConvertAll(il => il.InlineText + " :: " + il.Id)));
      }

      //startPar.CallRequestInlinesUpdate();
      //endPar.CallRequestInlinesUpdate();

      UpdateBlockAndInlineStarts(startPar);   // not necessary?

      return (AllSelectedInlines, edgeIds);

   }

   internal List<IEditable> SplitRunAtPos(int charIdxInDoc, IEditable inlineToSplit, int splitPos)
   {      
      //if (inlineToSplit.IsUIContainer)
      //   return [new EditableRun(""), inlineToSplit];

      if (GetContainingParagraph(charIdxInDoc) is not Paragraph containingPar) return [];
      ObservableCollection<IEditable> inlines = containingPar.Inlines;
      
      if (inlines.Count == 1 && charIdxInDoc == containingPar.StartInDoc + inlines[0].InlineLength) return [inlines[0]];

      disableRunTextUndo = true;

      int runIdx = inlines.IndexOf(inlineToSplit);

      string part2Text = inlineToSplit.InlineText[splitPos..];

      inlineToSplit.InlineText = inlineToSplit.InlineText[..splitPos];
      IEditable insertInline = inlineToSplit.Clone();
      insertInline.InlineText = part2Text;
      inlines.Insert(runIdx + 1, insertInline);
      
      disableRunTextUndo = false;

      return [inlineToSplit, insertInline];
   }

   internal Paragraph? GetNextParagraph(Paragraph par)
   {
      List<Paragraph> allPars = AllParagraphs;
      int myindex = allPars.IndexOf(par);
      if (myindex == allPars.Count - 1) return null!;
      return allPars[myindex + 1]  ?? null;
      
   }
   
   internal Paragraph? GetPreviousParagraph(Paragraph par)
   {
      List<Paragraph> allPars = AllParagraphs;
      int myindex = allPars.IndexOf(par);
      return myindex == 0 ? null : allPars[myindex - 1];

   }

   internal IEditable? GetStartInline(int charIndex)
   {
      List<Paragraph> allPars = AllParagraphs;
      if (allPars.LastOrDefault(b => b.StartInDoc <= charIndex) is Paragraph startPar)
      {
         //Check if start is at end of last paragraph (cannot span from end of a paragraph)
         if (startPar != allPars.Last() && startPar.EndInDoc == charIndex)
         {
            return null;
         }

         IEditable? startInline = null;
         bool IsAtLineBreak = false;
         if (startPar.Inlines.LastOrDefault(ied => startPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= charIndex) is IEditable startInlineReal)
         {
            //if (startPar.Inlines.LastOrDefault(ied => !ied.IsLineBreak && startPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= charIndex) is IEditable lastInline)
            if (startPar.Inlines.LastOrDefault(ied => startPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= charIndex) is IEditable lastInline)
               startInline = lastInline;
            IsAtLineBreak = startInline != startInlineReal;
         }
         return startInline;

      }
      else
         return null;

   }

}