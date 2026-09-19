using System.Collections.ObjectModel;

namespace AvRichTextBox;

public partial class FlowDocument
{
   /// <summary>
   /// Returns the hyperlink that contains (or starts at) the current selection start, or null if the caret/selection is not inside a hyperlink.
   /// </summary>
   internal EditableHyperlink? GetHyperlinkAtSelection()
   {
      if (Selection.StartInline is EditableHyperlink hl)
         return hl;

      // Also check when caret is right at the boundary after a hyperlink
      if (GetStartInline(Selection.Start) is EditableHyperlink hl2)
         return hl2;

      return null;
   }


    /// <summary>
    /// Inserts hyperlink at the specified range
    /// If text is selected, replace selected text with the new hyperlink.
    /// </summary>
    public void InsertHyperlinkAt(TextRange insertAtRange, EditableHyperlink newHyperlink)
    {
        if (GetStartInline(insertAtRange.Start) is not IEditable startInline || insertAtRange.GetStartPar() is not Paragraph destStartPar)
            return;

        newHyperlink.MyParagraphId = destStartPar.Id;
        newHyperlink.MyFlowDoc = destStartPar.MyFlowDoc;
          
        // Snapshot the affected paragraphs before any edit for undo.
        List<Block> affectedBlockClones = GetOverlappingBlocksInRange(insertAtRange, Selection.BiasForwardEnd).ConvertAll(b => b.FullClone(true));
        int firstParIndex = AllParagraphs.IndexOf(destStartPar);
        int origSelStart = insertAtRange.Start;
        bool firstBlockWasDeleted = false;
        bool lastBlockWasDeleted = false;
        int deleteRangeLength = insertAtRange.Length;
        int owningTableId = destStartPar.OwningTable == null ? -1 : destStartPar.OwningTable.Id;
        int owningCellId = destStartPar.OwningCell == null ? -1 : destStartPar.OwningCell.Id;

        int insertParIndex = -1;
        DetermineBlockCollection( destStartPar.IsCellBlock, firstParIndex, owningTableId, owningCellId, out insertParIndex);

        DisableUndoStack = true;

        bool firstParEmpty = false;

        if (insertAtRange.Length > 0)
        {
            // DeleteRange may collapse multiple paragraphs into one; track whether the first par is gone
            firstParEmpty = destStartPar.IsEmptyInlinePar; 
            firstBlockWasDeleted = destStartPar.StartInDoc == insertAtRange.Start && destStartPar.EndInDoc <= insertAtRange.End && !firstParEmpty;
            DeleteRange(insertAtRange, false, false, true);
            insertAtRange.CollapseToStart();
        }

        // Re-resolve the paragraph after possible deletion
        destStartPar = GetContainingParagraph(insertAtRange.Start);
        if (destStartPar == null) { DisableUndoStack = false; return; }


        if (GetStartInline(insertAtRange.Start) is not IEditable insertAfterInline)
            { DisableUndoStack = false; return; }

        IEditable leftRun;
        int insertIdx;

        if (insertAfterInline.IsLineBreak || insertAfterInline is EditableInlineUIContainer)
        {   // Cannot split a line break or UI container — insert the hyperlink before it instead.
            int nonSplittableIdx = destStartPar.Inlines.IndexOf(insertAfterInline);
            if (nonSplittableIdx > 0)
            {
                leftRun = destStartPar.Inlines[nonSplittableIdx - 1];
            }
            else
            {   // No preceding inline exists; create an empty run as a left anchor.
                var emptyAnchor = new EditableRun("") { MyParagraphId = destStartPar.Id, MyFlowDoc = this };
                destStartPar.Inlines.Insert(0, emptyAnchor);
                nonSplittableIdx = 1;
                leftRun = emptyAnchor;
            }
            insertIdx = nonSplittableIdx;
        }
        else
        {
            int charPosInInline = GetCharPosInInline(insertAfterInline, insertAtRange.Start);

            // Split the run at the caret so we can inject the hyperlink inline
            List<IEditable> splitRuns = SplitRunAtPos(insertAtRange.Start, insertAfterInline, charPosInInline);

            leftRun = splitRuns[0];
            insertIdx = destStartPar.Inlines.IndexOf(leftRun) + 1;

            bool leftWasEmpty = leftRun.InlineText == "";
            if (leftWasEmpty && splitRuns.Count > 1)
                insertIdx--;
        }


        destStartPar.Inlines.Insert(insertIdx, newHyperlink);

        
        if (insertAfterInline is not (EditableLineBreak or EditableInlineUIContainer) &&
            leftRun.InlineText == "" && destStartPar.Inlines.Contains(leftRun))
            destStartPar.Inlines.Remove(leftRun);

        
        destStartPar.CallRequestInlinesUpdate();
        UpdateBlockAndInlineStarts(destStartPar);

        int hyperlinkTextLength = newHyperlink.LinkDisplayText.Length;
        UpdateTextRanges(insertAtRange.Start, hyperlinkTextLength);
                
                     
        Undos.Add(new InsertHyperlinkAtCharIdxUndo(
            destStartPar.Id,
            insertParIndex,
            affectedBlockClones,
            this,
            origSelStart,
            deleteRangeLength - hyperlinkTextLength,
            firstParEmpty,
            firstBlockWasDeleted,
            lastBlockWasDeleted,
            destStartPar.IsCellBlock,
            owningTableId,
            owningCellId
            ));

        DisableUndoStack = false;
    }


    public void UpdateHyperlink(EditableHyperlink existingHyperlink, string displayText, string navigateUri)
    {

        Paragraph par = GetContainingParagraph(Selection.Start);
        if (par == null) return;

        int updateOrigSelStart = Selection.Start;
        int oldLength = existingHyperlink.InlineLength;

        DisableUndoStack = true;

        string oldUri = existingHyperlink.NavigateUri;
        string oldText = existingHyperlink.LinkDisplayText;
        existingHyperlink.NavigateUri = navigateUri;
        existingHyperlink.LinkDisplayText = displayText;

        par.CallRequestInlinesUpdate();
        UpdateBlockAndInlineStarts(par);

        int newLength = existingHyperlink.InlineLength;
        int lengthDelta = newLength - oldLength;

        if (lengthDelta != 0)
            UpdateTextRanges(par.StartInDoc + existingHyperlink.TextPositionOfInlineInParagraph, lengthDelta);

        Undos.Add(new HyperlinkUpdateUndo(par.Id, existingHyperlink.Id, oldUri, navigateUri, oldText, displayText, this, updateOrigSelStart, -lengthDelta));

        DisableUndoStack = false;

    }

    /// <summary>
    /// Removes the hyperlink under/at the current selection and replaces it with a plain EditableRun preserving the display text and font properties.
    /// </summary>
   internal void RemoveHyperlinkAtSelection()
   {
      if (GetHyperlinkAtSelection() is not EditableHyperlink eHL) return;

      Paragraph par = GetContainingParagraph(Selection.Start);
      if (par == null) return;

      // Snapshot before edit for undo
      Paragraph parClone = par.FullClone(true);
      int parIndex = AllParagraphs.IndexOf(par);
      int caretPos = Selection.Start;
      int hlLength = eHL.InlineLength;

      DisableUndoStack = true;

      int hlIdx = par.Inlines.IndexOf(eHL);

      // Replace hyperlink with a plain run preserving the display text and font properties
      var replacementRun = new EditableRun(eHL.LinkDisplayText ?? "")
      {
         FontFamily = eHL.FontFamily,
         FontWeight = eHL.FontWeight,
         FontStyle = eHL.FontStyle,
         FontSize = eHL.FontSize,
         Background = eHL.Background,
         BaselineAlignment = eHL.BaselineAlignment,
         MyParagraphId = par.Id,
         IsAttachedToDocument = eHL.IsAttachedToDocument,
         IsTableCellInline = eHL.IsTableCellInline,
         IsLastInlineOfParagraph = eHL.IsLastInlineOfParagraph,
         TextPositionOfInlineInParagraph = eHL.TextPositionOfInlineInParagraph,
         MyFlowDoc = this,
      };

      par.Inlines[hlIdx] = replacementRun;

      par.CallRequestInlinesUpdate();
      UpdateBlockAndInlineStarts(par);

      Select(caretPos, 0);

      DisableUndoStack = false;

      // The remove operation doesn't change the text length
      Undos.Add(new RemoveHyperlinkUndo(parClone.Id, eHL.CloneWithId(), replacementRun.Id, this));

   }

}
