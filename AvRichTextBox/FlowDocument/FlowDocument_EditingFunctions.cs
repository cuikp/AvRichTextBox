using DynamicData;

namespace AvRichTextBox;

public partial class FlowDocument
{  

   internal void SetRangeToText(TextRange tRange, string newText)
   {  //The delete range and SetRangeToText should constitute one Undo operation

      Paragraph startPar = tRange.StartParagraph;
      int rangeStart = tRange.Start;
      int deleteRangeLength = tRange.Length;
      int parIndex = Blocks.IndexOf(startPar);
      bool firstParEmpty = startPar.Inlines[0] is EditableRun erun && erun.Text == "";

      Undos.Add(new PasteUndo(GetOverlappingParagraphsInRange(tRange), parIndex, this, rangeStart, deleteRangeLength - newText.Length, firstParEmpty, []));

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
            Background = sRun.Background,
            BaselineAlignment = sRun.BaselineAlignment,
            Foreground = sRun.Foreground
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