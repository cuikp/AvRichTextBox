using DynamicData;
using static AvRichTextBox.FlowDocument;

namespace AvRichTextBox;

public partial class FlowDocument
{
    internal void SetRangeToText(TextRange tRange, string newText, bool copyFormatting = true)
    {  //The delete range and SetRangeToText should constitute one Undo operation

        Paragraph startPar = tRange.StartParagraph;
        int rangeStart = tRange.Start;
        int rangeEnd = tRange.End;
        int deleteRangeLength = tRange.Length;
        int parIndex = AllParagraphs.IndexOf(startPar);
        bool firstParEmpty = startPar.Inlines[0] is EditableRun erun && erun.Text == "";
        bool firstParWasDeleted = startPar.StartInDoc == rangeStart && startPar.EndInDoc <= rangeEnd && !firstParEmpty;

        //Delete any selected text first
        if (tRange.Length > 0)
        {
            DeleteRange(tRange, false, false);  // no undo, handled by PasteUndo
            tRange.CollapseToStart();
            SelectionExtendMode = ExtendMode.ExtendModeNone;
        }

        //Debug.WriteLine("first par deleted: " + firstBlockWasDeleted);

        if (tRange.StartInline is not IEditable startInline) return;

        List<IEditable> splitInlines = SplitRunAtPos(tRange.Start, startInline, GetCharPosInInline(startInline, tRange.Start));

        int startInlineIndex = startPar.Inlines.IndexOf(splitInlines[0]) + 1;

        if (splitInlines[0] is EditableRun sRun)
        {
            EditableRun newEditableRun = new(newText);

            if (copyFormatting)
            {
                newEditableRun.FontFamily = sRun.FontFamily;
                newEditableRun.FontWeight = sRun.FontWeight;
                newEditableRun.FontStyle = sRun.FontStyle;
                newEditableRun.FontSize = sRun.FontSize;
                newEditableRun.TextDecorations = sRun.TextDecorations;
                newEditableRun.Background = sRun.Background;
                newEditableRun.BaselineAlignment = sRun.BaselineAlignment;
                newEditableRun.Foreground = sRun.Foreground;
            }

            startPar.Inlines.Insert(startInlineIndex, newEditableRun);

            if (splitInlines[0].InlineText == "")
                startPar.Inlines.Remove(splitInlines[0]);
        }

        startPar.CallRequestInvalidateVisual();
        startPar.CallRequestTextLayoutInfoStart();
        startPar.CallRequestInlinesUpdate();

        UpdateBlockAndInlineStarts(startPar);


    }

    internal void Undo()
    {
        if (Undos.Count > 0)
        {
            disableRunTextUndo = true;

            Undos.Last().PerformUndo();

            UpdateSelection();

            if (Undos.Last().UpdateTextRanges)
                UpdateTextRanges(Selection.Start, Undos.Last().UndoEditOffset);

            Undos.RemoveAt(Undos.Count - 1);

            UpdateSelectedParagraphs();


            ScrollInDirection?.Invoke(1);
            ScrollInDirection?.Invoke(-1);

            disableRunTextUndo = false;

        }
    }

    internal void RestoreDeletedBlocks(List<Block> blockClones, int blockIndex, bool firstBlockWasDeleted, bool lastBlockWasDeleted)
    {
        bool tablePartiallyDeleted = Blocks[blockIndex] is Table || (blockIndex < Blocks.Count - 1 && Blocks[blockIndex + 1] is Table);

        if (!lastBlockWasDeleted)
        {
            Blocks.RemoveAt(blockIndex);
            // Special case if table contents were partially deleted, leaving the old table
            if (!firstBlockWasDeleted && tablePartiallyDeleted)
                Blocks.RemoveAt(blockIndex);
        }
        else if (!firstBlockWasDeleted)
            Blocks.RemoveAt(blockIndex);

        //Restore all of the previous paragraphs
        Blocks.AddOrInsertRange(blockClones, blockIndex);

        //Debug.WriteLine("restoring blocks = " + blockClones.Count + ", " + blockClones[0].GetType().ToString());

        UpdateBlockAndInlineStarts(blockIndex);

        foreach (Paragraph p in FlattenParagraphs(blockClones))
            p.CallRequestInlinesUpdate();
               
    }

    private int ProcessInsertBlocks(List<Block> blocks, Paragraph startPar, int insertIdx, int insertParIndex, List<int> addedBlockIds, List<IEditable> rightSplitRuns)
    {
        int pastedTextLength = 0;
        int blockno = 0;
        foreach (Block block in blocks)
        {
            if (block is Paragraph p)
            {
                Paragraph addPar = startPar;

                //Remove single empty run if present
                //if (addPar.Inlines.Count == 1 && addPar.Inlines[0] is EditableRun run && run.InlineText == "")
                if (addPar.IsEmptyInlinePar)
                {
                    addPar.Inlines.RemoveAt(0);
                    insertIdx = 0;
                }

                bool paragraphCreated = false;

                switch (blockno)
                {
                    case 0:
                        // insert first paragraph into existing paragraph
                        addPar.Inlines.AddOrInsertRange(p.Inlines, insertIdx);
                        break;

                    default:
                        // create new paragraphs for pars 1 onward
                        addPar = (Paragraph)block;
                        pastedTextLength += 1;
                        paragraphCreated = true;
                        break;
                }

                pastedTextLength += (p.TextLength - 1); // remove extra length for par CR
                

                if (paragraphCreated)
                {
                    if (blockno == blocks.Count - 1)
                    {
                        startPar.Inlines.RemoveMany(rightSplitRuns);
                        addPar.Inlines.AddRange(rightSplitRuns);
                    }

                    Blocks.Insert(insertParIndex + blockno, addPar);
                    addedBlockIds.Add(addPar.Id);

                }
            }
            else
            { // non-Paragraph block always pastes as new block
                Blocks.Insert(insertParIndex + blockno, block);
                addedBlockIds.Add(block.Id);
                pastedTextLength += block.TextLength;
            }

            blockno++;
        }

        return pastedTextLength;
    }

}