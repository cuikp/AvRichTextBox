using Avalonia.Threading;

namespace AvRichTextBox; 

internal class InsertNewFormattedTextUndo(int parId, EditableRun removedRunClone, (int leftId, int rightId) edgeIds, int addedRunId, int deletedRunIdx, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int EditOffset { get; set; } =  1;
    public bool UpdateTextRanges => true;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    int thisParLengthBefore = 0;
    readonly List<(int, IEditable)> removedInlines = [];

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            thisParLengthBefore = thisPar.TextLength;

            removedInlines.Clear();
            if (thisPar.Inlines.FirstOrDefault(il => il.Id == edgeIds.leftId) is EditableRun leftRun)
            {
                removedInlines.Add(new(thisPar.Inlines.IndexOf(leftRun), leftRun));
                thisPar.Inlines.Remove(leftRun);
            }
            if (thisPar.Inlines.FirstOrDefault(il => il.Id == edgeIds.rightId) is EditableRun rightRun)
            {
                removedInlines.Add(new(thisPar.Inlines.IndexOf(rightRun), rightRun));
                thisPar.Inlines.Remove(rightRun);
            }
            if (thisPar.Inlines.FirstOrDefault(il => il.Id == addedRunId) is EditableRun addedRun)
            {
                removedInlines.Add(new(thisPar.Inlines.IndexOf(addedRun), addedRun));
                thisPar.Inlines.Remove(addedRun);
            }
                

            thisPar.Inlines.Insert(deletedRunIdx, removedRunClone);

            PostUpdate(thisPar);

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at delete pos: {origSelectionStart}"); }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack = true;

            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            thisParLengthBefore = thisPar.TextLength;

            thisPar.Inlines.Remove(removedRunClone);

            for (int ilno = removedInlines.Count - 1; ilno >= 0; ilno--)
            {
                (int, IEditable) removedInline = removedInlines[ilno];
                thisPar.Inlines.Insert(removedInline.Item1, removedInline.Item2);
            }
            
            PostUpdate(thisPar);

        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at insert formatted text pos: {origSelectionStart}"); }
    }

    private void PostUpdate(Paragraph thisPar)
    {
        DisableUndoStack = false;

        thisPar.CallRequestInlinesUpdate();
        flowDoc.UpdateBlockAndInlineStarts(thisPar);
        flowDoc.UpdateTextRanges(thisPar.StartInDoc, thisPar.TextLength - thisParLengthBefore);


        flowDoc.Selection.Start = origSelectionStart;
        flowDoc.Selection.End = flowDoc.Selection.Start;
    }
}


internal class ApplyFormattingUndo(FlowDocument flowDoc, List<EditablePropertyAssociation> propertyAssociations, (int LeftId, int RightId) addedEdgeIds, int originalSelection, TextRange tRange) : IEditDo
{
    public int EditOffset { get; set; } =  0;
    public bool UpdateTextRanges => false;
    public int UpdateTextRangesFromCharIdx { get; set; } = 0;
    public bool DoNextUndo => false;public bool DoNextRedo => false;

    readonly List<(int, int, IEditable, int, string)> removedInlinesInfo = [];
    int trangeStart = 0;
    int trangeEnd = 0;

    public void PerformUndo()
    {
        try
        {
            DisableUndoStack = true;

            trangeStart = tRange.Start;
            trangeEnd = tRange.End;

            List<Paragraph> allPars = flowDoc.AllParagraphs;

            foreach (EditablePropertyAssociation propassoc in propertyAssociations)
            {
                                
                if (allPars.FirstOrDefault(bl => bl.Id == propassoc.BlockId) is Paragraph p && p.Inlines.FirstOrDefault(il => il.Id == propassoc.InlineId) is EditableRun erun)
                {
                    propassoc.keptERun = erun;
                    flowDoc.ApplyFormattingInlines(propassoc.FormatRuns, [erun], propassoc.NewPropertyValue);
                }
            }

            if (allPars.FirstOrDefault(bl => bl.Id == propertyAssociations[0].BlockId) is not Paragraph firstPar ||
               firstPar.Inlines.FirstOrDefault(il => il.Id == propertyAssociations[0].InlineId) is not IEditable firstAddedInline ||
               firstPar.Inlines.FirstOrDefault(il => il.Id == addedEdgeIds.LeftId) is not IEditable ilineLeft) return;

            if (allPars.FirstOrDefault(bl => bl.Id == propertyAssociations[^1].BlockId) is not Paragraph lastPar ||
               lastPar.Inlines.FirstOrDefault(il => il.Id == propertyAssociations[^1].InlineId) is not IEditable lastAddedInline ||
               lastPar.Inlines.FirstOrDefault(il => il.Id == addedEdgeIds.RightId) is not IEditable ilineRight) return;

            removedInlinesInfo.Clear();

            if (lastAddedInline.Id > ilineRight.Id)
            {
                int lastAddedInlineIndex = lastPar.Inlines.IndexOf(lastAddedInline);
                string keepIlineRightText = ilineRight.InlineText;
                ilineRight.InlineText = lastAddedInline.InlineText + ilineRight.InlineText;
                lastPar.Inlines.Remove(lastAddedInline);
                removedInlinesInfo.Add((lastPar.Id, lastAddedInlineIndex, lastAddedInline, ilineRight.Id, keepIlineRightText));
            }

            if (ilineLeft.Id != firstAddedInline.Id)
            {
                if (propertyAssociations.Count == 1)
                {
                    if (addedEdgeIds.LeftId > addedEdgeIds.RightId)
                    {
                        int inlineLeftIndex = firstPar.Inlines.IndexOf(ilineLeft);
                        string keepIlineRightText = ilineRight.InlineText;
                        ilineRight.InlineText = ilineLeft.InlineText + ilineRight.InlineText;
                        firstPar.Inlines.Remove(ilineLeft);
                        removedInlinesInfo.Add((firstPar.Id, inlineLeftIndex, ilineLeft, ilineRight.Id, keepIlineRightText));
                    }
                }
                else
                {
                    int lowestId = Math.Min(ilineLeft.Id, firstAddedInline.Id);

                    if (ilineLeft.Id > firstAddedInline.Id)
                    {
                        string keepFirstAddedInlineText = firstAddedInline.InlineText;
                        firstAddedInline.InlineText = ilineLeft.InlineText + firstAddedInline.InlineText;
                        int inlineLeftIndex = firstPar.Inlines.IndexOf(ilineLeft);
                        firstPar.Inlines.Remove(ilineLeft);
                        firstAddedInline.Id = lowestId;
                        removedInlinesInfo.Add((firstPar.Id, inlineLeftIndex, ilineLeft, firstAddedInline.Id, keepFirstAddedInlineText));
                    }
                    else
                    {
                        int firstAddedInlineIndex = firstPar.Inlines.IndexOf(firstAddedInline);
                        string keepIlineLeftText = ilineLeft.InlineText;
                        ilineLeft.InlineText += firstAddedInline.InlineText;
                        firstPar.Inlines.Remove(firstAddedInline);
                        removedInlinesInfo.Add((firstPar.Id, firstAddedInlineIndex, firstAddedInline, ilineLeft.Id, keepIlineLeftText));
                    }
                }
            }

            PostUpdate(trangeStart, trangeEnd, lastPar);

        }

        catch { Debug.WriteLine($"Failed {this.GetType().Name} at insert formatted text pos: {originalSelection}"); }
        finally { DisableUndoStack = false; }
    }

    public void PerformRedo()
    {
        try
        {
            DisableUndoStack = true;

            foreach (EditablePropertyAssociation propassoc in propertyAssociations)
                flowDoc.ApplyFormattingInlines(propassoc.FormatRuns, [propassoc.keptERun], propassoc.NewPropertyValue);

            Paragraph lastPar = null!;
            for (int infono = removedInlinesInfo.Count - 1; infono >= 0; infono--)
            {
                (int, int, IEditable, int, string) inlineInfo = removedInlinesInfo[infono];

                if (flowDoc.AllParagraphs.FirstOrDefault(p => p.Id == inlineInfo.Item1) is Paragraph thisPar)
                {
                    thisPar.Inlines.Insert(inlineInfo.Item2, inlineInfo.Item3);
                    lastPar = thisPar;
                    if (thisPar.Inlines.FirstOrDefault(il => il.Id == inlineInfo.Item4) is EditableRun erun)
                        erun.Text = inlineInfo.Item5;
                }
            }

            PostUpdate(trangeStart, trangeEnd, lastPar);

        }

        catch { Debug.WriteLine($"Failed {this.GetType().Name} at insert formatted text pos: {originalSelection}"); }
        finally { DisableUndoStack = false; }

    }

    private void PostUpdate(int rangeStart, int rangeEnd, Paragraph lastPar)
    {

        DisableUndoStack = false;

        foreach (Paragraph p in flowDoc.GetOverlappingParagraphsInRange(rangeStart, rangeEnd, tRange.BiasForwardEnd).OfType<Paragraph>())
        {
            p.CallRequestInlinesUpdate();
            p.UpdateEditableRunPositions();
        }

        flowDoc.UpdateSelection();
        lastPar?.CallRequestInlinesUpdate();  // fail-safe necessary?

        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.Selection.Start = originalSelection;
            flowDoc.Selection.End = originalSelection;
        });
    }
}

