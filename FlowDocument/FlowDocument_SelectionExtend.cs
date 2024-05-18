using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Linq;

namespace AvRichTextBox;

public partial class FlowDocument
{

   internal void ExtendSelectionRight()
   {

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeRight:

            SelectionExtendMode = ExtendMode.ExtendModeRight;

            if (Selection.EndParagraph == Blocks[^1] && Selection.EndParagraph.SelectionEndInBlock == Selection.EndParagraph.Text.Length)
               return;  // End of document

            Selection!.End += 1;

            break;

         case ExtendMode.ExtendModeLeft:

            Selection!.Start += 1;
            if (Selection.Start == Selection.End)
               SelectionExtendMode = ExtendMode.ExtendModeRight;

            break;
      }

      ScrollInDirection!(1);

   }

   internal void ExtendSelectionLeft()
   {
      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeLeft:
            if (Selection!.Start == 0) return;
            Selection.Start -= 1;
            SelectionExtendMode = ExtendMode.ExtendModeLeft;
            break;

         case ExtendMode.ExtendModeRight:
            if (Selection!.End == 0) return;
            Selection!.End -= 1;
            if (Selection.Start == Selection.End)
               SelectionExtendMode = ExtendMode.ExtendModeLeft;
            break;
      }

      ScrollInDirection!(-1);
   }

   internal void ExtendSelectionDown()
   {

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeRight:

            SelectionExtendMode = ExtendMode.ExtendModeRight;

            if (Selection!.EndParagraph == Blocks[^1] && Selection.EndParagraph.IsEndAtLastLine)
               return;  // last line of document
            
            Paragraph origEndPar = Selection.EndParagraph;
            int nextEnd = Selection.EndParagraph.StartInDoc + Selection.EndParagraph.CharNextLineEnd;
            if (Blocks.IndexOf(origEndPar) < Blocks.Count - 1)
            {
               Paragraph nextParRight = (Paragraph)Blocks[Blocks.IndexOf(origEndPar) + 1];
               nextEnd = Math.Min(nextParRight.StartInDoc + nextParRight.BlockLength - 2, Selection.EndParagraph.StartInDoc + Selection.EndParagraph.CharNextLineEnd);
            }

            Selection.End = nextEnd;

            //for selection continuity
            if (Selection.EndParagraph != origEndPar)
            {
               origEndPar.SelectionEndInBlock = origEndPar.Text.Length;
               Selection.EndParagraph.SelectionStartInBlock = 0;
            }

            break;

         case ExtendMode.ExtendModeLeft:

            if (Selection!.StartParagraph == Blocks[^1] && Selection.StartParagraph.IsStartAtLastLine)
               return;  // last line of document

            int newStart = Selection.StartParagraph.StartInDoc + Selection.StartParagraph.CharNextLineStart;

            if (Blocks.IndexOf(Selection!.StartParagraph) < Blocks.Count - 1)
            {
               Block nextBlock = Blocks[Blocks.IndexOf(Selection.StartParagraph) + 1];
               int charsFromStart = Selection.StartParagraph.SelectionStartInBlock - Selection.StartParagraph.FirstIndexLastLine;
               if (Selection.StartParagraph.IsStartAtLastLine)
               {
                  newStart = nextBlock.StartInDoc + charsFromStart;
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
               Selection!.Start = newStart;

            break;
      }

      ScrollInDirection!(1);
      
   }

   internal void ExtendSelectionUp()
   {
      Paragraph? prevPar = null;

      switch (SelectionExtendMode)
      {
         case ExtendMode.ExtendModeNone:
         case ExtendMode.ExtendModeLeft:

            if (Selection!.StartParagraph == Blocks[0] && Selection.StartParagraph.IsStartAtFirstLine)
               return;  // first line of document

            Paragraph origStartPar = Selection.StartParagraph;
            if (Selection.StartParagraph.IsStartAtFirstLine)
            {
               prevPar = (Paragraph)Blocks[Blocks.IndexOf(Selection.StartParagraph) - 1];
               Selection.Start = Math.Min(prevPar.StartInDoc + prevPar.BlockLength - 2, prevPar.StartInDoc + prevPar.FirstIndexLastLine + Selection.StartParagraph.CharPrevLineStart);
            }
            else
               Selection.Start = Selection.StartParagraph.StartInDoc + Selection.StartParagraph.CharPrevLineStart;

            //for selection continuity
            SelectionExtendMode = ExtendMode.ExtendModeLeft;
            if (Selection.StartParagraph != origStartPar)
            {
               origStartPar.SelectionStartInBlock = 0;
               Selection.StartParagraph.SelectionEndInBlock = Selection.StartParagraph.Text.Length;
            }

            break;


         case ExtendMode.ExtendModeRight:

            int newEnd = Selection.EndParagraph.StartInDoc + Selection.EndParagraph.CharPrevLineEnd;

            if (Blocks.IndexOf(Selection!.EndParagraph) > 0)
            {
               prevPar = (Paragraph)Blocks[Blocks.IndexOf(Selection.EndParagraph) - 1];
               int charsFromStart = Selection.EndParagraph.SelectionEndInBlock;
               if (Selection.EndParagraph.IsEndAtFirstLine)
               {
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
               Selection!.End = newEnd;

            break;
      }

      ScrollInDirection!(-1);
      

   }

   internal void EnsureSelectionContinuity()
   {

      foreach (Paragraph p in Blocks.Where(p => !SelectionParagraphs.Contains(p)))
          p.ClearSelection(); 

      if (SelectionParagraphs.Count > 1)
      {
         SelectionParagraphs[0].SelectionEndInBlock = SelectionParagraphs[0].BlockLength;

         for (int i = Blocks.IndexOf(SelectionParagraphs[0]) + 1; i < Blocks.IndexOf(SelectionParagraphs[^1]); i++)
         {
            Blocks[i].SelectionStartInBlock = 0;
            Blocks[i].SelectionEndInBlock = Blocks[i].BlockLength;
         }

         SelectionParagraphs[^1].SelectionStartInBlock = 0;
      }


      ////Temp for debugging
      //foreach (Paragraph p in Blocks.Where(p => !SelectionParagraphs.Contains(p)))
      //    p.Background = new SolidColorBrush(Colors.Transparent); 
      //foreach (Paragraph p in Blocks.Where(p => SelectionParagraphs.Contains(p)))
      //   p.Background = new SolidColorBrush(Colors.Red);

   }

}


