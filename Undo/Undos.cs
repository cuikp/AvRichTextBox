using Avalonia.Controls.Documents;
using DynamicData;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AvRichTextBox;

internal class InsertCharUndo (Paragraph charParagraph, int insertInlineIdx, int insertPos, FlowDocument flowDoc, int origSelectionStart) : IUndo
{
   internal Paragraph CharParagraph => charParagraph;
   internal int InsertInlineIdx =>insertInlineIdx;
   internal int InsertPos => insertPos;
   internal FlowDocument FlowDoc => flowDoc;
   public int UndoEditOffset => -1;
   public bool UpdateTextRanges => true;

   internal int OrigSelectionStart = origSelectionStart;
   
   public void PerformUndo()
   {
      try
      {
         Run? thisRun = CharParagraph.Inlines[InsertInlineIdx] as Run;
         thisRun!.Text = thisRun.Text!.Remove(InsertPos, 1);
         CharParagraph.RequestInlinesUpdate = true;
         FlowDoc.UpdateBlockAndInlineStarts(CharParagraph);
         FlowDoc.Selection.Start = OrigSelectionStart;
         FlowDoc.Selection.End = FlowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed InsertCharUndo at inline idx: " + InsertInlineIdx); }

   }
}

internal class DeleteCharUndo : IUndo
{
   internal Paragraph CharParagraph;
   internal string DeleteChar;
   internal IEditable DeleteInline;
   internal int DeletePos;
   internal FlowDocument FlowDoc;
   internal int OrigSelectionStart;
   public int UndoEditOffset => 1;
   public bool UpdateTextRanges => true;
   internal bool CreateInline = false;
   internal int DeleteInlineIdx = -1;

   public DeleteCharUndo(Paragraph charParagraph, IEditable deleteInline, int deleteInlineIdx, string deleteChar, int deletePos, FlowDocument flowDoc, int origSelectionStart, bool createInline)
   {
      
      CharParagraph = charParagraph;
      DeleteChar = deleteChar;
      DeleteInline = deleteInline;
      DeletePos = deletePos;
      FlowDoc = flowDoc;
      OrigSelectionStart = origSelectionStart;
      CreateInline = createInline;
      DeleteInlineIdx = deleteInlineIdx;

   }

   public void PerformUndo()
   {
      try
      {
         IEditable? thisInline = null;
         if (CreateInline)
         {
            thisInline = DeleteInline;
            CharParagraph.Inlines.Insert(DeleteInlineIdx, thisInline);
         }
         else
            thisInline = CharParagraph.Inlines[DeleteInlineIdx];

         if (thisInline.IsRun)
         {
            EditableRun? thisRun = thisInline as EditableRun;
            thisRun!.Text = thisRun.Text!.Insert(DeletePos, DeleteChar);
         }

         CharParagraph.RequestInlinesUpdate = true;
         FlowDoc.UpdateBlockAndInlineStarts(CharParagraph);
         FlowDoc.Selection.Start = OrigSelectionStart;
         FlowDoc.Selection.End = FlowDoc.Selection.Start;
      }
      catch { Debug.WriteLine("Failed DeleteCharUndo at delete pos: " + DeletePos); }
   }
      
}

internal class PasteUndo(Dictionary<Block, List<IEditable>> keptParsAndInlines, int parIndex, FlowDocument flowDoc, int origSelectionStart, int undoEditOffset) : IUndo
{
   internal Dictionary<Block, List<IEditable>> KeptParsAndInlines => keptParsAndInlines;
   internal int ParIndex => parIndex;
   internal FlowDocument FlowDoc => flowDoc;
   internal int OrigSelectionStart => origSelectionStart;
   public int UndoEditOffset => undoEditOffset;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {

         FlowDoc.RestoreDeletedBlocks(KeptParsAndInlines, ParIndex);

         FlowDoc.Selection.Start = 0;  //??? why necessary for cursor?
         FlowDoc.Selection.End = 0;
         FlowDoc.Selection.Start = OrigSelectionStart;
         FlowDoc.Selection.End = OrigSelectionStart;
         FlowDoc.UpdateSelection();
      }
      catch { Debug.WriteLine("Failed PasteUndo at OrigSelectionStart: " + OrigSelectionStart); }
   }
}

internal class DeleteRangeUndo (Dictionary<Block, List<IEditable>> keptParsAndInlines, int parIndex, FlowDocument flowDoc, int origSelectionStart, int undoEditOffset) : IUndo
{  //parInlines are cloned inlines

   internal Dictionary<Block, List<IEditable>> KeptParsAndInlines => keptParsAndInlines;
   internal int ParIndex => parIndex;
   internal FlowDocument FlowDoc => flowDoc;
   internal int OrigSelectionStart = origSelectionStart;
   
   public int UndoEditOffset => undoEditOffset;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {
         FlowDoc.RestoreDeletedBlocks(KeptParsAndInlines, ParIndex);

         FlowDoc.Selection.Start = 0;  //??? why necessary for cursor?
         FlowDoc.Selection.End = 0;
         FlowDoc.Selection.Start = OrigSelectionStart;
         FlowDoc.Selection.End = OrigSelectionStart;

         FlowDoc.UpdateSelection();
      }
      catch { Debug.WriteLine("Failed DeleteRangeUndo at ParIndex: " + ParIndex); }
   }

}


internal class InsertParagraphUndo (Dictionary<Block, List<IEditable>> keptParsAndInlines, int insertedParIndex, FlowDocument flowDoc, int origSelectionStart, int undoEditOffset) : IUndo
{
   internal int InsertedParIndex => insertedParIndex;
   Dictionary<Block, List<IEditable>> KeptParsAndInlines => keptParsAndInlines;
   internal FlowDocument FlowDoc => flowDoc;
   internal int OrigSelectionStart = origSelectionStart;
   public int UndoEditOffset => undoEditOffset;
   public bool UpdateTextRanges => true;

   public void PerformUndo()
   {
      try
      {
         Block addedBlock = FlowDoc.Blocks[InsertedParIndex];
         if (addedBlock.IsParagraph)
         {
            ((Paragraph)addedBlock).Inlines.Clear();
            ((Paragraph)addedBlock).RequestInlinesUpdate = true;
         }
         FlowDoc.Blocks.Remove(addedBlock);
                  
         Block thisBlock = FlowDoc.Blocks[InsertedParIndex];
         if (thisBlock.IsParagraph)
         {
            Paragraph? thisPar = (Paragraph)thisBlock;
            thisPar.Inlines.Clear();
            thisPar.RequestInlinesUpdate = true;
         }

         FlowDoc.RestoreDeletedBlocks(KeptParsAndInlines, InsertedParIndex);

         FlowDoc.Selection.Start = 0;         
         FlowDoc.Selection.End = 0;         
         FlowDoc.Selection.Start = OrigSelectionStart;
         FlowDoc.Selection.CollapseToStart();

         FlowDoc.UpdateSelection();

      }
      catch { Debug.WriteLine("Failed InsertParagraphUndo at InsertedIndex: " + InsertedParIndex); }

   }
}

internal class MergeParagraphUndo (int mergedCharIndex, FlowDocument flowDoc) : IUndo 
{
   internal int MergedCharIndex => mergedCharIndex;
   internal FlowDocument FlowDoc => flowDoc;
   public int UndoEditOffset => 0;
   public bool UpdateTextRanges => false;

   public void PerformUndo()
   {
      try
      {
         FlowDoc.Selection.Start = MergedCharIndex;
         FlowDoc.Selection.End = MergedCharIndex;
         FlowDoc.InsertParagraph(false);
         FlowDoc.UpdateBlockAndInlineStarts(FlowDoc.GetContainingParagraph(mergedCharIndex));
      }
      catch { Debug.WriteLine("Failed MergeParagraphUndo at MergedCharIndex: " + MergedCharIndex); }
   }
}


internal class ApplyFormattingUndo (FlowDocument flowDoc, List<IEditablePropertyAssociation> propertyAssociations, int originalSelection, TextRange tRange) : IUndo 
{
   internal TextRange TRange => tRange;
   internal FlowDocument FlowDoc => flowDoc;
   List<IEditablePropertyAssociation>  PropertyAssociations => propertyAssociations;
   int OriginalSelection => originalSelection;
   public int UndoEditOffset => 0;
   public bool UpdateTextRanges => false;

   public void PerformUndo()
   {

      foreach (IEditablePropertyAssociation propassoc in PropertyAssociations)
         if (propassoc.FormatRun != null)
            FlowDoc.ApplyFormattingInline(propassoc.FormatRun, propassoc.InlineItem, propassoc.PropertyValue);

      foreach (Paragraph p in FlowDoc.GetRangeBlocks(TRange).Where(b=>b.IsParagraph))
         p.RequestInlinesUpdate = true;
      
      FlowDoc.Selection.Start = OriginalSelection;
      FlowDoc.Selection.End = OriginalSelection;

   }
}


