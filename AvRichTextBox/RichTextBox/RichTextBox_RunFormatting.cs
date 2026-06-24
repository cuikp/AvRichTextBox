using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using DynamicData;
using System.Text;
using static AvRichTextBox.FlowDocument;

namespace AvRichTextBox;

public partial class RichTextBox
{
    private void ToggleItalics()
    {
        if (IsReadOnly) return;
        FlowDoc.ToggleItalic();

    }

    private void ToggleBold()
    {
        if (IsReadOnly) return;
        FlowDoc.ToggleBold();

    }

    private void ToggleUnderlining()
    {
        if (IsReadOnly) return;
        FlowDoc.ToggleUnderlining();

    }

    private void CopyToClipboard()
    {
        if (DisableUserCopy) return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;
              

        var dataTransfer = new DataTransfer();

        List<IEditable> rangeInlines = FlowDoc.GetTextRangeInlines(FlowDoc.Selection, addToDoc: false).createdInlines;

        // Rtf format
        string rtfString = GetRtfFromRange(FlowDoc.Selection);
        var richTextFormat = DataFormat.CreateBytesPlatformFormat("Rich Text Format");
        byte[] rtfbytes = Encoding.ASCII.GetBytes(rtfString + "\0");
        dataTransfer.Add(DataTransferItem.Create(richTextFormat, rtfbytes));

        // Plain text
        string inlinesText = string.Join("", rangeInlines.ConvertAll(il => il.InlineText));
        dataTransfer.Add(DataTransferItem.CreateText(inlinesText));

        _ = clipboard.SetDataAsync(dataTransfer);

    }

    internal string GetRtfFromRange(TextRange range)
    {
        var sb = new StringBuilder();
        List<Block> rangeBlocks = FlowDoc.GetOverlappingBlocksInRange(range).ConvertAll(b=> b.FullClone());

        // split first/last paragraphs at range
        if (rangeBlocks[0] is Paragraph firstPar && rangeBlocks[^1] is Paragraph lastPar)
        {
            lastPar.Inlines.RemoveMany(lastPar.Inlines.Where(il => lastPar.StartInDoc + il.TextPositionOfInlineInParagraph >= range.End));
            if (lastPar.Inlines[^1] is EditableRun edrunL)
            {
                int cutEnd = range.End - lastPar.StartInDoc - edrunL.TextPositionOfInlineInParagraph;
                if (cutEnd > 0)
                    edrunL.Text = edrunL.Text![..cutEnd];
            }

            firstPar.Inlines.RemoveMany(firstPar.Inlines.Where(il => firstPar.StartInDoc + il.TextPositionOfInlineInParagraph + il.InlineLength < range.Start));
            if (firstPar.Inlines[0] is EditableRun edrunF)
            {
                int cutStart = range.Start - firstPar.StartInDoc - edrunF.TextPositionOfInlineInParagraph;
                if (cutStart > 0)
                    edrunF.Text = edrunF.Text![cutStart..];
            }
        }

        return RtfConversions.GetRangeRtf(rangeBlocks);
        
    }


    readonly static DataFormat<byte[]> richTextFormat = DataFormat.CreateBytesPlatformFormat("Rich Text Format");

    private async void PasteFromClipboard(bool plainTextOnly = false)
    {
        if (IsReadOnly) return;
        if (FlowDoc.Selection.StartInline is not IEditable startInline) return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;

        // Get paste location properties
        int originalSelectionStart = FlowDoc.Selection.Start;
        int originalSelectionEnd = FlowDoc.Selection.End;
        TextRange insertRange = FlowDoc.Selection;
        List<Block> originalRangeBlocks = FlowDoc.GetOverlappingBlocksInRange(insertRange).ConvertAll(ob => ob.FullClone());
        int deleteRangeLength = insertRange.Length;

        Block startBlock = FlowDoc.Blocks.Last(b => b.StartInDoc < insertRange.Start);
        Block endBlock = FlowDoc.Blocks.Last(b => b.StartInDoc < insertRange.End);
        
        Paragraph startPar = insertRange.StartParagraph;
        //Paragraph endPar = insertRange.EndParagraph;
        int insertBlockIndex = FlowDoc.Blocks.IndexOf(startBlock);
        
        bool firstParEmpty = startBlock is Paragraph p && p.Inlines[0] is EditableRun erun && erun.Text == "";
        int pastedTextLength = 0;
        List<int> addedBlockIds = [];
        
        bool firstBlockWasDeleted = startBlock.StartInDoc == originalSelectionStart && startBlock.EndInDoc <= originalSelectionEnd && !firstParEmpty;
        bool lastBlockWasDeleted = endBlock.EndInDoc == originalSelectionEnd && endBlock.EndInDoc >= originalSelectionStart;
        
        bool addUndo = true;
        bool contentPasted = false;

        FlowDoc.disableRunTextUndo = true;

        // Get clipboard content
        if (!plainTextOnly && await clipboard.TryGetValueAsync(richTextFormat) is byte[] rtfbytes)
        {
            pastedTextLength = FlowDoc.InsertRTF(rtfbytes, startPar, insertRange, insertBlockIndex, addedBlockIds);
            contentPasted = true;
        }
        else if (!plainTextOnly && await clipboard.TryGetBitmapAsync() is Bitmap pasteBitmap)
        {
            Image pasteImage = new() { Source = pasteBitmap };
            EditableInlineUIContainer newEIUC = new(pasteImage);
            Paragraph newPar = new(FlowDoc);
            newPar.Inlines.Add(newEIUC);
            Paragraph extraPar = new(FlowDoc);
            // force pasted image into a new paragraph
            FlowDoc.Blocks.Insert(insertBlockIndex + 1, newPar);
            FlowDoc.Blocks.Insert(insertBlockIndex + 2, extraPar);
            addedBlockIds.Add(newPar.Id);
            addedBlockIds.Add(extraPar.Id);
            pastedTextLength = 2;
            contentPasted = true;
        }
        else if (await clipboard.TryGetTextAsync() is string pasteText)
        {
            FlowDoc.disableRunTextUndo = true;
            pastedTextLength = pasteText.Length;
            if (plainTextOnly)
                FlowDoc.SetRangeToText(insertRange, pasteText, copyFormatting: false);
            else
                FlowDoc.Selection.Text = pasteText;
            FlowDoc.disableRunTextUndo = false;
            contentPasted = true;
            addUndo = true;
        }

        FlowDoc.disableRunTextUndo = false;

        //Update based on pasted content
        if (contentPasted)
        {
            if (addUndo)
                FlowDoc.Undos.Add(new PasteUndo(
                   originalRangeBlocks,
                   insertBlockIndex,
                   FlowDoc,
                   originalSelectionStart,
                   deleteRangeLength - pastedTextLength,
                   firstParEmpty,
                   addedBlockIds,
                   firstBlockWasDeleted,
                   lastBlockWasDeleted
                   ));
            
            FlowDoc.UpdateBlockAndInlineStarts(insertBlockIndex);
            FlowDoc.UpdateSelection();

            this.DocIC.UpdateLayout();

            FlowDoc.UpdateTextRanges(originalSelectionStart, pastedTextLength - deleteRangeLength);

            CreateClient();

            FlowDoc.RestoreCaretTo(originalSelectionStart + pastedTextLength);
            
            FlowDoc.SelectionExtendMode = ExtendMode.ExtendModeNone;
            FlowDoc.Selection.BiasForwardStart = false;
            FlowDoc.Selection.BiasForwardEnd = false;

            FlowDoc.ScrollFlowDocInDirection(1);

            

        }
    }

    private void CutToClipboard()
    {
        if (IsReadOnly) return;
        CopyToClipboard();
        PerformDelete(false);
    }


}
