using Avalonia.Controls;
using Avalonia.Controls.Documents;
using DynamicData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AvRichTextBox;

public partial class FlowDocument
{

   internal void InsertChar(string? insertChar)
   {
      IEditable startInline = Selection.GetStartInline();

      if (startInline.GetType() == typeof(EditableInlineUIContainer))
         return;

      if (insertChar != null)
      {
         if (Selection!.Length > 0)
         {
            DeleteRange(Selection, true);
            Selection.CollapseToStart();
            SelectionExtendMode = ExtendMode.ExtendModeNone;
            startInline = Selection.GetStartInline();
         }

         int insertIdx = 0;
         if (InsertRunMode)
         {
            List<IEditable> applyInlines = CreateNewInlinesForRange(Selection);
            if (applyInlines.Count == 0)
            {
               applyInlines.Add(new EditableRun(""));
               Selection.StartParagraph.Inlines.Insert(0, applyInlines[0]);
            }
            startInline = applyInlines[0];
            startInline.InlineText = insertChar;
            toggleFormatRun!(startInline);
            InsertRunMode = false;
         }
         else
         {
            insertIdx = Selection.GetStartInline().GetRangeStartInInline(Selection);
            startInline.InlineText = startInline.InlineText!.Insert(insertIdx, insertChar);
         }

         Undos.Add(new InsertCharUndo(Selection.StartParagraph, Selection.StartParagraph.Inlines.IndexOf(startInline!), insertIdx, this, Selection.Start));
         UpdateTextRanges(Selection.Start, 1);


         Selection.StartParagraph.RequestInlinesUpdate = true;
         UpdateBlockAndInlineStarts(Blocks.IndexOf(Selection.StartParagraph));

         MoveSelectionRight(false);


      }

   }

   internal void DeleteChar()
   {
      if (Selection.Start >= Selection.StartParagraph.StartInDoc + Selection.StartParagraph.BlockLength)
         return;

      Selection!.BiasForward = true;

      Paragraph startP = (Paragraph)Selection.StartParagraph;

      if (startP.SelectionStartInBlock == startP.Text.Length) 
         MergeParagraphForward(startP, true);
      else
      {
         IEditable startInline = Selection.GetStartInline();
         int selectionStartInInline = Selection.GetStartInline().GetRangeStartInInline(Selection);
         string deletedChar = startInline.InlineText.Substring(selectionStartInInline, 1);
         bool saveEmptyInline = false;
         int startInlineIdx = startP.Inlines.IndexOf(startInline);

         if (startInline.GetType() == typeof(EditableInlineUIContainer) || startInline.GetType() == typeof(EditableLineBreak))
         {
            startP.Inlines.Remove(startInline);
            saveEmptyInline = true;
         }
         else
         {
            startInline.InlineText = startInline.InlineText.Remove(selectionStartInInline, 1);

            if (startP.Text != ""  && startInline.InlineText == "")
            {
               startP.Inlines.Remove(startInline);
               saveEmptyInline = true;
            }
         }

         Undos.Add(new DeleteCharUndo(startP, startInline, startInlineIdx, deletedChar, selectionStartInInline, this, Selection.Start, saveEmptyInline));

         UpdateTextRanges(Selection.Start, -1);

         UpdateBlockAndInlineStarts(Blocks.IndexOf(startP));
      }

      SelectionStart_Changed(Selection, Selection.Start);
      Selection.StartParagraph.RequestInlinesUpdate = true;
      Selection.StartParagraph.UpdateTextLayoutInfoStart();

   }

   private Dictionary<Block, List<IEditable>> KeepParsAndInlines (TextRange tRange)
   {
      Dictionary<Block, List<IEditable>> returnDict = [];
      
      List<Block> allBlocks = GetRangeBlocks(tRange);
      foreach (Block b in allBlocks)
      {
         List<IEditable> inlines = [];
         if (b.IsParagraph)
            inlines.AddRange(((Paragraph)b).Inlines.ToList().ConvertAll(il => il.Clone()));
         returnDict.TryAdd(b, inlines);
      }

      return returnDict;

   }

   internal void SetText(TextRange tRange, string newText)
   {  //The delete range and SetText should constitute one Undo operation

      Paragraph startPar = tRange.StartParagraph;
      int rangeStart = tRange.Start;
      int deleteRangeLength = tRange.Length;
      int parIndex = Blocks.IndexOf(startPar); 

      Undos.Add(new PasteUndo(KeepParsAndInlines(tRange), parIndex, this, rangeStart, deleteRangeLength - newText.Length));

      //Delete any selected text first
      if (tRange.Length > 0)
      {
         DeleteRange(tRange, false); 
         tRange.CollapseToStart(); 
         SelectionExtendMode = ExtendMode.ExtendModeNone;
      }

      IEditable startInline = tRange.GetStartInline();
      List<IEditable> splitInlines = SplitRunAtPos(tRange, startInline, startInline.GetRangeStartInInline(tRange));
      
      int startInlineIndex = startPar.Inlines.IndexOf(splitInlines[0]) + 1;

      EditableRun? sRun = splitInlines[0] as EditableRun;
      EditableRun newEditableRun = new EditableRun(newText)
      {
         FontFamily = sRun!.FontFamily,
         FontWeight = sRun.FontWeight,
         FontStyle = sRun.FontStyle,
         FontSize = sRun.FontSize,
         TextDecorations = sRun.TextDecorations,
         Background = sRun.Background
      };

      startPar.Inlines.Insert(startInlineIndex, newEditableRun);

      if (splitInlines[0].InlineText == "")
         startPar.Inlines.Remove(splitInlines[0]);

      startPar.RequestInlinesUpdate = true;
      UpdateBlockAndInlineStarts(startPar);

      tRange.End = rangeStart + newText.Length;
      tRange.CollapseToEnd();
      tRange.BiasForward = false;

      if (tRange.Equals(Selection))
         UpdateSelection();

      UpdateTextRanges(rangeStart, newText.Length);

   }

   internal void DeleteSelection()
   {
      DeleteRange(Selection, true);
      SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeNone;
      UpdateBlockAndInlineStarts(Selection.StartParagraph);
      Selection.CollapseToStart();
      Selection.BiasForward = false;  

   }

   internal void DeleteRange(TextRange trange, bool saveUndo)
   {
      int originalSelectionStart = Selection.Start;
      int originalTRangeLength = trange.Length;

      Dictionary<Block, List<IEditable>> keepParsAndInlines = KeepParsAndInlines(trange);
      List<Block> allBlocks = keepParsAndInlines.ToList().ConvertAll(keepP => keepP.Key);

      if (saveUndo) 
         Undos.Add(new DeleteRangeUndo(keepParsAndInlines, Blocks.IndexOf(allBlocks[0]), this, originalSelectionStart, originalTRangeLength));

      List<IEditable> rangeInlines = CreateNewInlinesForRange(trange);

      //Delete the created inlines
      foreach (IEditable toDeleteRun in rangeInlines)
         foreach (Block b in allBlocks)
         {
            if (b.IsParagraph)
            {
               Paragraph p = (Paragraph)b;
               if (p.Inlines.Contains(toDeleteRun))
                  p.Inlines.Remove(toDeleteRun);
               p.RequestInlinesUpdate = true;
            }
         }

      //Delete any full blocks contained within the range
      int idxStartPar = Blocks.IndexOf(allBlocks[0]);
      for (int i = idxStartPar + allBlocks.Count - 2; i > idxStartPar; i--)
      {
         if (Blocks[i].IsParagraph)
         {
            ((Paragraph)Blocks[i]).Inlines.Clear();
            ((Paragraph)Blocks[i]).RequestInlinesUpdate = true;
         }
         Blocks.RemoveAt(i);
      }

      //Add a blank run if all runs were deleted in one paragraph
      if (allBlocks.Count == 1 && ((Paragraph)allBlocks[0]).Inlines.Count == 0)
         ((Paragraph)allBlocks[0]).Inlines.Add(new EditableRun(""));

      //Merge inlines of last paragraph with first
      if (allBlocks.Count > 1)
      {
         Paragraph? lastPar = allBlocks[^1] as Paragraph;
         List<IEditable> moveInlines = new(lastPar!.Inlines);
         lastPar.Inlines.RemoveMany(moveInlines);
         lastPar.RequestInlinesUpdate = true;
         ((Paragraph)Blocks[idxStartPar]).Inlines.AddRange(moveInlines);
         ((Paragraph)Blocks[idxStartPar]).RequestInlinesUpdate = true; // ensure any image containers are updated
         Blocks.Remove(lastPar);
      }

      //Special case where all content was deleted leaving one empty block
      if (Blocks.Count == 1 && ((Paragraph)Blocks[0]).Inlines.Count == 0)
          ((Paragraph)Blocks[0]).Inlines.Add(new EditableRun(""));


      UpdateTextRanges(originalSelectionStart, -originalTRangeLength);

      UpdateSelection();
     

   }


   internal void InsertParagraph(bool addUndo)
   {  //The delete range and InsertParagraph should constitute one Undo operation

      Paragraph startPar = Selection.StartParagraph;
      int originalSelStart = Selection.Start;
      int selectionLength = Selection.Length;
      int parIndex = Blocks.IndexOf(Selection.StartParagraph);

      if (addUndo)
         //Undos.Add(new InsertParagraphUndo(Blocks.IndexOf(startPar), startPar.Inlines.ToList().ConvertAll(iline => iline.Clone()), this, originalSelStart, selectionLength - 1));
         Undos.Add(new InsertParagraphUndo(KeepParsAndInlines(Selection), parIndex, this, originalSelStart, selectionLength - 1));

      if (Selection.Length > 0)
      {
         DeleteRange(Selection, false);
         Selection.CollapseToStart();
         SelectionExtendMode = ExtendMode.ExtendModeNone;
      }

      IEditable startInline = Selection.GetStartInline();
      int StartRunIdx = startPar.Inlines.IndexOf(startInline);
      List<IEditable> parSplitRuns = SplitRunAtPos(Selection, startInline, startInline.GetRangeStartInInline(Selection));

      List<IEditable> OriginalParInlines = startPar.Inlines.ToList().ConvertAll(il => il.Clone());

      List<IEditable> RunList1 = new(startPar.Inlines.Take(new Range(0, StartRunIdx)).ToList().ConvertAll(r => r as IEditable));
      RunList1.Add(parSplitRuns[0]);
      List<IEditable> RunList2 = new(startPar.Inlines.Take(new Range(StartRunIdx + 1, startPar.Inlines.Count)).ToList().ConvertAll(r => r as IEditable));

      Paragraph? originalPar = startPar;
      
      originalPar.Inlines.Clear();
      originalPar.Inlines.AddRange(RunList1);
      originalPar.SelectionStartInBlock = 0;
      originalPar.CollapseToStart();

      Paragraph newPar = originalPar.Clone();
      newPar.Inlines.AddRange(RunList2);
      Blocks.Insert(parIndex + 1, newPar);

      if (originalPar.Inlines.Count > 1)
      {
         if (originalPar.Inlines.Last().InlineText == "")
            originalPar.Inlines.Remove(originalPar.Inlines.Last());
      }

      if (newPar.Inlines.Count == 0)
      {
         EditableRun erun = (EditableRun)originalPar.Inlines.Last().Clone();
         erun.Text = "";
         newPar.Inlines.Add(erun);
      }

      
      UpdateTextRanges(Selection.Start, 1);

      UpdateBlockAndInlineStarts(parIndex);
      originalPar.RequestInlinesUpdate = true;
      newPar.RequestInlinesUpdate = true;

      Selection.BiasForward = true;

      Selection.End += 1;
      Selection.CollapseToEnd();
      SelectionExtendMode = ExtendMode.ExtendModeNone;

      originalPar.UpdateTextLayoutInfoStart();
      newPar.UpdateTextLayoutInfoStart();
      originalPar.UpdateTextLayoutInfoEnd();
      newPar.UpdateTextLayoutInfoEnd();

      ScrollInDirection!(1);


   }

   internal void MergeParagraphForward(Paragraph thisPar, bool saveUndo)
   {
      int etbIndex = Blocks.IndexOf(thisPar);
      if (etbIndex == Blocks.Count - 1) return; //is last Paragraph, can't merge forward

      Paragraph nextPar = (Paragraph)Blocks[etbIndex + 1];
      
      List <IEditable> inlinesToMove = new(nextPar.Inlines);
      nextPar.Inlines.Clear();
      nextPar.RequestInlinesUpdate = true; // ensure image containers are updated
      Blocks.Remove(nextPar);
      thisPar.Inlines.AddRange(inlinesToMove);
      Selection!.BiasForward = true;

      if (thisPar.Inlines.Count > 1)
         RemoveEmptyInlines(new List<int>() { etbIndex });

      if (saveUndo)
         Undos.Add(new MergeParagraphUndo(Selection.Start, this));

      UpdateTextRanges(Selection.Start, -1);

      thisPar.RequestInlinesUpdate = true;
      UpdateBlockAndInlineStarts(etbIndex);

      thisPar.RequestTextBoxFocus = true;

   }

   internal void DeleteWord()
   {

      if (Selection.Start >= Selection.StartParagraph.StartInDoc + Selection.StartParagraph.BlockLength)
         return;

      Selection!.BiasForward = true;

      Paragraph startP = (Paragraph)Selection.StartParagraph;

      if (startP.SelectionStartInBlock == startP.Text.Length)
         MergeParagraphForward(startP, true); //updates text ranges and adds undo
      else
      {
         int NextWordEndPoint = -1;
         IEditable startInline = Selection.GetStartInline();
         if (startInline.IsUIContainer || startInline.IsLineBreak)
            NextWordEndPoint = Selection.Start + 1;
         else
         {
            int IndexNextSpace = Selection.StartParagraph.Text.IndexOf(" ", Selection.Start - Selection.StartParagraph.StartInDoc);
            if (IndexNextSpace == -1)
               IndexNextSpace = Selection.StartParagraph.Text.Length;
            else
               IndexNextSpace += 1;
            NextWordEndPoint = Selection.StartParagraph.StartInDoc + IndexNextSpace;
         }

         if (startP.Inlines.Count > 1)
            //RemoveEmptyInlines(new List<Paragraph>() { Selection.StartParagraph });
            RemoveEmptyInlines(new List<int>() { Blocks.IndexOf(Selection.StartParagraph) });
         
         TextRange deleteTextRange = new TextRange(this, Selection.Start, NextWordEndPoint);
         DeleteRange(deleteTextRange, true);  // updates all text ranges and adds undo
                           
         UpdateBlockAndInlineStarts(Blocks.IndexOf(startP));
      }

      SelectionStart_Changed(Selection, Selection.Start);
      Selection.StartParagraph.RequestInlinesUpdate = true;
      Selection.StartParagraph.UpdateTextLayoutInfoStart();

   }

   internal void BackWord()
   {
      if (Selection.Start <= 0) return;
      MoveLeftWord();
      DeleteWord();

   }

   internal void Undo()
   {
      if (Undos.Count > 0)
      {
         Undos.Last().PerformUndo();

         UpdateSelection();

         if (Undos.Last().UpdateTextRanges)
            UpdateTextRanges(Selection.Start, Undos.Last().UndoEditOffset);

         Undos.RemoveAt(Undos.Count - 1);

         UpdateSelectionParagraphs();

         ScrollInDirection!(1);
         ScrollInDirection!(-1);
      }
   }

   internal void RestoreDeletedBlocks(Dictionary<Block, List<IEditable>> parsAndInlines, int blockIndex)
   {
      //Reset all paragraphs with new inlines exactly as before

      foreach (KeyValuePair<Block, List<IEditable>> restorePar in parsAndInlines)
      {
         if (restorePar.Key.IsParagraph)
         {
            Paragraph? p = restorePar.Key as Paragraph;
            p!.Inlines.Clear();
            p.RequestInlinesUpdate = true;
            p.Inlines.AddRange(restorePar.Value);
         }
      }

      //Restore all of the previous paragraphs            
      Blocks.RemoveAt(blockIndex);
      Blocks.AddOrInsertRange(parsAndInlines.ToList().ConvertAll(pil=>pil.Key), blockIndex);

      foreach (KeyValuePair<Block, List<IEditable>> restorePar in parsAndInlines)
         if (restorePar.Key.IsParagraph)
         {
            Paragraph? p = restorePar.Key as Paragraph;
            p!.RequestInlinesUpdate = true;
            p.ClearSelection();
         }

      UpdateBlockAndInlineStarts(blockIndex);

   }


}