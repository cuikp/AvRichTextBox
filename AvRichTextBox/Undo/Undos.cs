using Avalonia.Controls.Documents;
using DynamicData;
using System.Diagnostics;

namespace AvRichTextBox;

internal class InsertCharUndo (int parId, int insertInlineIdx, int insertPos, FlowDocument flowDoc, int origSelectionStart) : IUndo
{
   public int UndoEditOffset => -1;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {
         if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
         if (thisPar.Inlines[insertInlineIdx] is not Run thisRun) return;
         thisRun!.Text = thisRun.Text!.Remove(insertPos, 1);
         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(thisPar);
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed InsertCharUndo at inline idx: " + insertInlineIdx); }

   }
}

internal class DeleteCharUndo(int parId, int deleteInlineIdx, IEditable? deletedRun, string deleteChar, int deletePos, FlowDocument flowDoc, int origSelectionStart) : IUndo
{  
   public int UndoEditOffset => 1;
   public bool UpdateTextRanges => true;
   internal int DeleteInlineIdx => deleteInlineIdx;

   public void PerformUndo()
   {
      try
      {
         if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
         if (deletedRun != null)
            thisPar.Inlines.Insert(deleteInlineIdx, deletedRun);
         else
         {
            if (thisPar.Inlines[deleteInlineIdx] is Run thisRun)
               thisRun.Text = thisRun.Text!.Insert(deletePos, deleteChar);
         }
         
         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(thisPar);
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed DeleteCharUndo at delete pos: " + deletePos); }
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
         if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
         if (emptyRunAdded)
            thisPar.Inlines.RemoveAt(deletedInlineIdx);
         thisPar.Inlines.Insert(deletedInlineIdx, deletedIUC);
         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(thisPar);
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed DeleteImageUndo at delete pos: " + origSelectionStart); }
   }
      
}

internal class PasteUndo(Dictionary<Block, List<IEditable>> keptParsAndInlines, int parIndex, FlowDocument flowDoc, int origSelectionStart, int undoEditOffset) : IUndo
{
   public int UndoEditOffset => undoEditOffset;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {
         flowDoc.RestoreDeletedBlocks(keptParsAndInlines, parIndex);

         flowDoc.Selection.Start = 0;  //??? why necessary for caret?
         flowDoc.Selection.End = 0;
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = origSelectionStart;
         flowDoc.UpdateSelection();
      }
      catch { Debug.WriteLine("Failed PasteUndo at OrigSelectionStart: " + origSelectionStart); }
   }
}

internal class DeleteRangeUndo (Dictionary<Block, List<IEditable>> keptParsAndInlines, int startParId, FlowDocument flowDoc, int origSelectionStart, int undoEditOffset) : IUndo
{  //parInlines are cloned inlines

   public int UndoEditOffset => undoEditOffset;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {
         if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == startParId) is not Paragraph startPar) return;
         int parIndex = flowDoc.Blocks.IndexOf(startPar);

         flowDoc.RestoreDeletedBlocks(keptParsAndInlines, parIndex);

         flowDoc.Selection.Start = 0;  //??? why necessary for caret?
         flowDoc.Selection.End = 0;
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = origSelectionStart;

         flowDoc.UpdateSelection();
      }
      catch { Debug.WriteLine("Failed DeleteRangeUndo at Par: " + startParId); }
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
         origPar.Inlines.Clear();
         origPar.Inlines.AddRange(keepParInlines);
         flowDoc.Blocks.Remove(insertedPar);
         int idx = flowDoc.Blocks.IndexOf(origPar);
         flowDoc.UpdateBlockAndInlineStarts(idx);
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

         for (int rno = mergedPar.Inlines.Count - 1; rno >= origMergedParInlinesCount; rno--)
            mergedPar.Inlines.RemoveAt(rno);

         if (mergedPar.Inlines.Count == 0)
            mergedPar.Inlines.Add(new EditableRun(""));
         
         mergedPar.CallRequestInlinesUpdate();
         mergedPar.UpdateEditableRunPositions();

         flowDoc.Blocks.Insert(mergedParIndex + 1, removedParClone);

         flowDoc.UpdateBlockAndInlineStarts(mergedParIndex);
         flowDoc.Selection.End = originalSelectionStart;
         flowDoc.Selection.Start = originalSelectionStart;
                  
      }
      catch { Debug.WriteLine("Failed MergeParagraphUndo at MergedPar: " + mergedParId); }
   }
}


internal class ApplyFormattingUndo (FlowDocument flowDoc, List<IEditablePropertyAssociation> propertyAssociations, (int LeftId, int RightId) edgeIds, int originalSelection, TextRange tRange) : IUndo 
{
   public int UndoEditOffset => 0;
   public bool UpdateTextRanges => false;

   public void PerformUndo()
   {
      int rangeStart = tRange.Start;
      int rangeEnd = tRange.End; 

      foreach (IEditablePropertyAssociation propassoc in propertyAssociations)
      { 
         if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == propassoc.BlockId) is Paragraph p && p.Inlines.FirstOrDefault(il => il.Id == propassoc.InlineId) is IEditable iline)
            flowDoc.ApplyFormattingInline(propassoc.FormatRun, iline, propassoc.PropertyValue);
      }

      if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == propertyAssociations[0].BlockId) is not Paragraph firstPar ||
         firstPar.Inlines.FirstOrDefault(il => il.Id == propertyAssociations[0].InlineId) is not IEditable ilineFirst ||
         firstPar.Inlines.FirstOrDefault(il => il.Id == edgeIds.LeftId) is not IEditable ilineLeft) return; 

      if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == propertyAssociations[^1].BlockId) is not Paragraph lastPar ||
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

      foreach (Paragraph p in flowDoc.GetRangeBlocks(rangeStart, rangeEnd).OfType<Paragraph>())
      {
         p.CallRequestInlinesUpdate();
         p.UpdateEditableRunPositions();
      }

      lastPar.CallRequestInlinesUpdate();  // fail-safe

      flowDoc.Selection.Start = originalSelection;
      flowDoc.Selection.End = originalSelection;

   }
}


internal class InsertLineBreakUndo(int insertParId, int insertedLBId, (int addedInlineLeftId, int addedInlineRightId) addedInlines, int insertIdx, IEditable origInline, FlowDocument flowDoc, int origSelectionStart) : IUndo
{
   public int UndoEditOffset => -1;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {
         if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == insertParId) is not Paragraph thisPar) return;
         if (thisPar.Inlines.FirstOrDefault(lb => lb.Id == insertedLBId) is not EditableLineBreak thisELB) return;
         if (thisPar.Inlines.FirstOrDefault(il => il.Id == addedInlines.addedInlineLeftId) is not IEditable iedLeft) return;
         if (thisPar.Inlines.FirstOrDefault(il => il.Id == addedInlines.addedInlineRightId) is not IEditable iedRight) return;
         
         thisPar.Inlines.Remove(iedLeft);
         thisPar.Inlines.Remove(iedRight);
         thisPar.Inlines.Remove(thisELB);
         thisPar.Inlines.Insert(insertIdx, origInline);

         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(thisPar);
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed InsertCharUndo of linebreak"); }

   }
}

internal class DeleteLineBreakUndo(int parId, int lineBreakId, FlowDocument flowDoc, int origSelectionStart) : IUndo
{
   public int UndoEditOffset => -1;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      if (flowDoc.Blocks.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;
            
      if (flowDoc.GetStartInline(origSelectionStart) is IEditable startInline)
      {
         int runIdx = thisPar.Inlines.IndexOf(startInline);
         int charPosInInline = flowDoc.GetCharPosInInline(startInline, origSelectionStart);
         if (charPosInInline > 0)
         {
            List<IEditable> newRuns = flowDoc.SplitRunAtPos(origSelectionStart, startInline, charPosInInline);
            runIdx += 1;
         }

         thisPar.Inlines.Insert(runIdx, new EditableLineBreak() { Id = lineBreakId });

         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(thisPar);
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      

   }
}

