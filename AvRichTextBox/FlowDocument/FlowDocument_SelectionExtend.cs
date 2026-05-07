
using Avalonia.Media;
using DocumentFormat.OpenXml.Wordprocessing;
using ReactiveUI;

namespace AvRichTextBox;

public partial class FlowDocument
{

   internal void ExtendSelectionRight()
   {
      Selection.BiasForwardEnd = false;
      
      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeRight:

            SelectionExtendMode = ExtendMode.ExtendModeRight;

            if (Selection.EndParagraph == AllParagraphs.ToList()[^1] && Selection.EndParagraph.SelectionEndInBlock == Selection.EndParagraph.TextLength)
               return;  // End of document

            Selection.End = GetNextPosition();

            break;

         case ExtendMode.ExtendModeLeft:

            Selection.BiasForwardStart = false;

            Selection.Start = GetNextPosition();

            if (Selection.Start == Selection.End)
               SelectionExtendMode = ExtendMode.ExtendModeRight;

            break;
      }

      ScrollInDirection?.Invoke(1);

   }

   internal void ExtendSelectionLeft()
   {
      Selection.BiasForwardEnd = false;
      Selection.BiasForwardStart = true;

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeLeft:
            if (Selection.Start == 0) return;

            Selection.Start = GetPreviousPosition();

            SelectionExtendMode = ExtendMode.ExtendModeLeft;
            break;

         case ExtendMode.ExtendModeRight:
            if (Selection.End == 0) return;

            Selection.BiasForwardEnd = true;

            Selection.End = GetPreviousPosition();

            if (Selection.Start == Selection.End)
               SelectionExtendMode = ExtendMode.ExtendModeLeft;
            break;
      }

      ScrollInDirection?.Invoke(-1);
   }

   internal void ExtendSelectionRightWord()
   {
      Selection.BiasForwardEnd = true;

      int targetPos = GetNextWordPosition();

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeRight:
            SelectionExtendMode = ExtendMode.ExtendModeRight;
            Selection.End = targetPos;
            break;

         case ExtendMode.ExtendModeLeft:
            if (targetPos >= Selection.End)
            {
               Selection.Start = Selection.End;
               Selection.End = targetPos;
               SelectionExtendMode = ExtendMode.ExtendModeRight;
            }
            else
               Selection.Start = targetPos;
            break;
      }

      ScrollInDirection?.Invoke(1);
   }

   internal void ExtendSelectionLeftWord()
   {
      Selection.BiasForwardEnd = false;

      int targetPos = GetPreviousWordPosition();

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeLeft:
            SelectionExtendMode = ExtendMode.ExtendModeLeft;
            Selection.Start = targetPos;
            break;

         case ExtendMode.ExtendModeRight:
            if (targetPos <= Selection.Start)
            {
               Selection.End = Selection.Start;
               Selection.Start = targetPos;
               SelectionExtendMode = ExtendMode.ExtendModeLeft;
            }
            else
               Selection.End = targetPos;
            break;
      }

      ScrollInDirection?.Invoke(-1);
   }

   internal void ExtendSelectionToDocStart()
   {
      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeLeft:
            SelectionExtendMode = ExtendMode.ExtendModeLeft;
            Selection.Start = 0;
            break;

         case ExtendMode.ExtendModeRight:
            // Was extending right, now selecting all the way to doc start
            // means we flip direction past the anchor
            Selection.End = Selection.Start;
            Selection.Start = 0;
            SelectionExtendMode = ExtendMode.ExtendModeLeft;
            break;
      }

      ScrollInDirection?.Invoke(-1);
   }

   internal void ExtendSelectionToDocEnd()
   {
      Selection.BiasForwardStart = false;
      Selection.BiasForwardEnd = false;

      List<Paragraph> allPars = AllParagraphs;
      int docEnd = allPars[^1].StartInDoc + allPars[^1].BlockLength - 1;

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeRight:
            SelectionExtendMode = ExtendMode.ExtendModeRight;
            Selection.End = docEnd;
            break;

         case ExtendMode.ExtendModeLeft:
            // Was extending left, now selecting all the way to doc end
            // means we flip direction past the anchor
            Selection.Start = Selection.End;
            Selection.End = docEnd;
            SelectionExtendMode = ExtendMode.ExtendModeRight;
            break;
      }

      ScrollInDirection?.Invoke(1);
   }

   private int GetPreviousPosition()
   {
      int currentPos = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.End : Selection.Start;
      if (currentPos <= 0)
         return 0;
      Paragraph startP = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.EndParagraph : Selection.StartParagraph;
      int posInBlock = currentPos - startP.StartInDoc;

      if (posInBlock <= 0)
      {  // At start of paragraph, move to end of previous paragraph
         return Math.Max(0, startP.StartInDoc - 1);
      }
      
      int computedPrevious = startP.StartInDoc + posInBlock - 1;

      // skip special
      if (Selection.Length > 0)
      {
         if (GetStartInline(computedPrevious) is EditableHyperlink hyperlink)
         {
            int absHyperlinkStart = startP.StartInDoc + hyperlink.TextPositionOfInlineInParagraph;
            int absHyperlinkEnd = absHyperlinkStart + hyperlink.InlineLength; 

            if (Selection.End <= absHyperlinkEnd && SelectionExtendMode == ExtendMode.ExtendModeLeft)
            {
               if (Selection.Start == absHyperlinkStart + 1)
                  Selection.End = absHyperlinkEnd;
            }
            else
               computedPrevious = absHyperlinkStart;
         }
      }


      if (GetStartInline(computedPrevious) is EditableLineBreak)
         computedPrevious -= 1;

      return computedPrevious;

   }
   
   private int GetNextPosition()
   {      
      int currentPos = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.Start : Selection.End;

      Paragraph startP = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.StartParagraph : Selection.EndParagraph;
      int posInBlock = currentPos - startP.StartInDoc;

      if (posInBlock >= startP.TextLength)
      {  // At end of paragraph, move to start of next paragraph
         int nextPos = startP.StartInDoc + startP.BlockLength;
         return Math.Min(nextPos, DocEndPoint - 1);
      }

      int computedNext = startP.StartInDoc + posInBlock + 1;

      // skip special
      if (Selection.Length > 0)
      {
         if (GetStartInline(computedNext - 1) is EditableHyperlink hyperlink)
         {
            int absHyperlinkStart = startP.StartInDoc + hyperlink.TextPositionOfInlineInParagraph;
            int absHyperlinkEnd = absHyperlinkStart + hyperlink.InlineLength; 

            if (Selection.Start >= absHyperlinkStart && SelectionExtendMode == ExtendMode.ExtendModeRight)
            {
               if (Selection.End == absHyperlinkEnd - 1)
                  Selection.Start = absHyperlinkStart;
            }
            else
               computedNext = absHyperlinkEnd;
         }

      }

      if (GetStartInline(computedNext - 1) is EditableLineBreak)
         computedNext += 1;

      return computedNext;

   }

   private int GetNextDown()
   {
      int currentPos = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.Start : Selection.End;

      Paragraph relPar = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.StartParagraph : Selection.EndParagraph;
      bool atParBottom = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? relPar.IsStartAtLastLine : relPar.IsEndAtLastLine;

      int posNextLineInBlock = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.StartParagraph.CharNextLineStart : Selection.EndParagraph.CharNextLineEnd; ;
      int computedNext = relPar.StartInDoc + posNextLineInBlock;

      if (atParBottom)
      {
         double currLeft = relPar.TextLayout.HitTestTextPosition(currentPos - relPar.StartInDoc).Left;
         int parIdx = AllParagraphs.IndexOf(relPar);
         if (parIdx < AllParagraphs.Count -1)
         {
            relPar = AllParagraphs[parIdx + 1];
            int textPosNewPar = Math.Min(relPar.BlockLength, relPar.TextLayout.HitTestPoint(new Point (currLeft, 0)).TextPosition);
            computedNext = relPar.StartInDoc + textPosNewPar;
         }
      }

      return computedNext;


   }

   private int GetNextUp()
   {
      

      int currentPos = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.End : Selection.Start;
      Paragraph relPar = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.EndParagraph : Selection.StartParagraph;

      int posPrevLineInBlock = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.EndParagraph.CharPrevLineEnd: Selection.StartParagraph.CharPrevLineStart;
      int computedPrev = relPar.StartInDoc + posPrevLineInBlock;

      bool atParTop = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? relPar.IsEndAtFirstLine : relPar.IsStartAtFirstLine;

      if (atParTop)
      {
         double currLeft = relPar.TextLayout.HitTestTextPosition(currentPos - relPar.StartInDoc).Left;
         int parIdx = AllParagraphs.IndexOf(relPar);
         if (parIdx > 0)
         {
            relPar = AllParagraphs[parIdx - 1];
            Point newPoint = new(currLeft, relPar.TextLayout.Height);
            //Debug.WriteLine("newpoint = " + newPoint.ToString());
            
            TextHitTestResult hitres = relPar.TextLayout.HitTestPoint(newPoint);
            //Debug.WriteLine("hitres = " + hitres.TextPosition);

            int textPosNewPar = Math.Min(relPar.BlockLength, relPar.TextLayout.HitTestPoint(new Point(currLeft, relPar.TextLayout.Height)).TextPosition);
            computedPrev = relPar.StartInDoc + textPosNewPar;
         }
      }

            
      return computedPrev;

   }


   /// <summary>
   /// Computes the position of the next word boundary (rightward) from the current selection end.
   /// </summary>
   private int GetNextWordPosition()
   {
      // Determine the anchor point based on extend mode
      int currentPos = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.Start : Selection.End;

      Paragraph startP = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.StartParagraph : Selection.EndParagraph;
      int posInBlock = currentPos - startP.StartInDoc;

      if (posInBlock >= startP.TextLength)
      {
         // At end of paragraph, move to start of next paragraph
         int nextPos = startP.StartInDoc + startP.BlockLength;
         return Math.Min(nextPos, DocEndPoint - 1);
      }

      string parText = startP.Text;

      // Skip any spaces at the current position
      int searchFrom = posInBlock;
      while (searchFrom < parText.Length && (parText[searchFrom] == ' ' || parText[searchFrom] == '\n'))
         searchFrom++;

      if (searchFrom >= parText.Length)
         return startP.StartInDoc + startP.TextLength;

      // Find next space from the adjusted position
      int indexNext = parText.IndexOf(' ', searchFrom);
      if (indexNext == -1)
      {  // No space found - go to end of paragraph text
         return startP.StartInDoc + startP.TextLength;
      }
      else
      {  // Go to position after the space
         int computedNext = startP.StartInDoc + indexNext + 1;

         // skip special
         if (GetStartInline(computedNext - 1) is EditableHyperlink hyperlink)
            computedNext = Selection.StartParagraph.StartInDoc + hyperlink.TextPositionOfInlineInParagraph + hyperlink.InlineLength;

         return computedNext;
      }
   }
     
   /// <summary>
   /// Computes the position of the previous word boundary (leftward) from the current selection start.
   /// </summary>
   private int GetPreviousWordPosition()
   {
      // Determine the anchor point based on extend mode
      int currentPos = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.End : Selection.Start;

      if (currentPos <= 0)
         return 0;

      Paragraph startP = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.EndParagraph : Selection.StartParagraph;
      int posInBlock = currentPos - startP.StartInDoc;

      if (posInBlock <= 0)
      {
         // At start of paragraph, move to end of previous paragraph
         return Math.Max(0, startP.StartInDoc - 1);
      }

      // Skip any spaces immediately to the left of current position
      int searchFrom = posInBlock - 1;
      string parText = startP.Text;
      while (searchFrom > 0 && (parText[searchFrom] == ' ' || parText[searchFrom] == '\n'))
         searchFrom--;

      if (searchFrom <= 0)
         return startP.StartInDoc;

      // Find previous space from the adjusted position
      int indexNext = parText.LastIndexOfAny(" \n".ToCharArray(), searchFrom);
      if (indexNext == -1)
      {  // No space found - go to start of paragraph
         return startP.StartInDoc;
      }
      else
      {  // Go to position after the space (right of space)
         int computedPrevious = startP.StartInDoc + indexNext + 1;

         // skip special
         if (GetStartInline(computedPrevious - 1) is EditableHyperlink hyperlink)
            computedPrevious = Selection.StartParagraph.StartInDoc + hyperlink.TextPositionOfInlineInParagraph;

         return computedPrevious;
      }
   }

   internal void ExtendSelectionDown()
   {
      Selection.BiasForwardEnd = true;

      List<Paragraph> allPars = AllParagraphs;

      switch (SelectionExtendMode)
      {

         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeRight:  // Hitting down key increases selection range from bottom

            SelectionExtendMode = ExtendMode.ExtendModeRight;

            Selection.End = GetNextDown();

            ////hyperlink encountered (select hyperlink)
            //if (GetStartInline(nextEnd) is EditableHyperlink hyperlink)
            //{
            //   if (AllParagraphs.LastOrDefault(p=> p.StartInDoc <= nextEnd) is Paragraph thisPar)
            //      Select(thisPar.StartInDoc + hyperlink.TextPositionOfInlineInParagraph, hyperlink.InlineLength);
            //   return;
            //}

            break;

         case ExtendMode.ExtendModeLeft:  // Hitting Down key reduces selection range from top 

            if (Selection.StartParagraph == allPars[^1] && Selection.StartParagraph.IsStartAtLastLine)
               return;  // last line of document

            int newStart = GetNextDown();

            if (newStart > Selection.End)
            {
               int oldEnd = Selection.End;
               Selection.End = newStart;
               Selection.Start = oldEnd;
               SelectionExtendMode = ExtendMode.ExtendModeRight;
            }
            else
               Selection.Start = newStart;

            break;
      }

      ScrollInDirection?.Invoke(1);
      
   }

   internal void ExtendSelectionUp()
   {
      Selection.BiasForwardEnd = false;

      List<Paragraph> allPars = AllParagraphs;

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeLeft:

            if (Selection.StartParagraph == allPars[0] && Selection.StartParagraph.IsStartAtFirstLine)
            {
               Selection.Start = 0;
               return;  // first line of document
            }

            Selection.Start = GetNextUp();

            ////hyperlink encountered (select hyperlink)
            //if (GetStartInline(nextStart) is EditableHyperlink hyperlink)
            //{
            //   if (AllParagraphs.LastOrDefault(p => p.StartInDoc <= nextStart) is Paragraph thisPar)
            //      Select(thisPar.StartInDoc + hyperlink.TextPositionOfInlineInParagraph, hyperlink.InlineLength);
            //   return;
            //}

            SelectionExtendMode = ExtendMode.ExtendModeLeft;

            break;


         case ExtendMode.ExtendModeRight: // Hitting up key reduces selection range from bottom 

            int newEnd = GetNextUp();

            if (newEnd < Selection.Start)
            {
               int oldStart = Selection.Start;
               Selection.Start = newEnd;
               Selection.End = oldStart;
               SelectionExtendMode = ExtendMode.ExtendModeLeft;
            }
            else
               Selection.End = newEnd;

            break;
      }

      ScrollInDirection?.Invoke(-1);
      

   }

  
}


