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

      // ── Case 1: caret is inside an existing hyperlink ─────────────────────
      if (GetHyperlinkAtSelection() is EditableHyperlink existingHyperlink)
      {
         Paragraph par = GetContainingParagraph(Selection.Start);
         if (par == null) return;

         disableRunTextUndo = true;

         existingHyperlink.NavigateUri = navigateUri;

         // Update display text only when it changed
         if (existingHyperlink.Text != displayText)
         {
            int hlIdx = par.Inlines.IndexOf(existingHyperlink);
            int oldLength = existingHyperlink.InlineLength;

            existingHyperlink.Text = displayText;

            par.CallRequestInlinesUpdate();
            UpdateBlockAndInlineStarts(par);
            int newLength = existingHyperlink.InlineLength;
            UpdateTextRanges(par.StartInDoc + existingHyperlink.TextPositionOfInlineInParagraph,
                             newLength - oldLength);
         }
         else
         {
            par.CallRequestInlinesUpdate();
         }

         disableRunTextUndo = false;
         return;
      }

      // ── Case 2: text is selected → replace selection with hyperlink ────────
      Paragraph? startPar = Selection.GetStartPar();
      if (startPar == null) return;

      disableRunTextUndo = true;

      if (Selection.Length > 0)
      {
         // Delete the selected range and collect split-point info
         var edgeIds = DeleteRange(Selection, false, false);
         Selection.CollapseToStart();
         SelectionExtendMode = ExtendMode.ExtendModeNone;
      }

      // Find where to insert after the possible delete
      if (GetStartInline(Selection.Start) is not IEditable insertAfterInline)
      {
         disableRunTextUndo = false;
         return;
      }

      startPar = GetContainingParagraph(Selection.Start);
      if (startPar == null)
      {
         disableRunTextUndo = false;
         return;
      }

      int charPosInInline = GetCharPosInInline(insertAfterInline, Selection.Start);

      // Split the run at the insert position so we can inject the hyperlink inline
      List<IEditable> splitRuns = SplitRunAtPos(Selection.Start, insertAfterInline, charPosInInline);

      // splitRuns[0] is left part, splitRuns[1] is right part (may be same object if no split needed)
      IEditable leftRun = splitRuns[0];
      int insertIdx = startPar.Inlines.IndexOf(leftRun) + 1;

      // Remove empty placeholder if the left run is empty
      bool leftWasEmpty = leftRun.InlineText == "";
      if (leftWasEmpty && splitRuns.Count > 1)
         insertIdx--;  // insert before left empty run (will be removed below)

      var newHyperlink = new EditableHyperlink(displayText, navigateUri)
      {
         MyParagraphId = startPar.Id,
         MyFlowDoc = this,
      };

      startPar.Inlines.Insert(insertIdx, newHyperlink);

      // Remove the empty left run if it was a zero-length leftover
      if (leftWasEmpty && startPar.Inlines.Contains(leftRun))
         startPar.Inlines.Remove(leftRun);

      startPar.CallRequestInlinesUpdate();
      UpdateBlockAndInlineStarts(startPar);
      UpdateTextRanges(Selection.Start, displayText.Length);

      // Move caret to end of inserted hyperlink
      Select(Selection.Start + displayText.Length, 0);

      disableRunTextUndo = false;
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

      disableRunTextUndo = true;

      int hlIdx = par.Inlines.IndexOf(hl);
      int caretPos = Selection.Start;

      // Replace hyperlink with a plain run that has the same text
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

      // Restore caret to approximately the same position
      Select(caretPos, 0);

      disableRunTextUndo = false;
   }
}
