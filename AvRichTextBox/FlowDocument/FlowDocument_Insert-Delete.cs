using DynamicData;
using System.Diagnostics;

namespace AvRichTextBox;

public partial class FlowDocument
{
   internal void InsertText(string? insertText)
   {
      if (Selection.GetStartInline() is not IEditable startInline || startInline.GetType() == typeof(EditableInlineUIContainer)) return;

      if (insertText != null)
      {
         if (Selection.Length > 0)
         {
            DeleteRange(Selection, true);
            Selection.CollapseToStart();
            SelectionExtendMode = ExtendMode.ExtendModeNone;
            startInline = Selection.GetStartInline() ?? startInline;
         }

         int insertIdx = 0;
         if (InsertRunMode)
         {
            (int idLeft, int idRight) edgeIds;
            List<IEditable> applyInlines = GetRangeInlinesAndAddToDoc(Selection, out edgeIds);
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
            insertIdx = GetCharPosInInline(startInline, Selection.Start); 
            startInline.InlineText = startInline.InlineText.Insert(insertIdx, insertText);
         }

         Undos.Add(new InsertCharUndo(Selection.StartParagraph.Id, Selection.StartParagraph.Inlines.IndexOf(startInline!), insertIdx, this, Selection.Start));
         UpdateTextRanges(Selection.Start, insertText.Length);


         Selection.StartParagraph.CallRequestInlinesUpdate();
         UpdateBlockAndInlineStarts(Selection.StartParagraph);

         for (int i = 0; i < insertText.Length; i++) 
            MoveSelectionRight(true);

         IEditable? newStartInline = Selection.GetStartInline();
         IEditable? nextInline = newStartInline == null ? null : GetNextInline(newStartInline);
         Selection.IsAtLineBreak = nextInline != null && nextInline.IsLineBreak;

      }

   }

   internal void DeleteChar(bool backspace)
   {
      int originalSelectionStart = Selection.Start;

      if (backspace)
         MoveSelectionLeft(true);

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

      if (Selection.GetStartInline() is not IEditable startInline) return;

      Paragraph startP = (Paragraph)Selection.StartParagraph;

      if (startP.SelectionStartInBlock == startP.TextLength)
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
               
            Undos.Add(new DeleteImageUndo(startP.Id, eIUC, startInlineIdx, this, originalSelectionStart, emptyRunAdded));

            startP.Inlines.Remove(eIUC);
         }
         else
         {
            bool isSelectionAtInlineEnd = GetCharPosInInline(startInline, Selection.End) == startInline.InlineLength;

            if (GetNextInline(startInline) is EditableLineBreak lbreak && isSelectionAtInlineEnd)
            {  //Delete linebreak
               IEditable? lbnext = GetNextInline(lbreak);
               startP.Inlines.Remove(lbreak);
               if (lbnext != null && lbnext.IsEmpty)
                  startP.Inlines.Remove(lbnext);
               else if (startInline.IsEmpty)
                  startP.Inlines.Remove(startInline);

               Undos.Add(new DeleteLineBreakUndo(startP.Id, lbreak.Id, this, originalSelectionStart));

            }
            else
            {  // delete normal run char
               IEditable? keepInline = null;

               if (startInline.InlineLength == 1 && GetNextInline(startInline) is not EditableLineBreak elb)  // keep empty run on linebreak
               {
                  keepInline = startInline;
                  startP.Inlines.Remove(startInline);
               }
               else
               {
                  selectionStartInInline = GetCharPosInInline(startInline, Selection.Start);
                  deletedChar = startInline.InlineText.Substring(selectionStartInInline, 1);
                  startInline.InlineText = startInline.InlineText.Remove(selectionStartInInline, 1);
               }
               
               //Paragraph must always have at least an empty run
               if (startP.Inlines.Count == 0)
                  startP.Inlines.Add(new EditableRun(""));

               Undos.Add(new DeleteCharUndo(startP.Id, startInlineIdx, keepInline, deletedChar, selectionStartInInline, this, originalSelectionStart));
            }


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
      if (Selection.GetStartInline() is not IEditable startInline) 
         return; 

      int runIdx = startPar.Inlines.IndexOf(startInline);
      IEditable originalInline = startInline.CloneWithId();

      List<IEditable> eruns = SplitRunAtPos(Selection.Start, startInline, GetCharPosInInline(startInline, Selection.Start)); // creates an empty inline

      //Debug.WriteLine("split runs\n" + string.Join("\n", eruns.OfType<EditableRun>().ToList().ConvertAll(er => er.Text)));

      EditableLineBreak newELB = new ();
      startPar.Inlines.Insert(runIdx + 1, newELB);

      Undos.Add(new InsertLineBreakUndo(Selection.StartParagraph.Id, newELB.Id, (eruns[0].Id, eruns[1].Id), runIdx, originalInline, this, Selection.Start));
      UpdateTextRanges(Selection.Start, 1);

      SelectionExtendMode = ExtendMode.ExtendModeNone;

      startPar.UpdateEditableRunPositions();
      startPar.CallRequestInlinesUpdate();
      startPar.CallRequestTextLayoutInfoStart();
      startPar.CallRequestTextLayoutInfoEnd();

      Select(Selection.Start + 2, 0);
      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

      ScrollInDirection?.Invoke(1);


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
         Undos.Add(new DeleteRangeUndo(keepParsAndInlines, allBlocks[0].Id, this, originalSelectionStart, originalTRangeLength));

      (int idLeft, int idRight) edgeIds;
      List<IEditable> rangeInlines = GetRangeInlinesAndAddToDoc(trange, out edgeIds);

      //Delete the created inlines
      foreach (IEditable toDeleteRun in rangeInlines)
         foreach (Block b in allBlocks)
         {
            if (b is Paragraph p1)
            {
               p1.Inlines.Remove(toDeleteRun);
               p1.CallRequestInlinesUpdate();
            }
         }

      //Delete any full blocks contained within the range
      int idxStartPar = Blocks.IndexOf(allBlocks[0]);
      for (int i = idxStartPar + allBlocks.Count - 2; i > idxStartPar; i--)
      {
         if (Blocks[i] is Paragraph p2)
         {
            p2.Inlines.Clear();
            p2.CallRequestInlinesUpdate();
         }
         Blocks.RemoveAt(i);
      }

      //Add a blank run if all runs were deleted in one paragraph
      if (allBlocks.Count == 1 && Blocks[0] is Paragraph p3 && p3.Inlines.Count == 0)
         p3.Inlines.Add(new EditableRun(""));

      //Merge inlines of last paragraph with first
      if (allBlocks.Count > 1)
      {
         if (allBlocks[^1] is Paragraph lastPar)
         {
            List<IEditable> moveInlines = [.. lastPar.Inlines];
            lastPar.Inlines.RemoveMany(moveInlines);
            lastPar.CallRequestInlinesUpdate();
            ((Paragraph)Blocks[idxStartPar]).Inlines.AddRange(moveInlines);
            ((Paragraph)Blocks[idxStartPar]).CallRequestInlinesUpdate(); // ensure any image containers are updated
            Blocks.Remove(lastPar);
         }
      }

      //Special case where all content was deleted leaving one empty block
      if (Blocks.Count == 1 && Blocks[0] is Paragraph p4 && p4.Inlines.Count == 0)
          p4.Inlines.Add(new EditableRun(""));


      UpdateTextRanges(originalSelectionStart, -originalTRangeLength);


      UpdateSelection();

      trange.CollapseToStart();
      SelectionExtendMode = ExtendMode.ExtendModeNone;


   }

   internal void InsertParagraph(bool addUndo, int insertCharIndex)
   {  //The delete range and InsertParagraph should constitute one Undo operation

      Paragraph insertPar = GetContainingParagraph(insertCharIndex);
      List<IEditable> keepParInlineClones = [.. insertPar.Inlines.Select(il=>il.CloneWithId())]; 

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

      if (GetStartInline(insertCharIndex) is not IEditable startInline) return;

      int StartRunIdx = insertPar.Inlines.IndexOf(startInline);

      //Split at selection
      List<IEditable> parSplitRuns = SplitRunAtPos(insertCharIndex, startInline, GetCharPosInInline(startInline, insertCharIndex));


      List<IEditable> RunList1 = [.. insertPar.Inlines.Take(new Range(0, StartRunIdx)).ToList().ConvertAll(r => r)];
      if (parSplitRuns[0].InlineText != "" || RunList1.Count == 0)
         RunList1.Add(parSplitRuns[0]);
      List<IEditable> RunList2 = [.. insertPar.Inlines.Take(new Range(StartRunIdx + 1, insertPar.Inlines.Count)).ToList().ConvertAll(r => r as IEditable)];
      
      Paragraph originalPar = insertPar;
      
      originalPar.Inlines.Clear();
      originalPar.Inlines.AddRange(RunList1);
      originalPar.SelectionStartInBlock = 0;
      originalPar.CollapseToStart();

      if (originalPar.Inlines.Last() is EditableLineBreak elb)
      {
         originalPar.Inlines.Insert(originalPar.Inlines.Count, new EditableRun(""));
      }

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
         Undos.Add(new InsertParagraphUndo(this, originalPar.Id, parToInsert.Id, keepParInlineClones, originalSelStart, selectionLength - 1));

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;
      Selection.End += 1;
      Selection.CollapseToEnd();

      originalPar.CallRequestTextLayoutInfoStart();
      parToInsert.CallRequestTextLayoutInfoStart();
      originalPar.CallRequestTextLayoutInfoEnd();
      parToInsert.CallRequestTextLayoutInfoEnd();

      ScrollInDirection?.Invoke(1);

   }

   internal void MergeParagraphForward(int mergeCharIndex, bool saveUndo, int originalSelectionStart)
   {
      Paragraph thisPar = GetContainingParagraph(mergeCharIndex);
      int thisParIndex = Blocks.IndexOf(thisPar);
      if (thisParIndex == Blocks.Count - 1) return; //is last Paragraph, can't merge forward
      int origMergedParInlinesCount = thisPar.Inlines.Count;

      if (Blocks[thisParIndex + 1] is not Paragraph nextPar) return;

      bool IsNextParagraphEmpty = nextPar.Inlines.Count == 1 && nextPar.Inlines[0].IsEmpty;
      bool IsThisParagraphEmpty = thisPar.Inlines.Count == 1 && thisPar.Inlines[0].IsEmpty;

      if (IsThisParagraphEmpty)
      {
         thisPar.Inlines.Clear();
         origMergedParInlinesCount = 0;
      }

      if (saveUndo)
         Undos.Add(new MergeParagraphUndo(origMergedParInlinesCount, thisPar.Id, nextPar.FullClone(), this, originalSelectionStart)); // cloned with Id and inlines

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
         List<IEditable> inlinesToMove = [.. nextPar.Inlines];
         nextPar.Inlines.Clear();
         nextPar.CallRequestInlinesUpdate(); // ensure image containers are updated
         thisPar.Inlines.AddRange(inlinesToMove);
      }
           
      Blocks.Remove(nextPar);

      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

      UpdateTextRanges(mergeCharIndex, -1);

      thisPar.CallRequestInlinesUpdate();
      UpdateBlockAndInlineStarts(thisParIndex);

      thisPar.CallRequestTextBoxFocus();

      UpdateSelectedParagraphs();

      if (ShowDebugger)
         UpdateDebuggerSelectionParagraphs();


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
      
      Selection.BiasForwardStart = true;
      Selection.BiasForwardEnd = true;

      Paragraph startP = Selection.StartParagraph;

      if (startP.SelectionStartInBlock == startP.TextLength)
         MergeParagraphForward(Selection.Start, true, originalSelectionStart); //updates text ranges and adds undo
      else
      {
         int NextWordEndPoint = -1;
         if (Selection.GetStartInline() is IEditable startInline && (startInline.IsUIContainer || startInline.IsLineBreak))
            NextWordEndPoint = Selection.Start + 1;
         else
         {
            int IndexNextSpace = Selection.StartParagraph.Text.IndexOf(' ', Selection.Start - Selection.StartParagraph.StartInDoc);
            if (IndexNextSpace == -1)
               IndexNextSpace = Selection.StartParagraph.TextLength;
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