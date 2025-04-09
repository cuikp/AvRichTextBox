using DynamicData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

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
            insertIdx = startInline.GetCharPosInInline(Selection.Start); 
            startInline.InlineText = startInline.InlineText!.Insert(insertIdx, insertText);
         }

         Undos.Add(new InsertCharUndo(Blocks.IndexOf(Selection.StartParagraph), Selection.StartParagraph.Inlines.IndexOf(startInline!), insertIdx, this, Selection.Start));
         UpdateTextRanges(Selection.Start, insertText.Length);


         Selection.StartParagraph.CallRequestInlinesUpdate();
         UpdateBlockAndInlineStarts(Selection.StartParagraph);

         for (int i = 0; i < insertText.Length; i++) 
            MoveSelectionRight(true);

         IEditable nextInline = GetNextInline(Selection.GetStartInline())!;
         Selection.IsAtLineBreak = nextInline != null && nextInline.IsLineBreak;

      }

   }

   internal void DeleteChar(bool backspace)
   {
      int originalSelectionStart = Selection.Start;

      if (backspace)
         MoveSelectionLeft(true);

      Selection!.BiasForwardStart = true;
      Selection!.BiasForwardEnd = true;

      IEditable startInline = Selection.GetStartInline();
      if (startInline == null) return;

      Paragraph startP = (Paragraph)Selection.StartParagraph;

      if (startP.SelectionStartInBlock == startP.Text.Length)
         MergeParagraphForward(Selection.Start, true, originalSelectionStart);
      else
      {  //Delete one unit
         int startInlineIdx = startP.Inlines.IndexOf(startInline);
         string deletedChar = "";
         int selectionStartInInline = 0;

         if (startInline is EditableInlineUIContainer eIUC)
         {
            bool emptyRunAdded = false;
            if (startP.Inlines.Count == 1)
            {
               startP.Inlines.Add(new EditableRun(""));
               emptyRunAdded = true;
            }
               
            Undos.Add(new DeleteImageUndo(Blocks.IndexOf(startP), eIUC, startInlineIdx, this, originalSelectionStart, emptyRunAdded));

            startP.Inlines.Remove(eIUC);
         }
         else
         {
            IEditable? nextInline = GetNextInline(startInline);
            bool isSelectionAtInlineEnd = startInline.GetCharPosInInline(Selection.End) == startInline.InlineLength;
            IEditable keepInline = null!;

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
               if (startInline.InlineLength == 1)
               {
                  keepInline = startInline;
                  startP.Inlines.Remove(startInline);
               }
               else
               {
                  selectionStartInInline = startInline.GetCharPosInInline(Selection.Start);
                  deletedChar = startInline.InlineText.Substring(selectionStartInInline, 1);
                  startInline.InlineText = startInline.InlineText.Remove(selectionStartInInline, 1);
               }
               
               //Paragraph must always have at least an empty run
               if (startP.Inlines.Count == 0)
                  startP.Inlines.Add(new EditableRun(""));
            }

            Undos.Add(new DeleteCharUndo(Blocks.IndexOf(startP), startInlineIdx, keepInline, deletedChar, selectionStartInInline, this, originalSelectionStart));

         }

         UpdateTextRanges(Selection.Start, -1);

         UpdateBlockAndInlineStarts(Blocks.IndexOf(startP));
      }

      SelectionStart_Changed(Selection, Selection.Start);
      Selection.StartParagraph.CallRequestInlinesUpdate();
      Selection.StartParagraph.CallRequestTextLayoutInfoStart();



   }

   internal void InsertLineBreak()
   {
      Paragraph startPar = Selection.StartParagraph;
      IEditable startInline = Selection.GetStartInline();
      int runIdx = startPar.Inlines.IndexOf(startInline);

      SplitRunAtPos(Selection.Start, startInline, startInline.GetCharPosInInline(Selection.Start)); // creates an empty inline

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

   internal void InsertParagraph(bool addUndo, int insertCharIndex)
   {  //The delete range and InsertParagraph should constitute one Undo operation

      Paragraph insertPar = GetContainingParagraph(insertCharIndex);
      List<IEditable> keepParInlines = insertPar.Inlines.Select(il=>il.Clone()).ToList(); 

      int originalSelStart = insertCharIndex;
      int parIndex = Blocks.IndexOf(insertPar);
      int selectionLength = 0;

      if (addUndo)
      {
         selectionLength = Selection.Length;
         if (Selection.Length > 0)
         {
            DeleteRange(Selection, false);
            Selection.CollapseToStart();
            SelectionExtendMode = ExtendMode.ExtendModeNone;
         }
      }

      IEditable startInline = GetStartInline(insertCharIndex);
      int StartRunIdx = insertPar.Inlines.IndexOf(startInline);

      //Split at selection
      List<IEditable> parSplitRuns = SplitRunAtPos(insertCharIndex, startInline, startInline.GetCharPosInInline(insertCharIndex));


      List<IEditable> RunList1 = new(insertPar.Inlines.Take(new Range(0, StartRunIdx)).ToList().ConvertAll(r => r));
      if (parSplitRuns[0].InlineText != "" || RunList1.Count == 0)
         RunList1.Add(parSplitRuns[0]);
      List<IEditable> RunList2 = new(insertPar.Inlines.Take(new Range(StartRunIdx + 1, insertPar.Inlines.Count)).ToList().ConvertAll(r => r as IEditable));
      
      Paragraph? originalPar = insertPar;
      
      originalPar.Inlines.Clear();
      originalPar.Inlines.AddRange(RunList1);
      originalPar.SelectionStartInBlock = 0;
      originalPar.CollapseToStart();

      Paragraph parToInsert = originalPar.PropertyClone();

      parToInsert.Inlines.AddRange(RunList2);
      Blocks.Insert(parIndex + 1, parToInsert);

      if (parToInsert.Inlines.Count == 0)
      {
         EditableRun erun = (EditableRun)originalPar.Inlines.Last().Clone();
         erun.Text = "";
         parToInsert.Inlines.Add(erun);
      }
      
      UpdateTextRanges(insertCharIndex, 1);

      UpdateBlockAndInlineStarts(parIndex);
      originalPar.CallRequestInlinesUpdate();
      parToInsert.CallRequestInlinesUpdate();


      if (addUndo)
         //Undos.Add(new InsertParagraphUndo(this, insertCharIndex, originalSelStart, selectionLength - 1));
         Undos.Add(new InsertParagraphUndo(this, parIndex, keepParInlines, originalSelStart, selectionLength - 1));

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;
      Selection.End += 1;
      Selection.CollapseToEnd();

      originalPar.CallRequestTextLayoutInfoStart();
      parToInsert.CallRequestTextLayoutInfoStart();
      originalPar.CallRequestTextLayoutInfoEnd();
      parToInsert.CallRequestTextLayoutInfoEnd();

      ScrollInDirection!(1);

   }

   internal void MergeParagraphForward(int mergeCharIndex, bool saveUndo, int originalSelectionStart)
   {
      Paragraph thisPar = GetContainingParagraph(mergeCharIndex);
      int thisParIndex = Blocks.IndexOf(thisPar);
      if (thisParIndex == Blocks.Count - 1) return; //is last Paragraph, can't merge forward
      int origParInlinesCount = thisPar.Inlines.Count;

      Paragraph nextPar = (Paragraph)Blocks[thisParIndex + 1];
      bool IsNextParagraphEmpty = nextPar.Inlines.Count == 1 && nextPar.Inlines[0].IsEmpty;
      bool IsThisParagraphEmpty = thisPar.Inlines.Count == 1 && thisPar.Inlines[0].IsEmpty;

      if (saveUndo)
         Undos.Add(new MergeParagraphUndo(origParInlinesCount, thisParIndex, nextPar.FullClone(), this, originalSelectionStart));

      if (IsThisParagraphEmpty)
         thisPar.Inlines.Clear();

      //bool runAdded = false;
      if (IsNextParagraphEmpty)
      {
         if (IsThisParagraphEmpty)
         {
            thisPar.Inlines.Add(new EditableRun(""));
            //runAdded = true;
         }
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

      UpdateTextRanges(mergeCharIndex, -1);

      thisPar.CallRequestInlinesUpdate();
      UpdateBlockAndInlineStarts(thisParIndex);

      thisPar.CallRequestTextBoxFocus();

#if DEBUG
      UpdateDebuggerSelectionParagraphs();
#endif



   }

   internal void DeleteWord(bool backspace)
   {
      if (backspace)
         if (Selection.Start <= 0) return;
      else
         if (Selection.Start >= Selection.StartParagraph.StartInDoc + Selection.StartParagraph.BlockLength)
            return;

      
      int originalSelectionStart = Selection.Start;
      
      if (backspace)
         MoveLeftWord();
      
      Selection!.BiasForwardStart = true;
      Selection!.BiasForwardEnd = true;

      Paragraph startP = Selection.StartParagraph;

      if (startP.SelectionStartInBlock == startP.Text.Length)
         MergeParagraphForward(Selection.Start, true, originalSelectionStart); //updates text ranges and adds undo
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


}