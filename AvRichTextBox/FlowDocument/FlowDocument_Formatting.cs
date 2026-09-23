using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using SkiaSharp;

namespace AvRichTextBox;

public partial class FlowDocument
{
    Dictionary<AvaloniaProperty, FormatRunsAction> formatRunsActions = [];

    private void DefineFormatRunActions()
    {
        formatRunsActions = new Dictionary<AvaloniaProperty, FormatRunsAction>
       {
           { Inline.FontFamilyProperty, ApplyFontFamilyRuns },
           { Inline.FontWeightProperty, ApplyBoldRuns },
           { Inline.FontStyleProperty, ApplyItalicRuns },
           { Inline.TextDecorationsProperty, ApplyTextDecorationRuns },
           { Inline.FontSizeProperty, ApplyFontSizeRuns },
           { Inline.BackgroundProperty, ApplyBackgroundRuns },
           { Inline.ForegroundProperty, ApplyForegroundRuns },
           { Inline.FontStretchProperty, ApplyFontStretchRuns },
           { Inline.BaselineAlignmentProperty, ApplyBaselineAlignmentRuns }
       };
    }


    bool CheckForInsertRunMode(EditableRun erun)
    {
        return 
            (UnderliningOn && erun.TextDecorations != TextDecorations.Underline) ||
            (BoldOn && erun.FontWeight != FontWeight.Bold) ||
            (ItalicOn && erun.FontStyle != FontStyle.Italic);

    }


    bool BoldOn = false;
    bool ItalicOn = false;
    bool UnderliningOn = false;

    private bool InsertRunMode = false;
    private ToggleFormatRun? toggleFormatRun;

    private delegate void ToggleFormatRun(IEditable ied);
    // used for inline toggling while typing
    private void ToggleInlineApplyBold(IEditable ied) { if (ied is EditableRun erun) { erun.FontWeight = BoldOn ? FontWeight.Bold : FontWeight.Normal; } }
    private void ToggleInlineApplyItalic(IEditable ied) { if (ied is EditableRun erun) { erun.FontStyle = ItalicOn ? FontStyle.Italic : FontStyle.Normal; } }
    private void ToggleInlineApplyUnderline(IEditable ied) { if (ied is EditableRun erun) { erun.TextDecorations = UnderliningOn ? TextDecorations.Underline : erun.TextDecorations; } }


    internal void ToggleUnderlining()
    {
        if (Selection.Length == 0)
        {
            UnderliningOn = !UnderliningOn;
            toggleFormatRun = ToggleInlineApplyUnderline;

            SetInsertRunMode();
        }
        else
            Selection.ApplyFormatting(Inline.TextDecorationsProperty, TextDecorationLocation.Underline);
    }

    internal void ToggleItalic()
    {
        if (Selection.Length == 0)
        {
            ItalicOn = !ItalicOn;
            toggleFormatRun = ToggleInlineApplyItalic;

            SetInsertRunMode();
        }
        else
            Selection.ApplyFormatting(Inline.FontStyleProperty, FontStyle.Italic);

    }

    internal void ToggleBold()
    {
        if (Selection.Length == 0)
        {
            BoldOn = !BoldOn;
            toggleFormatRun = ToggleInlineApplyBold;
            
            SetInsertRunMode();
        }
        else
            Selection.ApplyFormatting(Inline.FontWeightProperty, FontWeight.Bold);

    }

    void SetInsertRunMode()
    {
        InsertRunMode = true;

        if (Selection.StartInline is IEditable startInline)
        {
            if (startInline != Selection.StartParagraph.Inlines.Last() && GetCharPosInInline(startInline, Selection.Start) == startInline.InlineText.Length)
            {
                IEditable nextInline = Selection.StartParagraph.Inlines[Selection.StartParagraph.Inlines.IndexOf(startInline) + 1];
                if (nextInline is EditableRun nextrun)
                    InsertRunMode = CheckForInsertRunMode(nextrun);
                else
                    InsertRunMode = false;

                Selection.BiasForwardStart = !InsertRunMode;
            }
        }
    }


    internal void ApplyFormattingRange(AvaloniaProperty avProperty, object? newValue, TextRange textRange)
    {
        DisableUndoStack = true;

        (List<IEditable> createdInlines, (int idLeft, int idRight) edgeIds) createdInlinesResult = GetTextRangeInlines(textRange, addToDoc: true);
        List<IEditable> newInlines = createdInlinesResult.createdInlines;
        (int idLeft, int idRight) edgeIds = createdInlinesResult.edgeIds;

        //Debug.WriteLine("\nnewlines created:\n" + string.Join("\n", newInlines.ConvertAll(il=> il.InlineText + " :: " + il.Id + "\nEdge ids = L: " + edgeIds.idLeft + ", R: " + edgeIds.idRight)));  

        //create property association for undo 
        List<EditablePropertyAssociation> propertyAssociations = [];
        foreach (EditableRun erun in newInlines.OfType<EditableRun>())
        {   
            EditablePropertyAssociation edPropAssoc = new(erun.MyParagraphId, erun.Id, null!, erun.GetPropertyChangedObservable(avProperty), newValue);
            propertyAssociations.Add(edPropAssoc);

            if (formatRunsActions.TryGetValue(avProperty, out var runsAction))
                edPropAssoc.FormatRuns = runsAction;

            if (erun.GetValue(avProperty) is object o)
            {
                edPropAssoc.OrigPropertyValue = o;
            }
        }

        this.Undos.Add(new ApplyFormattingUndo(this, propertyAssociations, edgeIds, Selection.Start, textRange));


        if (formatRunsActions.TryGetValue(avProperty, out var applyToRunsAction))
            applyToRunsAction(newInlines, newValue);
        else
            throw new NotSupportedException($"Formatting for {avProperty.Name} is not supported.");

        //UpdateBlockAndInlineStarts(AllParagraphs.IndexOf(AllParagraphs.LastOrDefault(p => p.StartInDoc <= textRange.Start)!));

        foreach (Paragraph p in GetOverlappingParagraphsInRange(textRange, textRange.BiasForwardEnd).OfType<Paragraph>())
            p.CallRequestInlinesUpdate();

        DisableUndoStack = false;

        Selection.BiasForwardStart = true;
        Selection.BiasForwardEnd = true;

        if (GetContainingParagraph(Selection.Start) is Paragraph startPar)
        {
            Selection.StartParagraph = startPar;
            Selection.StartParagraph.SelectionStartInBlock = Selection.Start - Selection.StartParagraph.StartInDoc;
            Selection.EndParagraph.SelectionEndInBlock = Selection.End - Selection.EndParagraph.StartInDoc;
        }

        UpdateSelectedParagraphs();


        // Finally must update the selection rectangles/caret size for some formatting changes (bold, newFontsize, etc.)
        if (textRange == Selection)
        {
            Dispatcher.UIThread.Post(() => { SelectionChanged?.Invoke(Selection); }, DispatcherPriority.Background);
        }

    }

    internal void ApplyFormattingInlines(FormatRunsAction? formatRunsAction, List<IEditable> inlineItems, object? newValue)
    {
        formatRunsAction?.Invoke(inlineItems, newValue);
        Selection.BiasForwardStart = true;
        Selection.BiasForwardEnd = true;

    }

    internal delegate void FormatRunsAction(List<IEditable> ieds, object? newValue);

    private void ApplyFontFamilyRuns(List<IEditable> ieds, object? newFontfamily)
    {
        if (newFontfamily is not FontFamily applyFontFamily) return;

        foreach (IEditable ied in ieds)
            if (ied is EditableRun edrun) { edrun.FontFamily = applyFontFamily; }
    }

    private void ApplyBoldRuns(List<IEditable> ieds, object? newFontWeight)
    {
        if (newFontWeight is not FontWeight applyFontWeight) return;

        // if all of the eRuns are bold, force style to Normal
        if (newFontWeight is FontWeight.Bold)
            applyFontWeight = ieds.All(ar => ar is EditableRun edrun && edrun.FontWeight == FontWeight.Bold) ? FontWeight.Normal : FontWeight.Bold;
        // if all of the eRuns are normal, force style to Bold
        else if (newFontWeight is FontWeight.Normal)
            applyFontWeight = ieds.All(ar => ar is EditableRun edrun && edrun.FontWeight == FontWeight.Normal) ? FontWeight.Bold : FontWeight.Normal;


        foreach (EditableRun erun in ieds.OfType<EditableRun>())
            erun.FontWeight = applyFontWeight;

    }

    private void ApplyItalicRuns(List<IEditable> ieds, object? newFontstyle)
    {
        if (newFontstyle is not FontStyle applyFontStyle) return;

        // if all of the eRuns are italic, force style to Normal
        if (newFontstyle is FontStyle.Italic)
            applyFontStyle = ieds.All(ar => ar is EditableRun edrun && edrun.FontStyle == FontStyle.Italic) ? FontStyle.Normal : FontStyle.Italic;
        // if all of the eRuns are normal, force style to Italic
        else if (newFontstyle is FontStyle.Normal)
            applyFontStyle = ieds.All(ar => ar is EditableRun edrun && edrun.FontStyle == FontStyle.Normal) ? FontStyle.Italic : FontStyle.Normal;
        // otherwise (if mixed), apply italic to all eRuns
        foreach (EditableRun erun in ieds.OfType<EditableRun>())
            erun.FontStyle = applyFontStyle;


    }


    private void ApplyTextDecorationRuns(List<IEditable> ieds, object? newTextDecorationLocation)
    {
        if (newTextDecorationLocation is not TextDecorationLocation applyTextDecorationLocation)
            return;

        var eRuns = ieds.OfType<EditableRun>().ToList();

        bool remove = eRuns.All(r => r.TextDecorations?.Any(d => d.Location.HasFlag(applyTextDecorationLocation)) ?? false);

        foreach (var erun in eRuns)
        {
            var decorations = erun.TextDecorations == null ? new TextDecorationCollection() : [.. erun.TextDecorations];

            if (remove)
            {
                decorations.RemoveAll([.. decorations.Where(d => d.Location == applyTextDecorationLocation)]);
            }
            else if (!decorations.Any(d => d.Location == applyTextDecorationLocation))
            {
                decorations.Add(new TextDecoration { Location = applyTextDecorationLocation });
            }

            erun.TextDecorations = decorations.Count == 0 ? null : decorations;
        }
    }
      
    private void ApplyFontSizeRuns(List<IEditable> ieds, object? newFontsize)
    {
        if (newFontsize is not double applyFontSize) return;

        foreach (IEditable ied in ieds)
            if (ied is EditableRun edrun) { edrun.FontSize = applyFontSize; }
    }

    private void ApplyBackgroundRuns(List<IEditable> ieds, object? newBackground)
    {
        ISolidColorBrush applyBrush = Brushes.Transparent;
        if (newBackground is ISolidColorBrush solidBrush)
            applyBrush = solidBrush;

        foreach (IEditable ied in ieds)
            if (ied is EditableRun edrun) { edrun.Background = applyBrush; }

    }

    private void ApplyForegroundRuns(List<IEditable> ieds, object? newForeground)
    {
        ISolidColorBrush applyBrush = Brushes.Transparent;
        if (newForeground is SolidColorBrush solidBrush)
            applyBrush = solidBrush;
        foreach (IEditable ied in ieds)
            if (ied is EditableRun edrun) { edrun.Foreground = applyBrush; }

    }

    private void ApplyFontStretchRuns(List<IEditable> ieds, object? newFontstretch)
    {
        if (newFontstretch is not FontStretch applyFontStretch) return;
        foreach (IEditable ied in ieds)
            if (ied is EditableRun edrun) { edrun.FontStretch = applyFontStretch; }
    }

    private void ApplyBaselineAlignmentRuns(List<IEditable> ieds, object? newBaselinealignment)
    {
        if (newBaselinealignment is not BaselineAlignment applyBaselineAlignment) return;
        foreach (IEditable ied in ieds)
            if (ied is EditableRun edrun) { edrun.BaselineAlignment = applyBaselineAlignment; }
    }

    internal void ResetInsertFormatting()
    {
        InsertRunMode = false;
        BoldOn = false;
        ItalicOn = false;
        UnderliningOn = false;

    }

    internal static void CopyRunPropsToHyperlinkText(EditableRun linkRun, ref EditableHyperlink elink)
    {
        elink.LinkDisplayText = linkRun.Text!;
        elink.FontStyle = linkRun.FontStyle;
        elink.FontWeight = linkRun.FontWeight;
        elink.TextDecorations = linkRun.TextDecorations;
        elink.FontSize = linkRun.FontSize;
        elink.FontFamily = linkRun.FontFamily;
        elink.Background = linkRun.Background;
        elink.BaselineAlignment = linkRun.BaselineAlignment;
        elink.Foreground = linkRun.Foreground;
    }

  
}

