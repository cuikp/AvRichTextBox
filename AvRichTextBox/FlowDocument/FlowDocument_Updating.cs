using Avalonia.Threading;
using DynamicData;
using System.Reactive.Linq;

namespace AvRichTextBox;

public partial class FlowDocument
{
    internal void UpdateSelection()
    {
        Selection.StartParagraph.CallRequestInlinesUpdate();
        Selection.StartParagraph.CallRequestTextLayoutInfoStart();
        Selection.EndParagraph.CallRequestInlinesUpdate();
        Selection.EndParagraph.CallRequestTextLayoutInfoEnd();
        Selection.StartParagraph.CallRequestTextBoxFocus();

        UpdateBlockAndInlineStarts(Selection.StartParagraph);
    }

    internal void UpdateBlockAndInlineStarts(int fromBlockIndex)
    {
        //if (fromBlockIndex >= Blocks.Count || fromBlockIndex < 0) return;
        if (fromBlockIndex >= Blocks.Count) return;

        int blockSum = fromBlockIndex == 0 ? 0 : Blocks[fromBlockIndex - 1].StartInDoc + Blocks[fromBlockIndex - 1].BlockLength;

        for (int blockIndex = fromBlockIndex; blockIndex < Blocks.Count; blockIndex++)
        {
            Blocks[blockIndex].StartInDoc = blockSum;

            switch (Blocks[blockIndex])
            {
                case Paragraph thisPar:
                    thisPar.UpdateEditableRunPositions();
                    break;

                case Table thisTable:   // TODO: pass paragraph index and only update starting from that paragraph inside the table

                    int innerSum = 0;

                    foreach (Cell c in thisTable.Cells)
                    {
                        foreach (Block b in c.CellBlocks)
                        {
                            b.StartInDoc = blockSum + innerSum;

                            if (b is Paragraph p)
                            {
                                //Debug.WriteLine("updating paragraph : " + p.Text.TrimEnd("\r\n".ToArray()));
                                p.UpdateEditableRunPositions();
                            }

                            innerSum += b.BlockLength;
                        }
                    }
                    break;
            }

            blockSum += (Blocks[blockIndex].BlockLength);

        }


    }

    internal void UpdateBlockAndInlineStarts(Paragraph updateStartParagraph)
    {
        int fromBlockIndex = -1;

        // if the StartParagraph is in a table, update from its owning table
        if (updateStartParagraph.IsTableCellBlock) 
            fromBlockIndex = Blocks.IndexOf(updateStartParagraph.OwningTable);
        else
            fromBlockIndex = Blocks.IndexOf(updateStartParagraph);


        if (fromBlockIndex > -1)
            UpdateBlockAndInlineStarts(fromBlockIndex);

    }

    internal void UpdateSelectedParagraphs()
    {
        SelectionParagraphs.Clear();
        SelectionParagraphs.AddRange(AllParagraphs.Where(p => p.StartInDoc + p.BlockLength > Selection.Start && p.StartInDoc <= Selection.End));


#if DEBUG
        if (ShowDebugger)
            UpdateDebuggerSelectionParagraphs();
#endif

    }

    internal void UpdateTextRanges(int fromAbsCharIndex, int offset)
    {
        List<TextRange> toRemoveRanges = [];

        int editCharIndexEnd = offset == 1 ? fromAbsCharIndex : fromAbsCharIndex - offset;

        foreach (TextRange trange in TextRanges)
        {
            //if (trange.Equals(this.Selection)) continue;  //Don't update the selection range

            if (trange.Start >= fromAbsCharIndex && trange.End <= editCharIndexEnd)
            { toRemoveRanges.Add(trange); continue; }

            if (trange.Start >= fromAbsCharIndex)
            {
                if (trange.Start >= editCharIndexEnd)
                    trange.Start += offset;
                else
                    trange.Start = fromAbsCharIndex;
            }

            if (trange.End >= fromAbsCharIndex)
            {
                if (trange.End >= editCharIndexEnd)
                    trange.End += offset;
                else
                    trange.End = fromAbsCharIndex;
            }

            if (trange.Start > trange.End)
                trange.End = trange.Start;

        }

        for (int trangeNo = toRemoveRanges.Count - 1; trangeNo >= 0; trangeNo--)
        {
            if (!toRemoveRanges[trangeNo].Equals(Selection))
                toRemoveRanges[trangeNo].Dispose();
        }

        UpdateAllRangeContexts();

    }

    internal void RestoreCaretTo(int originalStart)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Select(-1, 0);  // forces reset
            Select(originalStart, 0);
            UpdateRTBCaret?.Invoke();

        }, DispatcherPriority.Background);

    }


}

