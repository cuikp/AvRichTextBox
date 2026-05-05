namespace AvRichTextBox;

public partial class FlowDocument
{
   /// <summary>
   /// Returns the hyperlink that contains (or starts at) the current selection start,
   /// or null if the caret/selection is not inside a hyperlink.
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
   /// Inserts a new hyperlink from the current selection, or updates the hyperlink
   /// the caret is currently inside.
   /// - If the caret is inside an existing hyperlink: update its text and URI in place.
   /// - If there is a text selection: replace the selected text with a hyperlink.
   /// - If there is no selection and no existing hyperlink: insert a new hyperlink with the given text.
   /// </summary>
   internal void InsertOrUpdateHyperlink(string displayText, string navigateUri)
   {
      if (string.IsNullOrWhiteSpace(navigateUri)) return;

      // Normalize URI – add https:// scheme if none is present
      if (!navigateUri.Contains("://") && !navigateUri.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
         navigateUri = "https://" + navigateUri;

      // ── Case 1: caret is inside an existing hyperlink → update in place ──────
      if (GetHyperlinkAtSelection() is EditableHyperlink existingHyperlink)
      {
         Paragraph par = GetContainingParagraph(Selection.Start);
         if (par == null) return;

         // Snapshot before edit for undo
         Paragraph parClone = par.FullClone();
         int parIndex = Blocks.IndexOf(par);
         int updateOrigSelStart = Selection.Start;
         int oldLength = existingHyperlink.InlineLength;

         disableRunTextUndo = true;

         existingHyperlink.NavigateUri = navigateUri;
         existingHyperlink.Text = displayText;

         par.CallRequestInlinesUpdate();
         UpdateBlockAndInlineStarts(par);

         int newLength = existingHyperlink.InlineLength;
         int lengthDelta = newLength - oldLength;

         if (lengthDelta != 0)
            UpdateTextRanges(par.StartInDoc + existingHyperlink.TextPositionOfInlineInParagraph, lengthDelta);

         disableRunTextUndo = false;

         Undos.Add(new HyperlinkParagraphUndo(parClone, parIndex, this, updateOrigSelStart, -lengthDelta));
         return;
      }

      // ── Case 2: insert new hyperlink (replace selection or insert at caret) ──
      Paragraph? startPar = Selection.GetStartPar();
      if (startPar == null) return;

      // Snapshot the affected paragraphs before any edit for undo.
      // When there is a selection that may span multiple paragraphs we need all of them.
      List<Paragraph> affectedParClones = GetOverlappingParagraphsInRange(Selection).ConvertAll(p => p.FullClone());
      int firstParIndex = Blocks.IndexOf(startPar);
      int origSelStart = Selection.Start;
      bool firstParWasDeleted = false;

      disableRunTextUndo = true;

      if (Selection.Length > 0)
      {
         // DeleteRange may collapse multiple paragraphs into one; track whether the first par is gone
         bool firstParEmpty = startPar.Inlines.Count == 1 && startPar.Inlines[0] is EditableRun er && er.Text == "";
         firstParWasDeleted = startPar.StartInDoc == Selection.Start && startPar.EndInDoc <= Selection.End && !firstParEmpty;

         DeleteRange(Selection, false, false);
         Selection.CollapseToStart();
         SelectionExtendMode = ExtendMode.ExtendModeNone;
      }

      // Re-resolve the paragraph after possible deletion
      startPar = GetContainingParagraph(Selection.Start);
      if (startPar == null) { disableRunTextUndo = false; return; }

      if (GetStartInline(Selection.Start) is not IEditable insertAfterInline)
      { disableRunTextUndo = false; return; }

      int charPosInInline = GetCharPosInInline(insertAfterInline, Selection.Start);

      // Split the run at the caret so we can inject the hyperlink inline
      List<IEditable> splitRuns = SplitRunAtPos(Selection.Start, insertAfterInline, charPosInInline);

      IEditable leftRun = splitRuns[0];
      int insertIdx = startPar.Inlines.IndexOf(leftRun) + 1;

      bool leftWasEmpty = leftRun.InlineText == "";
      if (leftWasEmpty && splitRuns.Count > 1)
         insertIdx--;

      var newHyperlink = new EditableHyperlink(displayText, navigateUri)
      {
         MyParagraphId = startPar.Id,
         MyFlowDoc = this,
      };

      startPar.Inlines.Insert(insertIdx, newHyperlink);

      if (leftWasEmpty && startPar.Inlines.Contains(leftRun))
         startPar.Inlines.Remove(leftRun);

      startPar.CallRequestInlinesUpdate();
      UpdateBlockAndInlineStarts(startPar);
      UpdateTextRanges(Selection.Start, displayText.Length);

      // Move caret to end of inserted hyperlink
      Select(Selection.Start + displayText.Length, 0);

      disableRunTextUndo = false;

      // undoEditOffset = -(displayText.Length) so Undo moves the selection back
      Undos.Add(new HyperlinkParagraphUndo(affectedParClones, firstParIndex, this, origSelStart, -displayText.Length, firstParWasDeleted));
   }

   /// <summary>
   /// Removes the hyperlink under/at the current selection and replaces it with a
   /// plain EditableRun preserving the display text and font properties.
   /// </summary>
   internal void RemoveHyperlink()
   {
      if (GetHyperlinkAtSelection() is not EditableHyperlink hl) return;

      Paragraph par = GetContainingParagraph(Selection.Start);
      if (par == null) return;

      // Snapshot before edit for undo
      Paragraph parClone = par.FullClone();
      int parIndex = Blocks.IndexOf(par);
      int caretPos = Selection.Start;
      int hlLength = hl.InlineLength;

      disableRunTextUndo = true;

      int hlIdx = par.Inlines.IndexOf(hl);

      // Replace hyperlink with a plain run preserving the display text and font properties
      var replacement = new EditableRun(hl.Text ?? "")
      {
         FontFamily = hl.FontFamily,
         FontWeight = hl.FontWeight,
         FontStyle = hl.FontStyle,
         FontSize = hl.FontSize,
         Background = hl.Background,
         BaselineAlignment = hl.BaselineAlignment,
         MyParagraphId = par.Id,
         MyFlowDoc = this,
      };

      par.Inlines[hlIdx] = replacement;

      par.CallRequestInlinesUpdate();
      UpdateBlockAndInlineStarts(par);

      Select(caretPos, 0);

      disableRunTextUndo = false;

      // The remove operation doesn't change the text length, so undoEditOffset = 0
      Undos.Add(new HyperlinkParagraphUndo(parClone, parIndex, this, caretPos, 0));
   }
}
