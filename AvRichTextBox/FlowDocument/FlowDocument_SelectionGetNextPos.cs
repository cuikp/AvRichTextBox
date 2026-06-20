
using DocumentFormat.OpenXml.Spreadsheet;

namespace AvRichTextBox;

public partial class FlowDocument
{

    private int GetPreviousPosition()
    {
        int currentPos = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.End : Selection.Start;
        if (currentPos <= 0)
            return 0;
        Paragraph startP = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.EndParagraph : Selection.StartParagraph;
        int posInBlock = currentPos - startP.StartInDoc;

        if (posInBlock <= 0)
        {  // At start of paragraph, move to end of previous paragraph
            return Math.Max(0, startP.StartInDoc - 1);
        }

        int computedPrevious = startP.StartInDoc + posInBlock - 1;


        // skip special
        if (Selection.Length > 0)
        {
            if (GetStartInline(computedPrevious) is EditableHyperlink hyperlink)
            {
                int absHyperlinkStart = startP.StartInDoc + hyperlink.TextPositionOfInlineInParagraph;
                int absHyperlinkEnd = absHyperlinkStart + hyperlink.InlineLength;

                if (Selection.End <= absHyperlinkEnd && SelectionExtendMode == ExtendMode.ExtendModeLeft)
                {
                    if (Selection.Start == absHyperlinkStart + 1)
                        Selection.End = absHyperlinkEnd;
                }
                else
                    computedPrevious = absHyperlinkStart;
            }
        }


        if (GetStartInline(computedPrevious) is EditableLineBreak)
            computedPrevious -= 1;
              

        return computedPrevious;

    }

    internal int GetNextPosition()
    {
        int currentPos = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.Start : Selection.End;

        Paragraph startP = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.StartParagraph : Selection.EndParagraph;
        int posInBlock = currentPos - startP.StartInDoc;
        int computedNext = startP.StartInDoc + posInBlock + 1;

        // skip special
        if (Selection.Length > 0)
        {
            if (GetStartInline(computedNext - 1) is EditableHyperlink hyperlink)
            {
                int absHyperlinkStart = startP.StartInDoc + hyperlink.TextPositionOfInlineInParagraph;
                int absHyperlinkEnd = absHyperlinkStart + hyperlink.InlineLength;

                if (Selection.Start >= absHyperlinkStart && SelectionExtendMode == ExtendMode.ExtendModeRight)
                {
                    if (Selection.End == absHyperlinkEnd - 1)
                        Selection.Start = absHyperlinkStart;
                }
                else
                    computedNext = absHyperlinkEnd;
            }

        }

        if (GetStartInline(computedNext - 1) is EditableLineBreak)
            computedNext += 1;

            
        return computedNext;

    }

    private int GetNextDown()
    {
        int currentPos = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.Start : Selection.End;

        Paragraph relPar = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.StartParagraph : Selection.EndParagraph;
        bool atParBottom = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? relPar.IsStartAtLastLine : relPar.IsEndAtLastLine;
        bool atCellBottom = atParBottom && relPar.IsCellBlock;
        
        if (atCellBottom)
        {
            int rowno = relPar.OwningCell.RowNo;
            int colno = relPar.OwningCell.ColNo;
            int colspan = relPar.OwningCell.ColSpan;

            Table thisTable = relPar.OwningTable;
            if (rowno == relPar.OwningTable.RowDefs.Count - 1)
            {
                if (AllParagraphs.FirstOrDefault(p => p.StartInDoc > thisTable.EndInDoc) is Paragraph nextPar)
                {
                    return nextPar.StartInDoc;
                }
            }
            else
            {
                if (thisTable.Cells.LastOrDefault(c => c.RowNo == rowno + 1 && c.ColNo <= colno + (colspan - 1)) is Cell cellBelow)
                {
                    if (cellBelow.CellBlocks.First() is Paragraph firstPar)
                    {
                        return firstPar.StartInDoc;
                    }
                }
            }
        }

        //int posNextLineInBlock = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.StartParagraph.CharNextLineStart : Selection.EndParagraph.CharNextLineEnd;
        int posNextLineInBlock = SelectionExtendMode switch
        {
            ExtendMode.ExtendModeLeft => Selection.StartParagraph.CharNextLineStart,
            _ => Selection.EndParagraph.CharNextLineEnd
        };

        int computedNext = relPar.StartInDoc + posNextLineInBlock;

        if (atParBottom)
        {
            bool keepWithinCurrentLine =
                SelectionExtendMode == ExtendMode.ExtendModeRight && !(relPar.Inlines.Count > 0 && relPar.Inlines[0].IsEmpty);

            double currLeft = relPar.TextLayout.HitTestTextPosition(currentPos - relPar.StartInDoc).Left;
            int parIdx = AllParagraphs.IndexOf(relPar);
            if (parIdx < AllParagraphs.Count - 1)
            {
                relPar = AllParagraphs[parIdx + 1];

                int adjustToKeepWithinCurrentLine = (keepWithinCurrentLine && relPar.Inlines.Count > 0 && !relPar.Inlines[0].IsEmpty) ? -1 : 0;

                try  // avoids error when line wraps change
                {
                    int textPosNewPar = Math.Min(relPar.BlockLength - 1, relPar.TextLayout.HitTestPoint(new Point(currLeft, 0)).TextPosition);
                    computedNext = relPar.StartInDoc + textPosNewPar + adjustToKeepWithinCurrentLine;
                }
                catch (Exception ex) { Debug.WriteLine($"error getting next down position: (currleft = {currLeft}) {ex.Message}"); }
            }
            
        }

        return computedNext;


    }

    private int GetNextUp()
    {

        int currentPos = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.End : Selection.Start;
        Paragraph relPar = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.EndParagraph : Selection.StartParagraph;

        int posPrevLineInBlock = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.EndParagraph.CharPrevLineEnd : Selection.StartParagraph.CharPrevLineStart;
        int computedPrev = relPar.StartInDoc + posPrevLineInBlock;

        bool atParTop = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? relPar.IsEndAtFirstLine : relPar.IsStartAtFirstLine;
        bool atCellTop = atParTop && relPar.IsCellBlock;

        if (atCellTop)
        {
            int rowno = relPar.OwningCell.RowNo;
            int colno = relPar.OwningCell.ColNo;
            Table thisTable = relPar.OwningTable;
            if (rowno == 0)
            {
                if (AllParagraphs.LastOrDefault(p=> p.StartInDoc < thisTable.StartInDoc) is Paragraph prevPar)
                {
                    return prevPar.EndInDoc;
                }
            }
            else
            {
                if (thisTable.Cells.LastOrDefault(c => c.RowNo == rowno - 1 && c.ColNo <= colno) is Cell cellAbove)
                {
                    if (cellAbove.CellBlocks.Last() is Paragraph lastPar)
                    {
                        return lastPar.StartInDoc + lastPar.BlockLength - 1;
                    }
                }
            }
        }



        if (atParTop)
        {
            double currLeft = relPar.TextLayout.HitTestTextPosition(currentPos - relPar.StartInDoc).Left;
            int parIdx = AllParagraphs.IndexOf(relPar);
            if (parIdx > 0)
            {
                relPar = AllParagraphs[parIdx - 1];
                                                
                try  // avoids error when line wraps change
                {
                    int textPosNewPar = Math.Min(relPar.BlockLength, relPar.TextLayout.HitTestPoint(new Point(currLeft, relPar.TextLayout.Height)).TextPosition);
                    computedPrev = relPar.StartInDoc + textPosNewPar;
                }
                catch (Exception ex) { Debug.WriteLine("error getting next up position: " + ex.Message); }
            }
        }


        return computedPrev;

    }


    /// <summary>
    /// Computes the position of the next word boundary (rightward) from the current selection end.
    /// </summary>
    private int GetNextWordPosition()
    {
        // Determine the anchor point based on extend mode
        int currentPos = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.Start : Selection.End;

        Paragraph startP = (SelectionExtendMode == ExtendMode.ExtendModeLeft) ? Selection.StartParagraph : Selection.EndParagraph;
        int posInBlock = currentPos - startP.StartInDoc;

        if (posInBlock >= startP.TextLength)
        {
            // At end of paragraph, move to start of next paragraph
            int nextPos = startP.StartInDoc + startP.BlockLength;
            return Math.Min(nextPos, DocEndPoint - 1);
        }

        string parText = startP.Text;

        // Skip any spaces at the current position
        int searchFrom = posInBlock;
        while (searchFrom < parText.Length && (parText[searchFrom] == ' ' || parText[searchFrom] == '\n'))
            searchFrom++;

        if (searchFrom >= parText.Length)
            return startP.StartInDoc + startP.TextLength;

        // Find next space from the adjusted position
        int indexNext = parText.IndexOf(' ', searchFrom);
        if (indexNext == -1)
        {  // No space found - go to end of paragraph text
            return startP.StartInDoc + startP.TextLength;
        }
        else
        {  // Go to position after the space
            int computedNext = startP.StartInDoc + indexNext + 1;

            // skip special
            if (GetStartInline(computedNext - 1) is EditableHyperlink hyperlink)
                computedNext = Selection.StartParagraph.StartInDoc + hyperlink.TextPositionOfInlineInParagraph + hyperlink.InlineLength;

            return computedNext;
        }
    }

    /// <summary>
    /// Computes the position of the previous word boundary (leftward) from the current selection start.
    /// </summary>
    private int GetPreviousWordPosition()
    {
        // Determine the anchor point based on extend mode
        int currentPos = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.End : Selection.Start;

        if (currentPos <= 0)
            return 0;

        Paragraph startP = (SelectionExtendMode == ExtendMode.ExtendModeRight) ? Selection.EndParagraph : Selection.StartParagraph;
        int posInBlock = currentPos - startP.StartInDoc;

        if (posInBlock <= 0)
        {
            // At start of paragraph, move to end of previous paragraph
            return Math.Max(0, startP.StartInDoc - 1);
        }

        // Skip any spaces immediately to the left of current position
        int searchFrom = posInBlock - 1;
        string parText = startP.Text;
        while (searchFrom > 0 && (parText[searchFrom] == ' ' || parText[searchFrom] == '\n'))
            searchFrom--;

        if (searchFrom <= 0)
            return startP.StartInDoc;

        // Find previous space from the adjusted position
        int indexNext = parText.LastIndexOfAny(" \n".ToCharArray(), searchFrom);
        if (indexNext == -1)
        {  // No space found - go to start of paragraph
            return startP.StartInDoc;
        }
        else
        {  // Go to position after the space (right of space)
            int computedPrevious = startP.StartInDoc + indexNext + 1;

            // skip special
            if (GetStartInline(computedPrevious - 1) is EditableHyperlink hyperlink)
                computedPrevious = Selection.StartParagraph.StartInDoc + hyperlink.TextPositionOfInlineInParagraph;

            return computedPrevious;
        }
    }


}


