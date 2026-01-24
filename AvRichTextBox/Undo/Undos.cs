using Avalonia.Controls.Documents;
using DynamicData;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AvRichTextBox;

internal class InsertCharUndo (int insertParIndex, int insertInlineIdx, int insertPos, FlowDocument flowDoc, int origSelectionStart) : IUndo
{
   public int UndoEditOffset => -1;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {
         if (flowDoc.Blocks[insertParIndex] is not Paragraph thisPar) return;
         if (thisPar.Inlines[insertInlineIdx] is not Run thisRun) return;
         thisRun!.Text = thisRun.Text!.Remove(insertPos, 1);
         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(insertParIndex);
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed InsertCharUndo at inline idx: " + insertInlineIdx); }

   }
}

internal class DeleteCharUndo(int deleteParIndex, int deleteInlineIdx, IEditable? deletedRun, string deleteChar, int deletePos, FlowDocument flowDoc, int origSelectionStart) : IUndo
{  
   public int UndoEditOffset => 1;
   public bool UpdateTextRanges => true;
   internal int DeleteInlineIdx => deleteInlineIdx;

   public void PerformUndo()
   {
      try
      {
         Paragraph thisPar = (Paragraph)flowDoc.Blocks[deleteParIndex];
         if (deletedRun != null)
            thisPar.Inlines.Insert(deleteInlineIdx, deletedRun);
         else
         {
            if (thisPar.Inlines[deleteInlineIdx] is Run thisRun)
               thisRun.Text = thisRun.Text!.Insert(deletePos, deleteChar);
         }
         
         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(deleteParIndex);
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed DeleteCharUndo at delete pos: " + deletePos); }
   }
      
}

internal class DeleteImageUndo(int deleteParIndex, IEditable deletedIUC, int deletedInlineIdx, FlowDocument flowDoc, int origSelectionStart, bool emptyRunAdded) : IUndo
{
   public int UndoEditOffset => 1;
   public bool UpdateTextRanges => true;
   
   public void PerformUndo()
   {
      try
      {
         Paragraph thisPar = (Paragraph)flowDoc.Blocks[deleteParIndex];
         if (emptyRunAdded)
            thisPar.Inlines.RemoveAt(deletedInlineIdx);
         thisPar.Inlines.Insert(deletedInlineIdx, deletedIUC);
         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(deleteParIndex);
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

internal class DeleteRangeUndo (Dictionary<Block, List<IEditable>> keptParsAndInlines, int parIndex, FlowDocument flowDoc, int origSelectionStart, int undoEditOffset) : IUndo
{  //parInlines are cloned inlines

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
      catch { Debug.WriteLine("Failed DeleteRangeUndo at ParIndex: " + parIndex); }
   }

}


internal class InsertParagraphUndo (FlowDocument flowDoc, int insertedParIndex, List<IEditable> keepParInlines, int origSelectionStart, int undoEditOffset) : IUndo
{  
   public int UndoEditOffset => undoEditOffset;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {
         Paragraph insertedPar = (Paragraph)flowDoc.Blocks[insertedParIndex];
         insertedPar.Inlines.Clear();
         insertedPar.Inlines.AddRange(keepParInlines);
         flowDoc.Blocks.RemoveAt(insertedParIndex + 1);
         flowDoc.UpdateBlockAndInlineStarts(insertedParIndex);
         //flowDoc.MergeParagraphForward(insertedIndex, false, origSelectionStart);
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed InsertParagraphUndo at InsertedIndex: " + insertedParIndex); }

   }
}


internal class MergeParagraphUndo (int origMergedParInlinesCount, int mergedParIndex, Paragraph removedPar, FlowDocument flowDoc, int originalSelectionStart) : IUndo 
{ //removedPar is a clone

   public int UndoEditOffset => 1;
   public bool UpdateTextRanges => false;

   public void PerformUndo()
   {
      try
      {
         //flowDoc.InsertParagraph(false, mergedCharIndex);

         Paragraph mergedPar = (Paragraph)flowDoc.Blocks[mergedParIndex];

         for (int rno = mergedPar.Inlines.Count - 1; rno >= origMergedParInlinesCount; rno--)
            mergedPar.Inlines.RemoveAt(rno);

         flowDoc.Blocks.Insert(mergedParIndex + 1, removedPar);

         flowDoc.UpdateBlockAndInlineStarts(mergedParIndex);
         flowDoc.Selection.End = originalSelectionStart;
         flowDoc.Selection.Start = originalSelectionStart;
                  
      }
      catch { Debug.WriteLine("Failed MergeParagraphUndo at MergedParIndex: " + mergedParIndex); }
   }
}


internal class ApplyFormattingUndo (FlowDocument flowDoc, List<IEditablePropertyAssociation> propertyAssociations, int originalSelection, TextRange tRange) : IUndo 
{
   public int UndoEditOffset => 0;
   public bool UpdateTextRanges => false;

   public void PerformUndo()
   {      
      foreach (IEditablePropertyAssociation propassoc in propertyAssociations)
         if (propassoc.FormatRun != null)
         {
            if (tRange.GetStartPar() is Paragraph p && p.Inlines.FirstOrDefault(il => il.Id == propassoc.InlineId) is IEditable iline)
               flowDoc.ApplyFormattingInline(propassoc.FormatRun, iline, propassoc.PropertyValue);
         }

      foreach (Paragraph p in flowDoc.GetRangeBlocks(tRange).Where(b=>b.IsParagraph))
         p.CallRequestInlinesUpdate();
      
      flowDoc.Selection.Start = originalSelection;
      flowDoc.Selection.End = originalSelection;

   }
}


internal class InsertLineBreakUndo(int insertParIndex, int insertInlineIdx, FlowDocument flowDoc, int origSelectionStart) : IUndo
{
   public int UndoEditOffset => -1;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {
         if (flowDoc.Blocks[insertParIndex] is not Paragraph thisPar) return;
         if (thisPar.Inlines[insertInlineIdx] is not EditableLineBreak thisELB) return;
         thisPar.Inlines.Remove(thisELB);
         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(insertParIndex);
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed InsertCharUndo at inline idx: " + insertInlineIdx); }

   }
}

internal class DeleteLineBreakUndo(int parIndex, int lineBreakId, FlowDocument flowDoc, int origSelectionStart) : IUndo
{
   public int UndoEditOffset => -1;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      if (flowDoc.Blocks[parIndex] is not Paragraph thisPar) return;
            
      if (flowDoc.GetStartInline(origSelectionStart) is IEditable startInline)
      {
         int runIdx = thisPar.Inlines.IndexOf(startInline);
         int charPosInInline = startInline.GetCharPosInInline(origSelectionStart);
         if (charPosInInline > 0)
         {
            List<IEditable> newRuns = flowDoc.SplitRunAtPos(origSelectionStart, startInline, charPosInInline);
            runIdx += 1;
         }

         thisPar.Inlines.Insert(runIdx, new EditableLineBreak() { Id = lineBreakId });

         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(parIndex);
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      

   }
}

