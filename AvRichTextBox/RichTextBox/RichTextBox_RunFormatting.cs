using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DynamicData;
using System.Text;
using static AvRichTextBox.FlowDocument;
using Avalonia.Threading;

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
      //create rtf string
      List<IEditable> newInlines = FlowDoc.GetRangeInlines(FlowDoc.Selection);
      string rtfString = RtfConversions.GetRtfFromInlines(newInlines);

      var dataTransfer = new DataTransfer();

      // Rtf format
      var richTextFormat = DataFormat.CreateBytesPlatformFormat("Rich Text Format");
      byte[] rtfbytes = Encoding.ASCII.GetBytes(rtfString + "\0");
      dataTransfer.Add(DataTransferItem.Create(richTextFormat, rtfbytes));

      // Plain text
      dataTransfer.Add(DataTransferItem.CreateText(FlowDoc.Selection.GetText()));

      _ = clipboard.SetDataAsync(dataTransfer);

   }

   readonly static DataFormat<byte[]> richTextFormat = DataFormat.CreateBytesPlatformFormat("Rich Text Format");


   private async void PasteFromClipboard()
   {
      if (IsReadOnly) return;
      if (FlowDoc.Selection.StartInline is not IEditable startInline) return;

      var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
      if (clipboard == null) return;

      //get paste location properties
      int originalSelectionStart = FlowDoc.Selection.Start;
      int originalSelectionEnd = FlowDoc.Selection.End;
      TextRange insertRange = FlowDoc.Selection;
      List<Paragraph> originalRangeParagraphs = FlowDoc.GetOverlappingParagraphsInRange(insertRange).ConvertAll(op => op.FullClone());
      int deleteRangeLength = insertRange.Length;
      Paragraph startPar = insertRange.StartParagraph;
      int insertParIndex = FlowDoc.Blocks.IndexOf(startPar);
      bool firstParEmpty = startPar.Inlines[0] is EditableRun erun && erun.Text == "";
      int pastedTextLength = 0;
      List<int> addedBlockIds = [];
      bool firstParWasDeleted = startPar.StartInDoc == originalSelectionStart && startPar.EndInDoc <= originalSelectionEnd && !firstParEmpty;
      bool addUndo = true;
      bool contentPasted = false;

      FlowDoc.disableRunTextUndo = true;

      //Get clipboard content
      if (await clipboard.TryGetValueAsync(richTextFormat) is byte[] rtfbytes)
      {
         pastedTextLength = FlowDoc.InsertRTF(rtfbytes, startPar, insertRange, insertParIndex, addedBlockIds);
         contentPasted = true;
      }

      else if (await clipboard.TryGetBitmapAsync() is Bitmap pasteBitmap)
      {
         Image pasteImage = new() { Source = pasteBitmap };
         EditableInlineUIContainer newEIUC = new(pasteImage);
         Paragraph newPar = new(FlowDoc);
         newPar.Inlines.Add(newEIUC);
         Paragraph extraPar = new(FlowDoc);
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
            FlowDoc.Undos.Add(new PasteUndo(originalRangeParagraphs, insertParIndex, FlowDoc, originalSelectionStart, deleteRangeLength - pastedTextLength, firstParEmpty, addedBlockIds, firstParWasDeleted));

         this.DocIC.UpdateLayout();
         
         FlowDoc.UpdateBlockAndInlineStarts(insertParIndex);
         FlowDoc.UpdateSelection();
         
         FlowDoc.SelectionExtendMode = ExtendMode.ExtendModeNone;
         
         CreateClient();

         // Defer caret positioning to after layout has completed for newly inserted paragraphs.
         // Without this, the caret can appear at the wrong position.
         Dispatcher.UIThread.Post(() =>
         {
            FlowDoc.Select(originalSelectionStart + pastedTextLength, 0);
            FlowDoc.Selection.BiasForwardStart = false;
            FlowDoc.Selection.BiasForwardEnd = false;
            FlowDoc.ScrollFlowDocInDirection(1);
            _ = FlowDoc.AsyncUpdateCaret(FlowDoc.Selection);
         });

      }
   }

   private void CutToClipboard()
   {
      if (IsReadOnly) return;
      CopyToClipboard();
      PerformDelete(false);
   }


}
