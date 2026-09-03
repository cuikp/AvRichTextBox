using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using DocumentFormat.OpenXml.InkML;
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

        // Rtf format
        string rtfString = GetRtfFromRange(FlowDoc.Selection);
        var richTextFormat = DataFormat.CreateBytesPlatformFormat("Rich Text Format");
        byte[] rtfbytes = Encoding.ASCII.GetBytes(rtfString + "\0");
        dataTransfer.Add(DataTransferItem.Create(richTextFormat, rtfbytes));

        //Debug.WriteLine("copy rtf string " + rtfString);

        // Plain text
        List<IEditable> rangeInlines = FlowDoc.GetTextRangeInlines(FlowDoc.Selection, addToDoc: false).createdInlines;
        
        if (rangeInlines.LastOrDefault() is EditableLineBreak elb)
            rangeInlines.Remove(elb);

        string inlinesText = string.Join("", rangeInlines.ConvertAll(il => il.InlineText));
        dataTransfer.Add(DataTransferItem.CreateText(inlinesText));

        _ = clipboard.SetDataAsync(dataTransfer);

    }

    internal string GetRtfFromRange(TextRange range)
    {
        var sb = new StringBuilder();

        List<Paragraph> rangePars = FlowDoc.GetOverlappingParagraphsInRange(range, range.BiasForwardEnd);

        rangePars = rangePars.ConvertAll(b=> 
        { 
            Paragraph clonedPar = b.FullClone(false);
            clonedPar.OwningCell = b.OwningCell;
            clonedPar.OwningTable = b.OwningTable;
            return clonedPar;
        });

        if (rangePars[0] is Paragraph firstPar && rangePars[^1] is Paragraph lastPar)
        {
            lastPar.Inlines.RemoveMany(lastPar.Inlines.Where(il => lastPar.StartInDoc + il.TextPositionOfInlineInParagraph >= range.End));
            if (lastPar.Inlines.Count > 0)
            {
                switch (lastPar.Inlines[^1])
                {
                    case EditableRun edrunL:
                        int cutEnd = range.End - lastPar.StartInDoc - edrunL.TextPositionOfInlineInParagraph;
                        if (cutEnd > 0 && cutEnd <= edrunL.InlineLength)
                            edrunL.Text = edrunL.Text![..cutEnd];
                        break;
                    case EditableLineBreak edLB:
                        lastPar.Inlines.Remove(edLB);
                        break;

                    case EditableInlineUIContainer edUIC:
                        Paragraph attachPar = new(FlowDoc);
                        attachPar.Inlines.Add(new EditableRun(""));
                        rangePars.Add(attachPar);
                        break;
                }
            }
            

            firstPar.Inlines.RemoveMany(firstPar.Inlines.Where(il => firstPar.StartInDoc + il.TextPositionOfInlineInParagraph + il.InlineLength < range.Start));
            if (lastPar.Inlines.Count > 0)
            {
                switch (firstPar.Inlines[0])
                {
                    case EditableRun edrunF:
                        int cutStart = range.Start - firstPar.StartInDoc - edrunF.TextPositionOfInlineInParagraph;
                        if (cutStart > 0)
                            edrunF.Text = edrunF.Text![cutStart..];
                        break;
                    
                    case EditableLineBreak edLB:
                        firstPar.Inlines.Remove(edLB);
                        break;
                }
            } 
        }

        return RtfConversions.GetRangeRtf(rangePars);
        
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
        int deleteRangeLength = insertRange.Length;

        Paragraph destStartPar = FlowDoc.AllParagraphs.Last(p => (p.IsEmptyInlinePar || p.StartInDoc == 0) ? p.StartInDoc <= insertRange.Start : p.StartInDoc < insertRange.Start);
        Paragraph destEndPar = FlowDoc.AllParagraphs.Last(p => (p.IsEmptyInlinePar || p.StartInDoc == 0) ? p.StartInDoc <= insertRange.End : p.StartInDoc < insertRange.End);

        List<Block> originalRangeBlocks = destStartPar.IsCellBlock switch 
        {
            true => FlowDoc.GetOverlappingParagraphsInRange(insertRange, false).ConvertAll(ob => ob.FullClone(true) as Block),
            _ => FlowDoc.GetOverlappingBlocksInRange(insertRange).ConvertAll(ob => ob.FullClone(true))
        };

        int insertParIndex = -1;
        if (destStartPar.IsCellBlock)
            insertParIndex = destStartPar.OwningCell.CellBlocks.IndexOf(destStartPar);
        else
            insertParIndex = FlowDoc.Blocks.IndexOf(destStartPar);

        bool firstParEmpty = destStartPar is Paragraph p && p.Inlines[0] is EditableRun erun && erun.Text == "";
        int pastedTextLength = 0;
        List<int> addedBlockIds = [];

        bool firstBlockWasDeleted = destStartPar.StartInDoc == originalSelectionStart; // && destStartPar.EndInDoc <= originalSelectionEnd && !firstParEmpty;
        bool lastBlockWasDeleted = !firstBlockWasDeleted && destEndPar.EndInDoc == originalSelectionEnd;
        bool addUndo = true;
        bool contentPasted = false;

        FlowDoc.disableRunTextUndo = true;

        // Get clipboard content
        if (!plainTextOnly && await clipboard.TryGetValueAsync(richTextFormat) is byte[] rtfbytes)
        {            
            pastedTextLength = FlowDoc.InsertRTF(rtfbytes, destStartPar, insertRange, insertParIndex, addedBlockIds);
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
            FlowDoc.Blocks.Insert(insertParIndex + 1, newPar);
            FlowDoc.Blocks.Insert(insertParIndex + 2, extraPar);
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
            {
                FlowDoc.Undos.Add(new PasteUndo(
                   originalRangeBlocks,
                   insertParIndex,
                   FlowDoc,
                   originalSelectionStart,
                   deleteRangeLength - pastedTextLength,
                   firstParEmpty,
                   addedBlockIds,
                   firstBlockWasDeleted,
                   lastBlockWasDeleted,
                   destStartPar.IsCellBlock,
                   destStartPar.OwningTable == null ? -1 : destStartPar.OwningTable.Id,
                   destStartPar.OwningCell == null ? -1 : destStartPar.OwningCell.Id
                   ));
            }

            destStartPar.CallRequestInlinesUpdate();

            FlowDoc.UpdateBlockAndInlineStarts(insertParIndex);
            FlowDoc.UpdateSelection();

            this.DocIC.UpdateLayout();

            FlowDoc.UpdateTextRanges(originalSelectionStart, pastedTextLength - deleteRangeLength);

            CreateClient();

            FlowDoc.RestoreCaretTo(originalSelectionStart + pastedTextLength);
            
            FlowDoc.SelectionExtendMode = ExtendMode.ExtendModeNone;
            FlowDoc.Selection.BiasForwardStart = false;
            FlowDoc.Selection.BiasForwardEnd = false;
            FlowDoc.ScrollFlowDocToCaret();
            
        }
    }

    private void CutToClipboard()
    {
        if (IsReadOnly) return;
        CopyToClipboard();
        PerformDelete(false);
    }


}
