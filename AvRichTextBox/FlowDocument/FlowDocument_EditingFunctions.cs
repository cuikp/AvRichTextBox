using DynamicData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace AvRichTextBox;

public partial class FlowDocument
{

   private Dictionary<Block, List<IEditable>> KeepParsAndInlines(TextRange tRange)
   {
      Dictionary<Block, List<IEditable>> returnDict = [];

      List<Block> allBlocks = GetRangeBlocks(tRange);
      foreach (Block b in allBlocks)
      {
         List<IEditable> inlines = [];
         if (b is Paragraph p)
            inlines.AddRange(p.Inlines.ToList().ConvertAll(il => il.Clone()));
         returnDict.TryAdd(b, inlines);

      }

      return returnDict;

   }

   internal int SetRangeToInlines(TextRange tRange, List<IEditable> newInlines)
   {  // All of this should constitute one Undo operation
      //Debug.WriteLine("newinlines=\n" + string.Join("\n", newInlines.ConvertAll(il => il.InlineText)));

      int addedCharCount = 0;

      Paragraph startPar = tRange.StartParagraph;

      int rangeStart = tRange.Start;
      int deleteRangeLength = tRange.Length;
      int parIndex = Blocks.IndexOf(startPar);

      Undos.Add(new PasteUndo(KeepParsAndInlines(tRange), parIndex, this, rangeStart, deleteRangeLength - newInlines.Sum(nil=>nil.InlineLength)));

      //Delete selected range first
      if (tRange.Length > 0)
         DeleteRange(tRange, false);

      IEditable startInline = tRange.GetStartInline();
      List<IEditable> splitInlines = SplitRunAtPos(tRange.Start, startInline, startInline.GetCharPosInInline(tRange.Start));

      int insertionPt = startPar.Inlines.IndexOf(splitInlines[0]) + 1;

      int startInlineIndex = startPar.Inlines.IndexOf(splitInlines[0]) + 1;
      Paragraph addPar = startPar;
      int inlineno = 0;
      foreach (IEditable newinline in newInlines)
      {
         inlineno++;

         bool addnewpar = false;

         if (newinline.InlineText.EndsWith('\r'))
         {
            newinline.InlineText = newinline.InlineText[..^1];
            addnewpar = inlineno > 1;
            //Debug.WriteLine("addnew par? " + addnewpar +  " (" + newinline.InlineText + ")");
         }

         if (addnewpar)
         {
            List<IEditable> moveInlines = addPar.Inlines.Take(new Range(0, startInlineIndex)).ToList();  // create an independent new list
            addPar.Inlines.RemoveMany(moveInlines);

            //Create new paragraph to insert
            addPar = new Paragraph();
            addPar.Inlines.AddRange(moveInlines);
            startInlineIndex = addPar.Inlines.Count;
            Blocks.Insert(parIndex, addPar);
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

      return addedCharCount;

   }


   internal void SetRangeToText(TextRange tRange, string newText)
   {  //The delete range and SetRangeToText should constitute one Undo operation

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
      List<IEditable> splitInlines = SplitRunAtPos(tRange.Start, startInline, startInline.GetCharPosInInline(tRange.Start));

      int startInlineIndex = startPar.Inlines.IndexOf(splitInlines[0]) + 1;

      EditableRun? sRun = splitInlines[0] as EditableRun;
      EditableRun newEditableRun = new(newText)
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

      startPar.CallRequestInlinesUpdate();
      UpdateBlockAndInlineStarts(startPar);

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

         if (ShowDebugger)
            UpdateDebuggerSelectionParagraphs();

         ScrollInDirection!(1);
         ScrollInDirection!(-1);
      }
   }

   internal void RestoreDeletedBlocks(Dictionary<Block, List<IEditable>> parsAndInlines, int blockIndex)
   {
      //Reset all paragraphs with new inlines exactly as before

      foreach (KeyValuePair<Block, List<IEditable>> restorePar in parsAndInlines)
      {
         if (restorePar.Key is Paragraph p)
         {
            p.Inlines.Clear();
            p.CallRequestInlinesUpdate();
            p.Inlines.AddRange(restorePar.Value);
         }
      }

      //Restore all of the previous paragraphs            
      Blocks.RemoveAt(blockIndex);
      Blocks.AddOrInsertRange(parsAndInlines.ToList().ConvertAll(pil => pil.Key), blockIndex);

      foreach (KeyValuePair<Block, List<IEditable>> restorePar in parsAndInlines)
         if (restorePar.Key.IsParagraph)
         {
            Paragraph? p = restorePar.Key as Paragraph;
            p!.CallRequestInlinesUpdate();
            p.ClearSelection();
         }

      UpdateBlockAndInlineStarts(blockIndex);

   }

   [GeneratedRegex(@"[\r\n]")]
   internal static partial Regex FindLineBreakCharsRegex();


   internal IEditable GetStartInline(int charIndex)
   {

      Paragraph? startPar = Blocks.LastOrDefault(b => b.IsParagraph && (b.StartInDoc <= charIndex))! as Paragraph;

      if (startPar != null)
      {
         //Check if start at end of last paragraph (cannot span from end of a paragraph)
         if (startPar != Blocks.Where(b => b.IsParagraph).Last() && startPar!.EndInDoc == charIndex)
            startPar = Blocks.FirstOrDefault(b => b.IsParagraph && Blocks.IndexOf(b) > Blocks.IndexOf(startPar))! as Paragraph;
      }

      if (startPar == null) return null!;

      IEditable startInline = null!;
      bool IsAtLineBreak = false;

      IEditable startInlineReal = startPar.Inlines.LastOrDefault(ied => startPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= charIndex)!;
      startInline = startPar.Inlines.LastOrDefault(ied => !ied.IsLineBreak && startPar.StartInDoc + ied.TextPositionOfInlineInParagraph <= charIndex)!;
      IsAtLineBreak = startInline != startInlineReal;

      return startInline!;

   }


}