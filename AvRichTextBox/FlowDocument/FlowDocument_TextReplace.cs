namespace AvRichTextBox;

// Headless text replacement (load RTF, select a plain-text range, replace it, save RTF) for hosts that use the
// document as an RTF tool without ever showing a RichTextBox, e.g. a search-and-replace over stored RTF texts.
//
// Why not SetRangeToText: it deletes the range first and only THEN reads StartInline (early exit if null). In
// a shown editor UpdateSelection fills the context in between - it requests it from the rendered paragraph via
// CallRequestTextLayoutInfoStart. Without a control nobody answers, StartInline stays empty and the
// replacement silently does not happen.
//
// UpdateContextStart and UpdateContextEnd derive the same context from the document model alone (AllParagraphs,
// StartInDoc, TextPositionOfInlineInParagraph) and need no layout. This file therefore runs the same sequence
// as SetRangeToText but refreshes the context again AFTER deleting. The formatting around stays intact because
// the new run takes over the formatting of the replaced run (same property copy as in SetRangeToText).
public partial class FlowDocument
{
    /// <summary>
    /// Loads RTF into the document without a control. LoadRtf itself is internal and reachable only through
    /// the RichTextBox.
    /// </summary>
    public void LoadRtfHeadless(string rtf)
    {
        LoadRtf(rtf ?? "");
    }

    /// <summary>The document as RTF, without a control.</summary>
    public string SaveRtfHeadless()
    {
        return SaveRtf();
    }

    /// <summary>
    /// Selects length characters from start in plain-text coordinates and resolves the context from the model.
    /// Meant for headless use; a shown control keeps using Select.
    /// </summary>
    public void SelectHeadless(int start, int length)
    {
        UpdateBlockAndInlineStarts(0);

        Selection.Start = start;
        Selection.End = start + length;

        RefreshSelectionContext();
    }

    /// <summary>
    /// Replaces the selected range with newText, without layout and without a control. Returns false if the range
    /// cannot be resolved in the document model; the document then stays unchanged.
    /// </summary>
    public bool ReplaceSelectionWithText(string newText)
    {
        if (Selection == null)
        {
            return false;
        }

        UpdateBlockAndInlineStarts(0);
        RefreshSelectionContext();

        if (Selection.StartParagraph == null || Selection.StartInline == null)
        {
            return false;
        }

        // The formatting of the REPLACED range is the template for the new text, as in RichEdit. It has to be
        // taken BEFORE deleting: afterwards the refreshed context points to the following run, whose formatting
        // would be wrong (for "bold before / normal old / bold after" the replacement would become bold).
        EditableRun? template = Selection.StartInline as EditableRun;

        if (Selection.Length > 0)
        {
            DeleteRange(Selection, false, false, false);
            Selection.CollapseToStart();
            SelectionExtendMode = ExtendMode.ExtendModeNone;

            // The point where SetRangeToText fails headless: after deleting the context is gone.
            UpdateBlockAndInlineStarts(0);
            RefreshSelectionContext();
        }

        if (Selection.StartParagraph is not Paragraph startPar || Selection.StartInline is not IEditable startInline)
        {
            return false;
        }

        List<IEditable> splitInlines = SplitRunAtPos(
            Selection.Start, startInline, GetCharPosInInline(startInline, Selection.Start));

        int startInlineIndex = startPar.Inlines.IndexOf(splitInlines[0]) + 1;

        if (splitInlines[0] is EditableRun sRun)
        {
            // The template is the run of the replaced range; only if it is missing the run at the cut applies.
            EditableRun formatSource = template ?? sRun;

            EditableRun newRun = new(newText ?? "")
            {
                FontFamily = formatSource.FontFamily,
                FontWeight = formatSource.FontWeight,
                FontStyle = formatSource.FontStyle,
                FontSize = formatSource.FontSize,
                TextDecorations = formatSource.TextDecorations,
                Background = formatSource.Background,
                BaselineAlignment = formatSource.BaselineAlignment,
                Foreground = formatSource.Foreground,
            };

            startPar.Inlines.Insert(startInlineIndex, newRun);

            if (splitInlines[0].InlineText == "")
            {
                startPar.Inlines.Remove(splitInlines[0]);
            }
        }

        UpdateBlockAndInlineStarts(startPar);
        return true;
    }

    private void RefreshSelectionContext()
    {
        Selection.UpdateContextStart();
        Selection.UpdateContextEnd();
    }

    /// <summary>
    /// State report for debugging the headless path: paragraphs, inlines and the resolved selection context.
    /// Reads only, changes nothing.
    /// </summary>
    public string DescribeState()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Text=" + (Text ?? "").Replace("\n", "\\n"));
        sb.AppendLine("Blocks=" + Blocks.Count + " AllParagraphs=" + AllParagraphs.Count
                      + " DocEndPoint=" + DocEndPoint);
        sb.AppendLine("Selection.Start=" + Selection.Start + " End=" + Selection.End
                      + " Length=" + Selection.Length);
        sb.AppendLine("StartParagraph=" + (Selection.StartParagraph == null ? "null" : "ok")
                      + " StartInline=" + (Selection.StartInline == null
                          ? "null"
                          : Selection.StartInline.GetType().Name + "[" + Selection.StartInline.InlineText + "]"));
        var index = 0;
        foreach (var p in AllParagraphs)
        {
            sb.AppendLine("  Par" + index + " StartInDoc=" + p.StartInDoc + " EndInDoc=" + p.EndInDoc
                          + " Inlines=" + p.Inlines.Count);
            foreach (var ied in p.Inlines)
            {
                sb.Append("    " + ied.GetType().Name + " pos=" + ied.TextPositionOfInlineInParagraph
                          + " text=[" + ied.InlineText + "]");
                if (ied is EditableRun run)
                {
                    sb.Append(" FontWeight=" + run.FontWeight + " FontStyle=" + run.FontStyle);
                }

                sb.AppendLine();
            }

            index++;
        }

        return sb.ToString();
    }
}
