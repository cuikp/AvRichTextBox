
namespace AvRichTextBox;

public partial class FlowDocument
{

    internal void ExtendSelectionRight()
    {
        Selection.BiasForwardEnd = false;

        switch (SelectionExtendMode)
        {
            case ExtendMode.ExtendModeNone:
            case ExtendMode.ExtendModeRight:

                SelectionExtendMode = ExtendMode.ExtendModeRight;

                if (Selection.EndParagraph == AllParagraphs.ToList()[^1] && Selection.EndParagraph.SelectionEndInBlock == Selection.EndParagraph.TextLength)
                    return;  // End of document

                Selection.End = GetNextPosition();

                break;

            case ExtendMode.ExtendModeLeft:

                Selection.BiasForwardStart = false;

                Selection.Start = GetNextPosition();

                if (Selection.Start == Selection.End)
                    SelectionExtendMode = ExtendMode.ExtendModeRight;

                break;
        }

        ScrollInDirection?.Invoke(1);

    }

    internal void ExtendSelectionLeft()
    {
        Selection.BiasForwardEnd = false;
        Selection.BiasForwardStart = true;

        switch (SelectionExtendMode)
        {
            case ExtendMode.ExtendModeNone:
            case ExtendMode.ExtendModeLeft:
                if (Selection.Start == 0) return;

                Selection.Start = GetPreviousPosition();

                SelectionExtendMode = ExtendMode.ExtendModeLeft;
                break;

            case ExtendMode.ExtendModeRight:
                if (Selection.End == 0) return;

                Selection.BiasForwardEnd = true;

                Selection.End = GetPreviousPosition();

                if (Selection.Start == Selection.End)
                    SelectionExtendMode = ExtendMode.ExtendModeLeft;
                break;
        }

        ScrollInDirection?.Invoke(-1);
    }

    internal void ExtendSelectionRightWord()
    {
        Selection.BiasForwardEnd = true;

        int targetPos = GetNextWordPosition();

        switch (SelectionExtendMode)
        {
            case ExtendMode.ExtendModeNone:
            case ExtendMode.ExtendModeRight:
                SelectionExtendMode = ExtendMode.ExtendModeRight;
                Selection.End = targetPos;
                break;

            case ExtendMode.ExtendModeLeft:
                if (targetPos >= Selection.End)
                {
                    Selection.Start = Selection.End;
                    Selection.End = targetPos;
                    SelectionExtendMode = ExtendMode.ExtendModeRight;
                }
                else
                    Selection.Start = targetPos;
                break;
        }

        ScrollInDirection?.Invoke(1);
    }

    internal void ExtendSelectionLeftWord()
    {
        Selection.BiasForwardEnd = false;

        int targetPos = GetPreviousWordPosition();

        switch (SelectionExtendMode)
        {
            case ExtendMode.ExtendModeNone:
            case ExtendMode.ExtendModeLeft:
                SelectionExtendMode = ExtendMode.ExtendModeLeft;
                Selection.Start = targetPos;
                break;

            case ExtendMode.ExtendModeRight:
                if (targetPos <= Selection.Start)
                {
                    Selection.End = Selection.Start;
                    Selection.Start = targetPos;
                    SelectionExtendMode = ExtendMode.ExtendModeLeft;
                }
                else
                    Selection.End = targetPos;
                break;
        }

        ScrollInDirection?.Invoke(-1);
    }

    internal void ExtendSelectionToDocStart()
    {
        Selection.BiasForwardStart = true;
        Selection.BiasForwardEnd = true;

        switch (SelectionExtendMode)
        {
            case ExtendMode.ExtendModeNone:
            case ExtendMode.ExtendModeLeft:
                SelectionExtendMode = ExtendMode.ExtendModeLeft;
                Selection.Start = 0;
                break;

            case ExtendMode.ExtendModeRight:
                // Was extending right, now selecting all the way to doc start
                // means we flip direction past the anchor
                Selection.End = Selection.Start;
                Selection.Start = 0;
                SelectionExtendMode = ExtendMode.ExtendModeLeft;
                break;
        }

        ScrollInDirection?.Invoke(-1);
    }

    internal void ExtendSelectionToDocEnd()
    {
        Selection.BiasForwardStart = false;
        Selection.BiasForwardEnd = false;

        List<Paragraph> allPars = AllParagraphs;
        int docEnd = allPars[^1].StartInDoc + allPars[^1].BlockLength - 1;

        switch (SelectionExtendMode)
        {
            case ExtendMode.ExtendModeNone:
            case ExtendMode.ExtendModeRight:
                SelectionExtendMode = ExtendMode.ExtendModeRight;
                Selection.End = docEnd;
                break;

            case ExtendMode.ExtendModeLeft:
                // Was extending left, now selecting all the way to doc end
                // means we flip direction past the anchor
                Selection.Start = Selection.End;
                Selection.End = docEnd;
                SelectionExtendMode = ExtendMode.ExtendModeRight;
                break;
        }

        ScrollInDirection?.Invoke(1);
    }
        
    internal void ExtendSelectionDown()
    {
        Selection.BiasForwardEnd = true;

        List<Paragraph> allPars = AllParagraphs;

        switch (SelectionExtendMode)
        {

            case ExtendMode.ExtendModeNone:
            case ExtendMode.ExtendModeRight:  // Hitting down key increases selection range from bottom

                SelectionExtendMode = ExtendMode.ExtendModeRight;

                Selection.End = GetNextDown();

                ////hyperlink encountered (select hyperlink)
                //if (GetStartInline(nextEnd) is EditableHyperlink hyperlink)
                //{
                //   if (AllParagraphs.LastOrDefault(p=> p.StartInDoc <= nextEnd) is Paragraph thisPar)
                //      Select(thisPar.StartInDoc + hyperlink.TextPositionOfInlineInParagraph, hyperlink.InlineLength);
                //   return;
                //}

                break;

            case ExtendMode.ExtendModeLeft:  // Hitting Down key reduces selection range from top 

                if (Selection.StartParagraph == allPars[^1] && Selection.StartParagraph.IsStartAtLastLine)
                    return;  // last line of document

                int newStart = GetNextDown();

                if (newStart > Selection.End)
                {
                    int oldEnd = Selection.End;
                    Selection.End = newStart;
                    Selection.Start = oldEnd;
                    SelectionExtendMode = ExtendMode.ExtendModeRight;
                }
                else
                    Selection.Start = newStart;

                break;
        }

        ScrollInDirection?.Invoke(1);

    }

    internal void ExtendSelectionUp()
    {
        Selection.BiasForwardEnd = false;

        List<Paragraph> allPars = AllParagraphs;

        switch (SelectionExtendMode)
        {
            case ExtendMode.ExtendModeNone:
            case ExtendMode.ExtendModeLeft:

                if (Selection.StartParagraph == allPars[0] && Selection.StartParagraph.IsStartAtFirstLine)
                {
                    Selection.Start = 0;
                    return;  // first line of document
                }

                Selection.Start = GetNextUp();

                ////hyperlink encountered (select hyperlink)
                //if (GetStartInline(nextStart) is EditableHyperlink hyperlink)
                //{
                //   if (AllParagraphs.LastOrDefault(p => p.StartInDoc <= nextStart) is Paragraph thisPar)
                //      Select(thisPar.StartInDoc + hyperlink.TextPositionOfInlineInParagraph, hyperlink.InlineLength);
                //   return;
                //}

                SelectionExtendMode = ExtendMode.ExtendModeLeft;

                break;


            case ExtendMode.ExtendModeRight: // Hitting up key reduces selection range from bottom 

                int newEnd = GetNextUp();

                if (newEnd < Selection.Start)
                {
                    int oldStart = Selection.Start;
                    Selection.Start = newEnd;
                    Selection.End = oldStart;
                    SelectionExtendMode = ExtendMode.ExtendModeLeft;
                }
                else
                    Selection.End = newEnd;

                break;
        }

        ScrollInDirection?.Invoke(-1);


    }


}


