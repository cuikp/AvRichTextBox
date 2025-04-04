using Avalonia.Controls.Documents;
using DocumentFormat.OpenXml.InkML;
using DynamicData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AvRichTextBox;

public partial class FlowDocument
{
   internal void InsertText(string? insertText)
   {
      IEditable startInline = Selection.GetStartInline();

      if (startInline == null) return;

      if (startInline.GetType() == typeof(EditableInlineUIContainer))
         return;
      
      if (insertText != null)
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
            startInline.InlineText = insertText;
            toggleFormatRun!(startInline);
            InsertRunMode = false;
         }
         else
         {  //Debug.WriteLine("starinlinetext = " + startInline.InlineText);
            insertIdx = startInline.GetRangeStartInInline(Selection); 
            startInline.InlineText = startInline.InlineText!.Insert(insertIdx, insertText);
         }

         Undos.Add(new InsertCharUndo(Selection.StartParagraph, Selection.StartParagraph.Inlines.IndexOf(startInline!), insertIdx, this, Selection.Start));
         UpdateTextRanges(Selection.Start, insertText.Length);


         Selection.StartParagraph.CallRequestInlinesUpdate();
         UpdateBlockAndInlineStarts(Selection.StartParagraph);

         for (int i = 0; i < insertText.Length; i++) 
            MoveSelectionRight(true);

         IEditable nextInline = GetNextInline(Selection.GetStartInline())!;
         Selection.IsAtLineBreak = nextInline != null && nextInline.IsLineBreak;

      }

   }

   internal void BackspaceChar()
   {
      int origStart = Selection.Start;

      if (Selection!.Length > 0)
         DeleteSelection();
      else
      {
         if (Selection.Start == 0) return;

         if (Selection.StartParagraph.SelectionStartInBlock == 0)
         { //at start of paragraph 
            MoveSelectionLeft(true);
            MergeParagraphForward(Selection.StartParagraph, true);
         }
         else
         {
            MoveSelectionLeft(true);
            DeleteChar(origStart);
         }
      }

   }


   internal void DeleteChar(int originalSelectionStart)
   {
      if (Selection.Start >= Selection.StartParagraph.StartInDoc + Selection.StartParagraph.BlockLength)
         return;

      Selection!.BiasForwardStart = true;
      Selection!.BiasForwardEnd = true;

      IEditable startInline = Selection.GetStartInline();
      if (startInline == null) return;

      Paragraph startP = (Paragraph)Selection.StartParagraph;

      if (startP.SelectionStartInBlock == startP.Text.Length)
         MergeParagraphForward(startP, true);
      else
      {  //Delete one unit
         int startInlineIdx = startP.Inlines.IndexOf(startInline);
         string deletedChar = "";
         int selectionStartInInline = 0;

         if (startInline.GetType() == typeof(EditableInlineUIContainer))
         {
            IEditable? nextInline = GetNextInline(startInline);
            if (nextInline != null)
            {
               startP.Inlines.Remove(nextInline);
               deletedChar = " ";
            }
         }
         else 
         {
            IEditable? nextInline = GetNextInline(startInline);
            bool isSelectionAtInlineEnd = startInline.GetRangeEndInInline(Selection) == startInline.InlineLength;

            if (nextInline != null && nextInline.IsLineBreak && isSelectionAtInlineEnd)
            {  //Delete linebreak
               IEditable? lbnext = GetNextInline(nextInline);
               startP.Inlines.Remove(nextInline);
               if (lbnext != null && lbnext.IsEmpty)
                  startP.Inlines.Remove(lbnext);
               else if (startInline.IsEmpty)
                  startP.Inlines.Remove(startInline);
            }
            else
            {  // delete normal run char
               selectionStartInInline = Selection.GetStartInline().GetRangeStartInInline(Selection);
               deletedChar = startInline.InlineText.Substring(selectionStartInInline, 1);
               startInline.InlineText = startInline.InlineText.Remove(selectionStartInInline, 1);

               IEditable previed = GetPreviousInline(startInline);
               bool isSelectionAtInlineStart = startInline.GetRangeStartInInline(Selection) == 0;
               if (previed != null && !previed.IsLineBreak && isSelectionAtInlineStart && startInline.IsEmpty)
               {
                  startP.Inlines.Remove(startInline);
               }
               if (startP.Inlines.Count == 0)
                  startP.Inlines.Add(new EditableRun(""));
            }
         } 
         
         //Undos.Add(new DeleteCharUndo(startP, startInline, startInlineIdx, deletedChar, selectionStartInInline, this, Selection.Start, saveEmptyInline));
         Undos.Add(new DeleteCharUndo(startP, startInline, startInlineIdx, deletedChar, selectionStartInInline, this, originalSelectionStart, false));

         UpdateTextRanges(Selection.Start, -1);

         UpdateBlockAndInlineStarts(Blocks.IndexOf(startP));
      }

      SelectionStart_Changed(Selection, Selection.Start);
      Selection.StartParagraph.CallRequestInlinesUpdate();
      Selection.StartParagraph.CallRequestTextLayoutInfoStart();

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

   internal int SetRangeToInlines(TextRange tRange, List<IEditable> newInlines)
   {  // All of this should constitute one Undo operation
      //Debug.WriteLine("newinlines=\n" + string.Join("\n", newInlines.ConvertAll(il => il.InlineText)));

      int addedCharCount = 0;

      Paragraph startPar = tRange.StartParagraph;

      int rangeStart = tRange.Start;
      int deleteRangeLength = tRange.Length;
      int parIndex = Blocks.IndexOf(startPar);

      //Undos.Add(new PasteUndo(KeepParsAndInlines(tRange), parIndex, this, rangeStart, deleteRangeLength - newText.Length));

      //Delete selected range first
      if (tRange.Length > 0)
         DeleteRange(tRange, false);

      IEditable startInline = tRange.GetStartInline();
      List<IEditable> splitInlines = SplitRunAtPos(tRange, startInline, startInline.GetRangeStartInInline(tRange));

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
      List<IEditable> splitInlines = SplitRunAtPos(tRange, startInline, startInline.GetRangeStartInInline(tRange));

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

   internal void InsertLineBreak()
   {
      Paragraph startPar = Selection.StartParagraph;
      IEditable startInline = Selection.GetStartInline();
      int runIdx = startPar.Inlines.IndexOf(startInline);

      SplitRunAtPos(Selection, startInline, startInline.GetRangeStartInInline(Selection)); // creates an empty inline

      startPar.Inlines.Insert(runIdx + 1, new EditableLineBreak());

      SelectionExtendMode = ExtendMode.ExtendModeNone;

      startPar.UpdateEditableRunPositions();
      startPar.CallRequestInlinesUpdate();
      startPar.CallRequestTextLayoutInfoStart();
      startPar.CallRequestTextLayoutInfoEnd();

      Select(Selection.Start + 2, 0);
      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

      ScrollInDirection!(1);

   }


   internal void DeleteSelection()
   {
      DeleteRange(Selection, true);
      SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeNone;
      UpdateBlockAndInlineStarts(Selection.StartParagraph);
      Selection.CollapseToStart();
      Selection.BiasForwardStart = false;  
      Selection.BiasForwardEnd = false;  

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
               p.Inlines.Remove(toDeleteRun);
               p.CallRequestInlinesUpdate();
            }
         }

      //Delete any full blocks contained within the range
      int idxStartPar = Blocks.IndexOf(allBlocks[0]);
      for (int i = idxStartPar + allBlocks.Count - 2; i > idxStartPar; i--)
      {
         if (Blocks[i].IsParagraph)
         {
            ((Paragraph)Blocks[i]).Inlines.Clear();
            ((Paragraph)Blocks[i]).CallRequestInlinesUpdate();
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
         lastPar.CallRequestInlinesUpdate();
         ((Paragraph)Blocks[idxStartPar]).Inlines.AddRange(moveInlines);
         ((Paragraph)Blocks[idxStartPar]).CallRequestInlinesUpdate(); // ensure any image containers are updated
         Blocks.Remove(lastPar);
      }

      //Special case where all content was deleted leaving one empty block
      if (Blocks.Count == 1 && ((Paragraph)Blocks[0]).Inlines.Count == 0)
          ((Paragraph)Blocks[0]).Inlines.Add(new EditableRun(""));


      UpdateTextRanges(originalSelectionStart, -originalTRangeLength);


      UpdateSelection();

      trange.CollapseToStart();
      SelectionExtendMode = ExtendMode.ExtendModeNone;


   }


   internal Paragraph InsertParagraph(bool addUndo)
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

      List<IEditable> RunList1 = new(startPar.Inlines.Take(new Range(0, StartRunIdx)).ToList().ConvertAll(r => r as IEditable))
      {
         parSplitRuns[0]
      };
      List<IEditable> RunList2 = new(startPar.Inlines.Take(new Range(StartRunIdx + 1, startPar.Inlines.Count)).ToList().ConvertAll(r => r as IEditable));

      Paragraph? originalPar = startPar;
      
      originalPar.Inlines.Clear();
      originalPar.Inlines.AddRange(RunList1);
      originalPar.SelectionStartInBlock = 0;
      originalPar.CollapseToStart();

      Paragraph newPar = originalPar.Clone();
      newPar.Inlines.AddRange(RunList2);
      Blocks.Insert(parIndex + 1, newPar);

      if (newPar.Inlines.Count == 0)
      {
         EditableRun erun = (EditableRun)originalPar.Inlines.Last().Clone();
         erun.Text = "";
         newPar.Inlines.Add(erun);
      }
      
      UpdateTextRanges(Selection.Start, 1);

      UpdateBlockAndInlineStarts(parIndex);
      originalPar.CallRequestInlinesUpdate();
      newPar.CallRequestInlinesUpdate();

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

      Selection.End += 1;
      
      Selection.CollapseToEnd();
     

      SelectionExtendMode = ExtendMode.ExtendModeNone;

      originalPar.CallRequestTextLayoutInfoStart();
      newPar.CallRequestTextLayoutInfoStart();
      originalPar.CallRequestTextLayoutInfoEnd();
      newPar.CallRequestTextLayoutInfoEnd();

      ScrollInDirection!(1);

      return newPar;

   }

   internal void MergeParagraphForward(Paragraph thisPar, bool saveUndo)
   {
      int etbIndex = Blocks.IndexOf(thisPar);
      if (etbIndex == Blocks.Count - 1) return; //is last Paragraph, can't merge forward

      Paragraph nextPar = (Paragraph)Blocks[etbIndex + 1];
      bool IsNextParagraphEmpty = nextPar.Inlines.Count == 1 && nextPar.Inlines[0].IsEmpty;
      bool IsThisParagraphEmpty = thisPar.Inlines.Count == 1 && thisPar.Inlines[0].IsEmpty;
      
      if (IsThisParagraphEmpty)
         thisPar.Inlines.Clear();

      if (IsNextParagraphEmpty)
      {
         if (IsThisParagraphEmpty)
            thisPar.Inlines.Add(new EditableRun(""));
      }
      else
      {
         List<IEditable> inlinesToMove = new(nextPar.Inlines);
         nextPar.Inlines.Clear();
         nextPar.CallRequestInlinesUpdate(); // ensure image containers are updated
         thisPar.Inlines.AddRange(inlinesToMove);
      }

      Blocks.Remove(nextPar);

      Selection!.BiasForwardStart = true;
      Selection!.BiasForwardEnd = true;

      if (saveUndo)
         Undos.Add(new MergeParagraphUndo(Selection.Start, this));

      UpdateTextRanges(Selection.Start, -1);

      thisPar.CallRequestInlinesUpdate();
      UpdateBlockAndInlineStarts(etbIndex);

      thisPar.CallRequestTextBoxFocus();

#if DEBUG
      UpdateDebuggerSelectionParagraphs();
#endif



   }

   internal void DeleteWord()
   {

      if (Selection.Start >= Selection.StartParagraph.StartInDoc + Selection.StartParagraph.BlockLength)
         return;

      Selection!.BiasForwardStart = true;
      Selection!.BiasForwardEnd = true;

      Paragraph startP = Selection.StartParagraph;

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
            int IndexNextSpace = Selection.StartParagraph.Text.IndexOf(' ', Selection.Start - Selection.StartParagraph.StartInDoc);
            if (IndexNextSpace == -1)
               IndexNextSpace = Selection.StartParagraph.Text.Length;
            else
               IndexNextSpace += 1;
            NextWordEndPoint = Selection.StartParagraph.StartInDoc + IndexNextSpace;
         }

         //if (startP.Inlines.Count > 1)
         //   startP.RemoveEmptyInlines();
         
         TextRange deleteTextRange = new (this, Selection.Start, NextWordEndPoint);
         DeleteRange(deleteTextRange, true);  // updates all text ranges and adds undo
                           
         UpdateBlockAndInlineStarts(Blocks.IndexOf(startP));
      }

      SelectionStart_Changed(Selection, Selection.Start);
      Selection.StartParagraph.CallRequestInlinesUpdate();
      Selection.StartParagraph.CallRequestTextLayoutInfoStart();

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
         if (restorePar.Key.IsParagraph)
         {
            Paragraph? p = restorePar.Key as Paragraph;
            p!.Inlines.Clear();
            p.CallRequestInlinesUpdate();
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
            p!.CallRequestInlinesUpdate();
            p.ClearSelection();
         }

      UpdateBlockAndInlineStarts(blockIndex);

   }

   [GeneratedRegex(@"[\r\n]")]
   internal static partial Regex FindLineBreakCharsRegex();
}