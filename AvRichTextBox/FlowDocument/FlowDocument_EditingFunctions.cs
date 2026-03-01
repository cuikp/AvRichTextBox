using DynamicData;

namespace AvRichTextBox;

public partial class FlowDocument
{
  
   internal int PasteInlinesIntoRange(TextRange tRange, List<IEditable> newInlines)
   {  // All of this should constitute one Undo operation
      //Debug.WriteLine("newinlines=\n" + string.Join("\n", newInlines.ConvertAll(il => il.InlineText)));

      disableRunTextUndo = true;

      int addedCharCount = 0;

      Paragraph startPar = tRange.StartParagraph;

      int rangeStart = tRange.Start;
      int deleteRangeLength = tRange.Length;
      int parIndex = AllParagraphs.IndexOf(startPar);

      Undos.Add(new PasteUndo(GetOverlappingParagraphsInRange(tRange), parIndex, this, rangeStart, deleteRangeLength - newInlines.Sum(nil=>nil.InlineLength)));

      //Delete selected range first
      if (tRange.Length > 0)
         DeleteRange(tRange, false);

      if (tRange.GetStartInline() is not IEditable startInline) return 0;

      List<IEditable> splitInlines = SplitRunAtPos(tRange.Start, startInline, GetCharPosInInline(startInline, tRange.Start));

      int insertionPt = startPar.Inlines.IndexOf(splitInlines[0]) + 1;

      int startInlineIndex = startPar.Inlines.IndexOf(splitInlines[0]) + 1;
      Paragraph addPar = startPar;
      int inlineno = 0;
      foreach (IEditable newinline in newInlines)
      {
         inlineno++;

         bool addnewpar = false;

         if (newinline.InlineText.EndsWith("\r\n"))
         {
            newinline.InlineText = newinline.InlineText[..^1];
            addnewpar = inlineno > 1;
            //Debug.WriteLine("addnew par? " + addnewpar +  " (" + newinline.InlineText + ")");
         }

         if (addnewpar)
         {
            List<IEditable> moveInlines = [.. addPar.Inlines.Take(new Range(0, startInlineIndex))];  // create an independent new list
            addPar.Inlines.RemoveMany(moveInlines);

            //Create new paragraph to insert
            addPar = new Paragraph(this);
            addPar.Inlines.AddRange(moveInlines);
            startInlineIndex = addPar.Inlines.Count;
            Blocks.Insert(parIndex, addPar);  /////$$$$$$
            addPar.CallRequestInlinesUpdate();
            UpdateBlockAndInlineStarts(addPar);
            addedCharCount += 1;
         }

         addPar.Inlines.Insert(startInlineIndex, newinline);
         addPar.CallRequestInlinesUpdate();
         UpdateBlockAndInlineStarts(addPar);
         addedCharCount += newinline.InlineLength;
      }

      if (splitInlines[0].InlineText == "")
         startPar.Inlines.Remove(splitInlines[0]);

      startPar.CallRequestInlinesUpdate();
      UpdateBlockAndInlineStarts(startPar);

      disableRunTextUndo = false;

      return addedCharCount;

   }


   internal void SetRangeToText(TextRange tRange, string newText)
   {  //The delete range and SetRangeToText should constitute one Undo operation

      Paragraph startPar = tRange.StartParagraph;
      int rangeStart = tRange.Start;
      int deleteRangeLength = tRange.Length;
      int parIndex = Blocks.IndexOf(startPar);

      Undos.Add(new PasteUndo(GetOverlappingParagraphsInRange(tRange), parIndex, this, rangeStart, deleteRangeLength - newText.Length));

      //Delete any selected text first
      if (tRange.Length > 0)
      {
         DeleteRange(tRange, false);
         tRange.CollapseToStart();
         SelectionExtendMode = ExtendMode.ExtendModeNone;
      }

      if (tRange.GetStartInline() is not IEditable startInline) return;

      List<IEditable> splitInlines = SplitRunAtPos(tRange.Start, startInline, GetCharPosInInline(startInline, tRange.Start));

      int startInlineIndex = startPar.Inlines.IndexOf(splitInlines[0]) + 1;

      if (splitInlines[0] is EditableRun sRun)
      {
         EditableRun newEditableRun = new(newText)
         {
            FontFamily = sRun.FontFamily,
            FontWeight = sRun.FontWeight,
            FontStyle = sRun.FontStyle,
            FontSize = sRun.FontSize,
            TextDecorations = sRun.TextDecorations,
            Background = sRun.Background
         };

         startPar.Inlines.Insert(startInlineIndex, newEditableRun);

         if (splitInlines[0].InlineText == "")
            startPar.Inlines.Remove(splitInlines[0]);

         startPar.CallRequestInlinesUpdate();
         UpdateBlockAndInlineStarts(startPar);

      }

   }

   internal void Undo()
   {
      if (Undos.Count > 0)
      {
         disableRunTextUndo = true;
         
         Undos.Last().PerformUndo();

         UpdateSelection();

         if (Undos.Last().UpdateTextRanges)
            UpdateTextRanges(Selection.Start, Undos.Last().UndoEditOffset);

         Undos.RemoveAt(Undos.Count - 1);

         UpdateSelectedParagraphs();
         

         ScrollInDirection?.Invoke(1);
         ScrollInDirection?.Invoke(-1);

         disableRunTextUndo = false;

      }
   }

   internal void RestoreDeletedBlocks(List<Paragraph> parClones, int blockIndex)
   {
      //Restore all of the previous paragraphs            
      Blocks.RemoveAt(blockIndex);
      Blocks.AddOrInsertRange(parClones, blockIndex);

      foreach (Paragraph p in parClones)
      {
         p.CallRequestInlinesUpdate();
         p.ClearSelection();
      }
  
      UpdateBlockAndInlineStarts(blockIndex);

   }

   

}