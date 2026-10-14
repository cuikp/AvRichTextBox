using Avalonia.Controls;
using Avalonia.Media.Imaging;
using DynamicData;

namespace AvRichTextBox;

public partial class FlowDocument
{
    internal bool InsertImagesAt(int position, IReadOnlyList<Bitmap> images)
    {
        if (images.Count == 0 || GetContainingParagraph(position) is not Paragraph paragraph)
            return false;

        IEditable? inline = paragraph.Inlines.LastOrDefault(item =>
            paragraph.StartInDoc + item.TextPositionOfInlineInParagraph <= position);
        if (inline == null)
            return false;

        var blocks = paragraph.IsCellBlock ? paragraph.OwningCell!.CellBlocks : Blocks;
        int paragraphIndex = blocks.IndexOf(paragraph);
        int updateIndex = paragraph.IsCellBlock ? Blocks.IndexOf(paragraph.OwningTable!) : paragraphIndex;
        if (paragraphIndex < 0 || updateIndex < 0)
            return false;

        Paragraph original = paragraph.FullClone(true);
        int oldLength = DocEndPoint;
        bool firstEmpty = paragraph.IsEmptyInlinePar;
        List<int> addedBlockIds = [];
        bool previousUndoState = DisableUndoStack;
        DisableUndoStack = true;
        try
        {
            int splitPosition = GetCharPosInInline(inline, position);
            int insertIndex = paragraph.Inlines.IndexOf(inline);
            if (position == paragraph.StartInDoc)
                insertIndex = 0;
            else if (splitPosition >= inline.InlineLength)
                insertIndex++;
            else if (splitPosition > 0 && inline is EditableRun)
            {
                List<IEditable> split = SplitRunAtPos(position, inline, splitPosition);
                insertIndex = paragraph.Inlines.IndexOf(split[0]) + 1;
            }

            List<IEditable> remaining = [.. paragraph.Inlines.Skip(insertIndex)];
            paragraph.Inlines.RemoveMany(remaining);
            Paragraph following = paragraph.PropertyClone();
            following.Inlines.AddRange(remaining);
            following.EnsureEmptyRuns();

            int nextIndex = paragraphIndex + 1;
            for (int index = 0; index < images.Count; index++)
            {
                Paragraph imageParagraph;
                if (index == 0 && position == paragraph.StartInDoc)
                    imageParagraph = paragraph;
                else
                {
                    imageParagraph = new Paragraph();
                    blocks.Insert(nextIndex++, imageParagraph);
                    addedBlockIds.Add(imageParagraph.Id);
                }
                imageParagraph.Inlines.Add(new EditableInlineUIContainer(new Image { Source = images[index] }));
            }

            blocks.Insert(nextIndex, following);
            addedBlockIds.Add(following.Id);
            paragraph.EnsureEmptyRuns();
            UpdateBlockAndInlineStarts(updateIndex);
            int insertedLength = DocEndPoint - oldLength;

            Undos.Add(new PasteUndo(paragraph.Id, paragraphIndex, [original], this, position, 0,
                insertedLength, firstEmpty, addedBlockIds, false, false, paragraph.IsCellBlock,
                paragraph.OwningTable?.Id ?? -1, paragraph.OwningCell?.Id ?? -1));
            Redos.Clear();
            UpdateTextRanges(position, insertedLength);
            UpdateTextLayouts(blocks.Skip(paragraphIndex).Take(nextIndex - paragraphIndex + 1));
            Selection.BiasForwardStart = true;
            Selection.BiasForwardEnd = true;
            Select(following.StartInDoc, 0);
            ScrollFlowDocToCaret();
            return true;
        }
        finally
        {
            DisableUndoStack = previousUndoState;
        }
    }
}
