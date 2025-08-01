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
         Paragraph thisPar = (Paragraph)flowDoc.Blocks[insertParIndex];
         Run? thisRun = thisPar.Inlines[insertInlineIdx] as Run;
         thisRun!.Text = thisRun.Text!.Remove(insertPos, 1);
         thisPar.CallRequestInlinesUpdate();
         flowDoc.UpdateBlockAndInlineStarts(insertParIndex);
         flowDoc.Selection.Start = origSelectionStart;
         flowDoc.Selection.End = flowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed InsertCharUndo at inline idx: " + insertInlineIdx); }

   }
}

internal class DeleteCharUndo(int deleteParIndex, int deleteInlineIdx, IEditable deletedRun, string deleteChar, int deletePos, FlowDocument flowDoc, int origSelectionStart) : IUndo
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
            Run? thisRun = thisPar.Inlines[deleteInlineIdx] as Run;
            thisRun!.Text = thisRun.Text!.Insert(deletePos, deleteChar);
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
            flowDoc.ApplyFormattingInline(propassoc.FormatRun, propassoc.InlineItem, propassoc.PropertyValue);

      foreach (Paragraph p in flowDoc.GetRangeBlocks(tRange).Where(b=>b.IsParagraph))
         p.CallRequestInlinesUpdate();
      
      flowDoc.Selection.Start = originalSelection;
      flowDoc.Selection.End = originalSelection;

   }
}


