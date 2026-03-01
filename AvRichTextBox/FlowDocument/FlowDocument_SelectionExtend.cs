
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

   internal void ExtendSelectionDown()
   {
      Selection.BiasForwardEnd = true;

      List<Paragraph> allPars = AllParagraphs;

      switch (SelectionExtendMode)
      {

         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeRight:  // Hitting down key increases selection range from bottom

            SelectionExtendMode = ExtendMode.ExtendModeRight;

            if (Selection.EndParagraph == allPars[^1] && Selection.End == Text.Length)
               return;  // last line of document
            
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
                  Selection.StartParagraph.CollapseToStart();
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
               return;  // first line of document

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
                  Selection.EndParagraph.CollapseToStart();
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

   internal void EnsureSelectionContinuity()
   {

      List<Paragraph> allPars = AllParagraphs;

      foreach (Paragraph p in allPars.Where(p => !SelectionParagraphs.Contains(p)))
          p.ClearSelection(); 

      if (SelectionParagraphs.Count > 1)
      {
         for (int i = 0; i < SelectionParagraphs.Count; i++)
         {
            Paragraph selPar = SelectionParagraphs[i];
            switch (i)
            {
               case 0:
                  //ensure first par selected to end
                  selPar.SelectionEndInBlock = selPar.BlockLength;
                  break;

               case int last when last == SelectionParagraphs.Count - 1:
                  //ensure last par selected from start
                  selPar.SelectionStartInBlock = 0;
                  break;

               default:
                  selPar.SelectionStartInBlock = 0;
                  selPar.SelectionEndInBlock = selPar.BlockLength;
                  break;
            }
         }
      }

      foreach (Paragraph p in SelectionParagraphs)
      {
         if (p.IsTableCellBlock)
            p.OwningCell.Selected = (p.SelectionStartInBlock == 0 && p.SelectionEndInBlock == p.BlockLength);
      }

   }

}


