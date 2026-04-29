
namespace AvRichTextBox;

public partial class FlowDocument
{

   internal void ExtendSelectionRight()
   {
      Selection.BiasForwardEnd = true;

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeRight:

            SelectionExtendMode = ExtendMode.ExtendModeRight;

            if (Selection.EndParagraph == AllParagraphs.ToList()[^1] && Selection.EndParagraph.SelectionEndInBlock == Selection.EndParagraph.TextLength)
               return;  // End of document

            Selection.End += 1;

            break;

         case ExtendMode.ExtendModeLeft:

            Selection.Start += 1;
            if (Selection.Start == Selection.End)
               SelectionExtendMode = ExtendMode.ExtendModeRight;

            break;
      }

      ScrollInDirection?.Invoke(1);

   }

   internal void ExtendSelectionLeft()
   {
      Selection.BiasForwardEnd = false;

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeLeft:
            if (Selection.Start == 0) return;
            Selection.Start -= 1;
            SelectionExtendMode = ExtendMode.ExtendModeLeft;
            break;

         case ExtendMode.ExtendModeRight:
            if (Selection.End == 0) return;
            Selection.End -= 1;
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
      {
         // No space found - go to end of paragraph text
         return startP.StartInDoc + startP.TextLength;
      }
      else
      {
         // Go to position after the space
         return startP.StartInDoc + indexNext + 1;
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
      {
         // No space found - go to start of paragraph
         return startP.StartInDoc;
      }
      else
      {
         // Go to position after the space (right of space)
         return startP.StartInDoc + indexNext + 1;
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

            if (Selection.EndParagraph == allPars[^1] && Selection.StartParagraph.IsEndAtLastLine)
            {
               Selection.End = Selection.EndParagraph.StartInDoc + Selection.EndParagraph.TextLength;
               return;  // last line of document
            }
            
            Paragraph origEndPar = Selection.EndParagraph;
            
            int nextEnd = Selection.EndParagraph.StartInDoc + Selection.EndParagraph.CharNextLineEnd;
   
            if (Selection.EndParagraph.IsEndAtLastLine)
            {
               if (Selection.EndParagraph != allPars[^1])
               {
                  int nextParIndex = Blocks.IndexOf(Selection.EndParagraph) + 1;
                  Paragraph nextPar = (Paragraph)allPars[nextParIndex];
                  Selection.End = Math.Min(nextPar.StartInDoc + nextPar.BlockLength - 1, nextEnd);
               }
            }
            else
               Selection.End = nextEnd;


            //for selection continuity
            if (Selection.EndParagraph != origEndPar)
            {
               origEndPar.SelectionEndInBlock = origEndPar.TextLength;
               Selection.EndParagraph.SelectionStartInBlock = 0;
            }

            break;

         case ExtendMode.ExtendModeLeft:  // Hitting down key reduces selection range from top 

            if (Selection.StartParagraph == allPars[^1] && Selection.StartParagraph.IsStartAtLastLine)
               return;  // last line of document

            int newStart = Selection.StartParagraph.StartInDoc + Selection.StartParagraph.CharNextLineStart;

            if (AllParagraphs.IndexOf(Selection.StartParagraph) < AllParagraphs.Count - 1)
            {
               Paragraph nextPar = allPars[allPars.IndexOf(Selection.StartParagraph) + 1];
               int charsFromStart = Selection.StartParagraph.SelectionStartInBlock - Selection.StartParagraph.FirstIndexLastLine;
               if (Selection.StartParagraph.IsStartAtLastLine)
               {
                  charsFromStart = Math.Min(charsFromStart, nextPar.TextLength);
                  newStart = nextPar.StartInDoc + charsFromStart;
               }
            }

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
      Paragraph? prevPar = null;
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
               

            Paragraph origStartPar = Selection.StartParagraph;
            if (Selection.StartParagraph.IsStartAtFirstLine)
            {
               prevPar = allPars[allPars.IndexOf(Selection.StartParagraph) - 1];
               Selection.Start = Math.Min(prevPar.StartInDoc + prevPar.BlockLength - 2, prevPar.StartInDoc + prevPar.FirstIndexLastLine + Selection.StartParagraph.CharPrevLineStart);
            }
            else
               Selection.Start = Selection.StartParagraph.StartInDoc + Selection.StartParagraph.CharPrevLineStart;

            //for selection continuity
            SelectionExtendMode = ExtendMode.ExtendModeLeft;
            if (Selection.StartParagraph != origStartPar)
            {
               origStartPar.SelectionStartInBlock = 0;
               Selection.StartParagraph.SelectionEndInBlock = Selection.StartParagraph.TextLength;
            }

            break;


         case ExtendMode.ExtendModeRight: // Hitting up key reduces selection range from bottom 

            int newEnd = Selection.EndParagraph.StartInDoc + Selection.EndParagraph.CharPrevLineEnd;

            if (AllParagraphs.IndexOf(Selection.EndParagraph) > 0)
            {
               prevPar = allPars[allPars.IndexOf(Selection.EndParagraph) - 1];
               int charsFromStart = Selection.EndParagraph.SelectionEndInBlock;
               if (Selection.EndParagraph.IsEndAtFirstLine)
               {
                  charsFromStart = Math.Min(charsFromStart, prevPar.TextLength);
                  newEnd = prevPar.StartInDoc + prevPar.FirstIndexLastLine + charsFromStart;
               }
                  
            }

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


