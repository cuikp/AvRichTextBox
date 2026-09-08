using Avalonia.Threading;
using DynamicData;
using System.Collections.ObjectModel;

namespace AvRichTextBox; 


internal class InsertNewFormattedTextUndo(int parId, EditableRun removedRunClone, (int leftId, int rightId) edgeIds, int addedRunId, int deletedRunIdx, FlowDocument flowDoc, int origSelectionStart) : IEditDo
{
    public int UndoEditOffset => 1;
    public bool UpdateTextRanges => true;

    public void PerformUndo()
    {
        try
        {
            flowDoc.disableRunTextUndo = true;
            if (flowDoc.AllParagraphs.FirstOrDefault(bl => bl.Id == parId) is not Paragraph thisPar) return;

            int thisParLengthBefore = thisPar.TextLength;

            if (thisPar.Inlines.FirstOrDefault(il => il.Id == edgeIds.leftId) is EditableRun leftRun)
                thisPar.Inlines.Remove(leftRun);
            if (thisPar.Inlines.FirstOrDefault(il => il.Id == edgeIds.rightId) is EditableRun rightRun)
                thisPar.Inlines.Remove(rightRun);
            if (thisPar.Inlines.FirstOrDefault(il => il.Id == addedRunId) is EditableRun addedRun)
                thisPar.Inlines.Remove(addedRun);

            thisPar.Inlines.Insert(deletedRunIdx, removedRunClone);

            flowDoc.disableRunTextUndo = false;

            int thisParLengthAfter = thisPar.TextLength;

            thisPar.CallRequestInlinesUpdate();
            flowDoc.UpdateBlockAndInlineStarts(thisPar);
            flowDoc.UpdateTextRanges(thisPar.StartInDoc, thisParLengthAfter - thisParLengthBefore);


            flowDoc.Selection.Start = origSelectionStart;
            flowDoc.Selection.End = flowDoc.Selection.Start;
        }
        catch { Debug.WriteLine($"Failed {this.GetType().Name} at delete pos: {origSelectionStart}"); }
    }

    public void PerformRedo()
    {
    }

}


internal class ApplyFormattingUndo(FlowDocument flowDoc, List<EditablePropertyAssociation> propertyAssociations, (int LeftId, int RightId) edgeIds, int originalSelection, TextRange tRange) : IEditDo
{
    public int UndoEditOffset => 0;
    public bool UpdateTextRanges => false;

    public void PerformUndo()
    {
        flowDoc.disableRunTextUndo = true;

        int rangeStart = tRange.Start;
        int rangeEnd = tRange.End;

        List<Paragraph> allPars = flowDoc.AllParagraphs;

        foreach (EditablePropertyAssociation propassoc in propertyAssociations)
        {
            if (allPars.FirstOrDefault(bl => bl.Id == propassoc.BlockId) is Paragraph p && p.Inlines.FirstOrDefault(il => il.Id == propassoc.InlineId) is IEditable iline)
                flowDoc.ApplyFormattingInlines(propassoc.FormatRuns, [iline], propassoc.PropertyValue);
        }

        if (allPars.FirstOrDefault(bl => bl.Id == propertyAssociations[0].BlockId) is not Paragraph firstPar ||
           firstPar.Inlines.FirstOrDefault(il => il.Id == propertyAssociations[0].InlineId) is not IEditable ilineFirst ||
           firstPar.Inlines.FirstOrDefault(il => il.Id == edgeIds.LeftId) is not IEditable ilineLeft) return;

        if (allPars.FirstOrDefault(bl => bl.Id == propertyAssociations[^1].BlockId) is not Paragraph lastPar ||
           lastPar.Inlines.FirstOrDefault(il => il.Id == propertyAssociations[^1].InlineId) is not IEditable ilineLast ||
           lastPar.Inlines.FirstOrDefault(il => il.Id == edgeIds.RightId) is not IEditable ilineRight) return;

        if (ilineLast.Id > ilineRight.Id)
        {
            ilineRight.InlineText = ilineLast.InlineText + ilineRight.InlineText;
            lastPar.Inlines.Remove(ilineLast);
        }

        if (ilineLeft.Id != ilineFirst.Id)
        {
            if (propertyAssociations.Count == 1)
            {
                if (edgeIds.LeftId > edgeIds.RightId)
                {
                    ilineRight.InlineText = ilineLeft.InlineText + ilineRight.InlineText;
                    firstPar.Inlines.Remove(ilineLeft);
                }
            }
            else
            {
                int lowestId = Math.Min(ilineLeft.Id, ilineFirst.Id);

                if (ilineLeft.Id > ilineFirst.Id)
                {
                    ilineFirst.InlineText = ilineLeft.InlineText + ilineFirst.InlineText;
                    firstPar.Inlines.Remove(ilineLeft);
                    ilineFirst.Id = lowestId;
                }
                else
                {
                    ilineLeft.InlineText += ilineFirst.InlineText;
                    firstPar.Inlines.Remove(ilineFirst);
                }
            }
        }

        flowDoc.disableRunTextUndo = false;

        foreach (Paragraph p in flowDoc.GetOverlappingParagraphsInRange(rangeStart, rangeEnd, tRange.BiasForwardEnd).OfType<Paragraph>())
        {
            p.CallRequestInlinesUpdate();
            p.UpdateEditableRunPositions();
        }

        flowDoc.UpdateSelection();
        lastPar.CallRequestInlinesUpdate();  // fail-safe

        Dispatcher.UIThread.Post(() =>
        {
            flowDoc.Selection.Start = originalSelection;
            flowDoc.Selection.End = originalSelection;
        });

    }

    public void PerformRedo()
    {
    }

}

