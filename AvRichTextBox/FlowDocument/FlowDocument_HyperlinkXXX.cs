//namespace AvRichTextBox;

//public partial class FlowDocument
//{
//    /// <summary>
//    /// Returns the hyperlink that contains (or starts at) the current selection start, or null if the caret/selection is not inside a hyperlink.
//    /// </summary>
//    internal EditableHyperlink? GetHyperlinkAtSelection() { return GetHyperlinkAtCharIndex(Selection.Start); }

//    /// <summary>
//    /// Returns the hyperlink that contains (or starts at) the specified character index, or null if the charIndex is not inside a hyperlink.
//    /// </summary>
//    public EditableHyperlink? GetHyperlinkAtCharIndex(int charIndex)
//    {
//        if (GetStartInline(charIndex) is EditableHyperlink hl2)
//            return hl2;

//        return null;
//    }
    
//    internal void InsertOrUpdateHyperlink(bool IsSelection, int charIndex, string displayText, string navigateUri)
//    {

//    }
  
//    internal void InsertOrUpdateHyperlinkAt(bool IsSelection, int charIndex, string displayText, string navigateUri)
//    {
//        if (string.IsNullOrWhiteSpace(navigateUri)) return;

//        // Normalize URI – add https:// scheme if none is present
//        if (!navigateUri.Contains("://") && !navigateUri.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
//            navigateUri = "https://" + navigateUri;

//        // ── Case 1: caret is inside an existing hyperlink → update in place ──────
//        if (GetHyperlinkAtCharIndex(charIndex) is EditableHyperlink existingHyperlink)
//        {
//            Paragraph par = GetContainingParagraph(charIndex);
//            if (par == null) return;

//            // Snapshot before edit for undo
//            Paragraph parClone = par.FullClone(false);
//            int parIndex = AllParagraphs.IndexOf(par);
//            int updateOrigCharIndex = charIndex;
//            int oldLength = existingHyperlink.InlineLength;

//            DisableRunTextUndo = true;

//            existingHyperlink.NavigateUri = navigateUri;
//            existingHyperlink.Text = displayText;

//            par.CallRequestInlinesUpdate();
//            UpdateBlockAndInlineStarts(par);

//            int newLength = existingHyperlink.InlineLength;
//            int lengthDelta = newLength - oldLength;

//            if (lengthDelta != 0)
//                UpdateTextRanges(par.StartInDoc + existingHyperlink.TextPositionOfInlineInParagraph, lengthDelta);

//            DisableRunTextUndo = false;

//            Undos.Add(new HyperlinkParagraphUndo(parClone, parIndex, this, updateOrigCharIndex, -lengthDelta));
//            return;
//        }

//        // ── Case 2: insert new hyperlink (replace selection or insert at caret) ──
//        Paragraph? currentPar =  Selection.GetStartPar();
//        if (currentPar == null) return;

//        // Snapshot the affected paragraphs before any edit for undo.
//        // When there is a selection that may span multiple paragraphs we need all of them.
//        List<Block> affectedBlockClones = GetOverlappingBlocksInRange(Selection).ConvertAll(b => b.FullClone(true));
//        int firstParIndex = AllParagraphs.IndexOf(currentPar);
//        int origCharIndex = charIndex;
//        bool firstParWasDeleted = false;

//        DisableRunTextUndo = true;

//        if (IsSelection && Selection.Length > 0)
//        {
//            // DeleteRange may collapse multiple paragraphs into one; track whether the first par is gone
//            bool firstParEmpty = currentPar.Inlines.Count == 1 && currentPar.Inlines[0] is EditableRun er && er.Text == "";
//            firstParWasDeleted = currentPar.StartInDoc == Selection.Start && currentPar.EndInDoc <= Selection.End && !firstParEmpty;

//            DeleteRange(Selection, false, false);
//            Selection.CollapseToStart();
//            SelectionExtendMode = ExtendMode.ExtendModeNone;
//        }

//        // Re-resolve the paragraph after possible deletion
//        currentPar = GetContainingParagraph(charIndex);
//        if (currentPar == null) { DisableRunTextUndo = false; return; }

//        if (GetStartInline(charIndex) is not IEditable insertAfterInline)
//        { DisableRunTextUndo = false; return; }

//        IEditable leftRun;
//        int insertIdx;

//        if (insertAfterInline.IsLineBreak || insertAfterInline is EditableInlineUIContainer)
//        {
//            // Cannot split a line break or UI container — insert the hyperlink before it instead.
//            int nonSplittableIdx = currentPar.Inlines.IndexOf(insertAfterInline);
//            if (nonSplittableIdx > 0)
//            {
//                leftRun = currentPar.Inlines[nonSplittableIdx - 1];
//            }
//            else
//            {
//                // No preceding inline exists; create an empty run as a left anchor.
//                var emptyAnchor = new EditableRun("") { MyParagraphId = currentPar.Id, MyFlowDoc = this };
//                currentPar.Inlines.Insert(0, emptyAnchor);
//                nonSplittableIdx = 1;
//                leftRun = emptyAnchor;
//            }
//            insertIdx = nonSplittableIdx;
//        }
//        else
//        {
//            int charPosInInline = GetCharPosInInline(insertAfterInline, charIndex);

//            // Split the run at the caret so we can inject the hyperlink inline
//            List<IEditable> splitRuns = SplitRunAtPos(Selection.Start, insertAfterInline, charPosInInline);

//            leftRun = splitRuns[0];
//            insertIdx = currentPar.Inlines.IndexOf(leftRun) + 1;

//            bool leftWasEmpty = leftRun.InlineText == "";
//            if (leftWasEmpty && splitRuns.Count > 1)
//                insertIdx--;
//        }

//        var newHyperlink = new EditableHyperlink(displayText, navigateUri)
//        {
//            MyParagraphId = currentPar.Id,
//            MyFlowDoc = this,
//        };

//        currentPar.Inlines.Insert(insertIdx, newHyperlink);

//        if (insertAfterInline is not (EditableLineBreak or EditableInlineUIContainer) &&
//            leftRun.InlineText == "" && currentPar.Inlines.Contains(leftRun))
//            currentPar.Inlines.Remove(leftRun);

//        currentPar.CallRequestInlinesUpdate();
//        UpdateBlockAndInlineStarts(currentPar);
//        UpdateTextRanges(Selection.Start, displayText.Length);

//        // Move caret to end of inserted hyperlink
//        Select(origCharIndex + displayText.Length, 0);

//        DisableRunTextUndo = false;

//        // undoEditOffset = -(displayText.Length) so Undo moves the selection back
//        Undos.Add(new HyperlinkParagraphUndo(affectedBlockClones, firstParIndex, this, origCharIndex, -displayText.Length, firstParWasDeleted));
//    }


//    ///// <summary>
//    ///// Inserts a new hyperlink from the current selection, or updates the hyperlink the caret is currently inside.
//    ///// - If the caret is inside an existing hyperlink: update its text and URI in place.
//    ///// - If there is a text selection: replace the selected text with a hyperlink.
//    ///// - If there is no selection and no existing hyperlink: insert a new hyperlink with the given text.
//    ///// </summary>
//    //internal void InsertOrUpdateHyperlink(string displayText, string navigateUri)
//    //{
//    //    if (string.IsNullOrWhiteSpace(navigateUri)) return;

//    //    // Normalize URI – add https:// scheme if none is present
//    //    if (!navigateUri.Contains("://") && !navigateUri.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
//    //        navigateUri = "https://" + navigateUri;

//    //    // ── Case 1: caret is inside an existing hyperlink → update in place ──────
//    //    if (GetHyperlinkAtCharIndex(Selection.Start) is EditableHyperlink existingHyperlink)
//    //    {
//    //        Paragraph par = GetContainingParagraph(Selection.Start);
//    //        if (par == null) return;

//    //        // Snapshot before edit for undo
//    //        Paragraph parClone = par.FullClone(false);
//    //        int parIndex = AllParagraphs.IndexOf(par);
//    //        int updateOrigSelStart = Selection.Start;
//    //        int oldLength = existingHyperlink.InlineLength;

//    //        DisableRunTextUndo = true;

//    //        existingHyperlink.NavigateUri = navigateUri;
//    //        existingHyperlink.Text = displayText;

//    //        par.CallRequestInlinesUpdate();
//    //        UpdateBlockAndInlineStarts(par);

//    //        int newLength = existingHyperlink.InlineLength;
//    //        int lengthDelta = newLength - oldLength;

//    //        if (lengthDelta != 0)
//    //            UpdateTextRanges(par.StartInDoc + existingHyperlink.TextPositionOfInlineInParagraph, lengthDelta);

//    //        DisableRunTextUndo = false;

//    //        Undos.Add(new HyperlinkParagraphUndo(parClone, parIndex, this, updateOrigSelStart, -lengthDelta));
//    //        return;
//    //    }

//    //    // ── Case 2: insert new hyperlink (replace selection or insert at caret) ──
//    //    Paragraph? startPar = Selection.GetStartPar();
//    //    if (startPar == null) return;

//    //    // Snapshot the affected paragraphs before any edit for undo.
//    //    // When there is a selection that may span multiple paragraphs we need all of them.
//    //    List<Block> affectedBlockClones = GetOverlappingBlocksInRange(Selection).ConvertAll(b => b.FullClone(true));
//    //    int firstParIndex = AllParagraphs.IndexOf(startPar);
//    //    int origSelStart = Selection.Start;
//    //    bool firstParWasDeleted = false;

//    //    DisableRunTextUndo = true;

//    //    if (Selection.Length > 0)
//    //    {
//    //        // DeleteRange may collapse multiple paragraphs into one; track whether the first par is gone
//    //        bool firstParEmpty = startPar.Inlines.Count == 1 && startPar.Inlines[0] is EditableRun er && er.Text == "";
//    //        firstParWasDeleted = startPar.StartInDoc == Selection.Start && startPar.EndInDoc <= Selection.End && !firstParEmpty;

//    //        DeleteRange(Selection, false, false);
//    //        Selection.CollapseToStart();
//    //        SelectionExtendMode = ExtendMode.ExtendModeNone;
//    //    }

//    //    // Re-resolve the paragraph after possible deletion
//    //    startPar = GetContainingParagraph(Selection.Start);
//    //    if (startPar == null) { DisableRunTextUndo = false; return; }

//    //    if (GetStartInline(Selection.Start) is not IEditable insertAfterInline)
//    //    { DisableRunTextUndo = false; return; }

//    //    IEditable leftRun;
//    //    int insertIdx;

//    //    if (insertAfterInline.IsLineBreak || insertAfterInline is EditableInlineUIContainer)
//    //    {
//    //        // Cannot split a line break or UI container — insert the hyperlink before it instead.
//    //        int nonSplittableIdx = startPar.Inlines.IndexOf(insertAfterInline);
//    //        if (nonSplittableIdx > 0)
//    //        {
//    //            leftRun = startPar.Inlines[nonSplittableIdx - 1];
//    //        }
//    //        else
//    //        {
//    //            // No preceding inline exists; create an empty run as a left anchor.
//    //            var emptyAnchor = new EditableRun("") { MyParagraphId = startPar.Id, MyFlowDoc = this };
//    //            startPar.Inlines.Insert(0, emptyAnchor);
//    //            nonSplittableIdx = 1;
//    //            leftRun = emptyAnchor;
//    //        }
//    //        insertIdx = nonSplittableIdx;
//    //    }
//    //    else
//    //    {
//    //        int charPosInInline = GetCharPosInInline(insertAfterInline, Selection.Start);

//    //        // Split the run at the caret so we can inject the hyperlink inline
//    //        List<IEditable> splitRuns = SplitRunAtPos(Selection.Start, insertAfterInline, charPosInInline);

//    //        leftRun = splitRuns[0];
//    //        insertIdx = startPar.Inlines.IndexOf(leftRun) + 1;

//    //        bool leftWasEmpty = leftRun.InlineText == "";
//    //        if (leftWasEmpty && splitRuns.Count > 1)
//    //            insertIdx--;
//    //    }

//    //    var newHyperlink = new EditableHyperlink(displayText, navigateUri)
//    //    {
//    //        MyParagraphId = startPar.Id,
//    //        MyFlowDoc = this,
//    //    };

//    //    startPar.Inlines.Insert(insertIdx, newHyperlink);

//    //    if (insertAfterInline is not (EditableLineBreak or EditableInlineUIContainer) &&
//    //        leftRun.InlineText == "" && startPar.Inlines.Contains(leftRun))
//    //        startPar.Inlines.Remove(leftRun);

//    //    startPar.CallRequestInlinesUpdate();
//    //    UpdateBlockAndInlineStarts(startPar);
//    //    UpdateTextRanges(Selection.Start, displayText.Length);

//    //    // Move caret to end of inserted hyperlink
//    //    Select(origSelStart + displayText.Length, 0);

//    //    DisableRunTextUndo = false;

//    //    // undoEditOffset = -(displayText.Length) so Undo moves the selection back
//    //    Undos.Add(new HyperlinkParagraphUndo(affectedBlockClones, firstParIndex, this, origSelStart, -displayText.Length, firstParWasDeleted));
//    //}

//    /// <summary>
//    /// Removes the hyperlink under/at the current selection and replaces it with a plain EditableRun preserving the display text and font properties.
//    /// </summary>
//    internal void RemoveHyperlinkAtSelection()
//    {
//        if (GetHyperlinkAtCharIndex(charIndex) is not EditableHyperlink hl) return;

//        Paragraph par = GetContainingParagraph(Selection.Start);
//        if (par == null) return;

//        // Snapshot before edit for undo
//        Paragraph parClone = par.FullClone(true);
//        int parIndex = AllParagraphs.IndexOf(par);
//        int caretPos = Selection.Start;
//        int hlLength = hl.InlineLength;

//        DisableRunTextUndo = true;

//        int hlIdx = par.Inlines.IndexOf(hl);

//        // Replace hyperlink with a plain run preserving the display text and font properties
//        var replacement = new EditableRun(hl.Text ?? "")
//        {
//            FontFamily = hl.FontFamily,
//            FontWeight = hl.FontWeight,
//            FontStyle = hl.FontStyle,
//            FontSize = hl.FontSize,
//            Background = hl.Background,
//            BaselineAlignment = hl.BaselineAlignment,
//            MyParagraphId = par.Id,
//            MyFlowDoc = this,
//        };

//        par.Inlines[hlIdx] = replacement;

//        par.CallRequestInlinesUpdate();
//        UpdateBlockAndInlineStarts(par);

//        Select(caretPos, 0);

//        DisableRunTextUndo = false;

//        // The remove operation doesn't change the text length, so undoEditOffset = 0
//        Undos.Add(new HyperlinkParagraphUndo(parClone, parIndex, this, caretPos, 0));
//    }
//}
