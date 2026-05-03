using Avalonia.Threading;
using DynamicData;

namespace AvRichTextBox;

internal class TextChangedUndo(FlowDocument flowDoc, int parId, int runId, int start, string deletedText, string insertText, int origSelectionStart) : IUndo
{
   public int UndoEditOffset => -1;
   public bool UpdateTextRanges => true;
   
   public void PerformUndo()
   {
      if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

      int thisParLengthBefore = thisPar.TextLength;

      if (thisPar.Inlines.FirstOrDefault(il=> il.Id == runId) is not EditableRun thisRun) return;

      flowDoc.disableRunTextUndo = true;

      if (insertText.Length > 0)
         thisRun.Text = thisRun.Text!.Remove(start, insertText.Length);
      if (deletedText.Length > 0)
         thisRun.Text = thisRun.Text!.Insert(start, deletedText);

      flowDoc.disableRunTextUndo = false;

      int thisParLengthAfter = thisPar.TextLength;

      thisPar.CallRequestInlinesUpdate();
      flowDoc.UpdateBlockAndInlineStarts(thisPar);
      flowDoc.UpdateTextRanges(thisPar.StartInDoc, thisParLengthAfter - thisParLengthBefore);

      Dispatcher.UIThread.Post(() =>
      {
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      });
                  
   }
}

internal class DeleteImageUndo(int parId, IEditable deletedIUC, int deletedInlineIdx, FlowDocument flowDoc, int origSelectionStart, bool emptyRunAdded) : IUndo
{
   public int UndoEditOffset => 1;
   public bool UpdateTextRanges => true;
   
   public void PerformUndo()
   {
      try
      {
         if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
         if (emptyRunAdded)
            thisPar.Inlines.RemoveAt(deletedInlineIdx);
         thisPar.Inlines.Insert(deletedInlineIdx, deletedIUC);
         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(thisPar);
         flowDoc.UpdateTextRanges(thisPar.StartInDoc, 1);

         Dispatcher.UIThread.Post(() =>
         {
            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
         });
      }
      catch { Debug.WriteLine("Failed DeleteImageUndo at delete pos: " + origSelectionStart); }
   }
      
}

internal class DeleteRunUndo(int parId, EditableRun removedRunClone, int deletedRunIdx, FlowDocument flowDoc, int origSelectionStart) : IUndo
{
   public int UndoEditOffset => 1;
   public bool UpdateTextRanges => true;
   
   public void PerformUndo()
   {
      try
      {
         if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

         int thisParLengthBefore = thisPar.TextLength;
         
         thisPar.Inlines.Insert(deletedRunIdx, removedRunClone);

         int thisParLengthAfter = thisPar.TextLength;

         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(thisPar);
         flowDoc.UpdateTextRanges(thisPar.StartInDoc, thisParLengthAfter - thisParLengthBefore);

         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed DeleteImageUndo at delete pos: " + origSelectionStart); }
   }
      
}

internal class InsertNewFormattedTextUndo(int parId, EditableRun removedRunClone, (int leftId, int rightId) edgeIds, int addedRunId, int deletedRunIdx, FlowDocument flowDoc, int origSelectionStart) : IUndo
{
   public int UndoEditOffset => 1;
   public bool UpdateTextRanges => true;
   
   public void PerformUndo()
   {
      try
      {
         flowDoc.disableRunTextUndo = true;
         if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

         int thisParLengthBefore = thisPar.TextLength;

         if (thisPar.Inlines.FirstOrDefault(il => il.Id == edgeIds.leftId) is EditableRun leftRun)
            thisPar.Inlines.Remove(leftRun);
         if (thisPar.Inlines.FirstOrDefault(il => il.Id == edgeIds.rightId) is EditableRun rightRun)
            thisPar.Inlines.Remove(rightRun);
         if (thisPar.Inlines.FirstOrDefault(il => il.Id == addedRunId) is EditableRun addedRun)
            thisPar.Inlines.Remove(addedRun);
         
         thisPar.Inlines.Insert(deletedRunIdx, removedRunClone);

         flowDoc.disableRunTextUndo = false;

         int thisParLengthAfter = thisPar.TextLength;

         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(thisPar);
         flowDoc.UpdateTextRanges(thisPar.StartInDoc, thisParLengthAfter - thisParLengthBefore);


         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed InsertNewFormattedTextUndo at delete pos: " + origSelectionStart); }
   }
      
}


internal class PasteUndo(List<Paragraph> keptPars, int parIndex, FlowDocument flowDoc, int origSelectionStart, int undoEditOffset, bool firstParEmpty, List<int> addedBlockIds, bool firstParWasDeleted) : IUndo
{
   public int UndoEditOffset => undoEditOffset;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {
         flowDoc.disableRunTextUndo = true;

         int lengthBefore = flowDoc.Text.Length;

         flowDoc.RestoreDeletedBlocks(keptPars, parIndex, firstParWasDeleted);
         
         foreach (int bid in addedBlockIds)
            if (flowDoc.Blocks.FirstOrDefault(b => b.Id == bid) is Block foundBlock)
               flowDoc.Blocks.Remove(foundBlock);

         if (firstParEmpty)
         {
            Paragraph firstPar = flowDoc.GetAllParagraphs.ToList()[parIndex];
            if (firstPar.Inlines.Count == 1 && firstPar.Inlines[0] is EditableRun run)
               run.Text = "";
         }

         flowDoc.disableRunTextUndo = false;

         int lengthAfter = flowDoc.Text.Length;

         Dispatcher.UIThread.Post(() =>
         {
            flowDoc.Selection.Start = 0;  //??? why necessary for caret?
            flowDoc.Selection.End = 0;
            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = origSelectionStart;
            flowDoc.UpdateSelection();
            flowDoc.UpdateTextRanges(keptPars[0].StartInDoc, lengthAfter - lengthBefore);
         });         

      }
      catch { Debug.WriteLine("Failed Undo at OrigSelectionStart: " + origSelectionStart); }
   }
}

internal class DeleteRangeUndo (List<Paragraph> keptParClones, int startParIndex, FlowDocument flowDoc, int origSelectionStart, int undoEditOffset, bool firstParWasDeleted) : IUndo
{  //parInlines are cloned inlines

   public int UndoEditOffset => undoEditOffset;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {
         flowDoc.disableRunTextUndo = true;
         int lengthBefore = flowDoc.Text.Length;
         flowDoc.RestoreDeletedBlocks(keptParClones, startParIndex, firstParWasDeleted);
         flowDoc.disableRunTextUndo = false;
         int lengthAfter = flowDoc.Text.Length;

         Dispatcher.UIThread.Post(() =>
         {
            flowDoc.Selection.Start = Math.Max(0, origSelectionStart - 1);  //necessary to reset caret
            flowDoc.Selection.CollapseToStart();
            flowDoc.UpdateSelection();
            flowDoc.UpdateTextRanges(origSelectionStart, lengthAfter - lengthBefore);

            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = origSelectionStart;
         });

      }
      catch { Debug.WriteLine("Failed DeleteRangeUndo at Par index: " + startParIndex); }
   }

}


internal class InsertParagraphUndo (FlowDocument flowDoc, int origParId, int insertedParId, List<IEditable> keepParInlines, int origSelectionStart, int undoEditOffset) : IUndo
{  //all original inlines preserved, so no need to worry about split inlines

   public int UndoEditOffset => undoEditOffset;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {
         if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == insertedParId) is not Paragraph insertedPar) return;
         if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == origParId) is not Paragraph origPar) return;
         
         int lengthBefore = flowDoc.Text.Length;

         origPar.Inlines.Clear();
         origPar.Inlines.AddRange(keepParInlines);
         flowDoc.Blocks.Remove(insertedPar);

         int idx = flowDoc.Blocks.IndexOf(origPar); ///////////////$$$$$$

         int lengthAfter = flowDoc.Text.Length;

         flowDoc.UpdateBlockAndInlineStarts(idx);
         flowDoc.UpdateTextRanges(origSelectionStart, lengthAfter - lengthBefore);

         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed InsertParagraphUndo at Inserted par id: " + insertedParId); }

   }
}


internal class MergeParagraphUndo (int origMergedParInlinesCount, int mergedParId, Paragraph removedParClone, FlowDocument flowDoc, int originalSelectionStart) : IUndo 
{ //removedPar is a clone

   public int UndoEditOffset => 1;
   public bool UpdateTextRanges => false;

   public void PerformUndo()
   {
      try
      {
         if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == mergedParId) is not Paragraph mergedPar) return;
         int mergedParIndex = flowDoc.Blocks.IndexOf(mergedPar);

         int lengthBefore = flowDoc.Text.Length;

         for (int rno = mergedPar.Inlines.Count - 1; rno >= origMergedParInlinesCount; rno--)
            mergedPar.Inlines.RemoveAt(rno);

         if (mergedPar.Inlines.Count == 0)
            mergedPar.Inlines.Add(new EditableRun(""));
         
         mergedPar.CallRequestInlinesUpdate();
         mergedPar.UpdateEditableRunPositions();

         flowDoc.Blocks.Insert(mergedParIndex + 1, removedParClone);

         int lengthAfter = flowDoc.Text.Length;

         flowDoc.UpdateBlockAndInlineStarts(mergedParIndex);
         flowDoc.UpdateTextRanges(originalSelectionStart, lengthAfter - lengthBefore);

         flowDoc.Selection.End = originalSelectionStart;
         flowDoc.Selection.Start = originalSelectionStart;

      }
      catch { Debug.WriteLine("Failed MergeParagraphUndo at MergedPar: " + mergedParId); }
   }
}


internal class ApplyFormattingUndo (FlowDocument flowDoc, List<EditablePropertyAssociation> propertyAssociations, (int LeftId, int RightId) edgeIds, int originalSelection, TextRange tRange) : IUndo 
{
   public int UndoEditOffset => 0;
   public bool UpdateTextRanges => false;

   public void PerformUndo()
   {
      flowDoc.disableRunTextUndo = true;

      int rangeStart = tRange.Start;
      int rangeEnd = tRange.End;

      List<Paragraph> allPars = flowDoc.AllParagraphs;

      foreach (EditablePropertyAssociation propassoc in propertyAssociations)
      { 
         if (allPars.FirstOrDefault(bl => bl.Id == propassoc.BlockId) is Paragraph p && p.Inlines.FirstOrDefault(il => il.Id == propassoc.InlineId) is IEditable iline)
            flowDoc.ApplyFormattingInlines(propassoc.FormatRuns, [iline], propassoc.PropertyValue);
      }

      if (allPars.FirstOrDefault(bl => bl.Id == propertyAssociations[0].BlockId) is not Paragraph firstPar ||
         firstPar.Inlines.FirstOrDefault(il => il.Id == propertyAssociations[0].InlineId) is not IEditable ilineFirst ||
         firstPar.Inlines.FirstOrDefault(il => il.Id == edgeIds.LeftId) is not IEditable ilineLeft) return; 

      if (allPars.FirstOrDefault(bl => bl.Id == propertyAssociations[^1].BlockId) is not Paragraph lastPar ||
         lastPar.Inlines.FirstOrDefault(il => il.Id == propertyAssociations[^1].InlineId) is not IEditable ilineLast ||
         lastPar.Inlines.FirstOrDefault(il => il.Id == edgeIds.RightId) is not IEditable ilineRight) return; 
      
      if (ilineLast.Id > ilineRight.Id)
      {
         ilineRight.InlineText = ilineLast.InlineText + ilineRight.InlineText;
         lastPar.Inlines.Remove(ilineLast);
      }

      if (ilineLeft.Id != ilineFirst.Id)
      {
         if (propertyAssociations.Count == 1)
         {
            if (edgeIds.LeftId > edgeIds.RightId)
            {
               ilineRight.InlineText = ilineLeft.InlineText + ilineRight.InlineText;
               firstPar.Inlines.Remove(ilineLeft);
            }
         }
         else
         {
            int lowestId = Math.Min(ilineLeft.Id, ilineFirst.Id);

            if (ilineLeft.Id > ilineFirst.Id)
            {
               ilineFirst.InlineText = ilineLeft.InlineText + ilineFirst.InlineText;
               firstPar.Inlines.Remove(ilineLeft);
               ilineFirst.Id = lowestId;
            }
            else
            {
               ilineLeft.InlineText += ilineFirst.InlineText;
               firstPar.Inlines.Remove(ilineFirst);
            }
         }
      }

      foreach (Paragraph p in flowDoc.GetOverlappingParagraphsInRange(rangeStart, rangeEnd).OfType<Paragraph>())
      {
         p.CallRequestInlinesUpdate();
         p.UpdateEditableRunPositions();
      }

      lastPar.CallRequestInlinesUpdate();  // fail-safe

      flowDoc.Selection.Start = originalSelection;
      flowDoc.Selection.End = originalSelection;

      flowDoc.disableRunTextUndo = false;

   }
}


internal class InsertLineBreakUndo(int insertParId, int insertedLBId, List<int> addedInlineIds, int insertIdx, IEditable origInlineClone, FlowDocument flowDoc, int origSelectionStart) : IUndo
{
   public int UndoEditOffset => -1;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {
         flowDoc.disableRunTextUndo = true;
         if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == insertParId) is not Paragraph thisPar) return;
         int thisParLengthBefore = thisPar.TextLength;
         if (thisPar.Inlines.FirstOrDefault(lb => lb.Id == insertedLBId) is not EditableLineBreak thisELB) return;
         
         foreach (int ilId in addedInlineIds)
         {
            if (thisPar.Inlines.FirstOrDefault(il => il.Id == ilId) is IEditable ied)
               thisPar.Inlines.Remove(ied);
         }
         
         thisPar.Inlines.Remove(thisELB);
         thisPar.Inlines.Insert(insertIdx, origInlineClone);

         int thisParLengthAfter = thisPar.TextLength;

         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(thisPar);
         flowDoc.UpdateTextRanges(thisPar.StartInDoc, thisParLengthAfter - thisParLengthBefore);
         
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
         flowDoc.disableRunTextUndo = false;
      }
      catch { Debug.WriteLine("Failed InsertCharUndo of linebreak"); }

   }
}

internal class DeleteLineBreakUndo(int parId, ((Type t1, int id1), (Type t2, int id2)) types, int lineBreakIdx, FlowDocument flowDoc, int origSelectionStart) : IUndo
{
   public int UndoEditOffset => -1;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
      int thisParLengthBefore = thisPar.TextLength;

      flowDoc.disableRunTextUndo = true;

      IEditable addIED1 = null!;
      if (types.Item1.t1 == typeof(EditableRun))
         addIED1 = new EditableRun("");
      else
         addIED1 = new EditableLineBreak();
      addIED1.Id = types.Item1.id1;

      thisPar.Inlines.Insert(lineBreakIdx, addIED1);

      
      if (types.Item2.t2 != null)
      {
         IEditable addIED2 = null!;
         if (types.Item2.t2 == typeof(EditableRun))
            addIED2 = new EditableRun("");
         else
            addIED2 = new EditableLineBreak();
         addIED2.Id = types.Item2.id2;
         thisPar.Inlines.Insert(lineBreakIdx + 1, addIED2);
      }

      int thisParLengthAfter = thisPar.TextLength;

      thisPar.CallRequestInlinesUpdate();
      flowDoc.UpdateBlockAndInlineStarts(thisPar);
      flowDoc.UpdateTextRanges(thisPar.StartInDoc, thisParLengthAfter - thisParLengthBefore);

      flowDoc.Selection.Start = origSelectionStart;
      flowDoc.Selection.End = flowDoc.Selection.Start;

      flowDoc.disableRunTextUndo = false;


   }

   
}



