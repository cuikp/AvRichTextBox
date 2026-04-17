using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media;
using RtfDomParserAv;
using static AvRichTextBox.FlowDocument;

namespace AvRichTextBox;

public partial class RichTextBox
{

   private static ScaleTransform strans = new(0.75, 0.75);
   internal static TransformGroup SubscriptTG = new();
   internal static TransformGroup SuperscriptTG = new();

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
      byte[] rtfbytes = System.Text.Encoding.Default.GetBytes(rtfString);

      var richTextFormat = DataFormat.CreateBytesPlatformFormat("Rich Text Format");
      var dataTransfer = new DataTransfer();
      dataTransfer.Add(DataTransferItem.Create(richTextFormat, rtfbytes));
      dataTransfer.Add(DataTransferItem.CreateText(FlowDoc.Selection.GetText()));

      _ = clipboard.SetDataAsync(dataTransfer);

   }

   
   private async void PasteFromClipboard()
   {
      if (IsReadOnly) return;

      bool TextPasted = false;
      int originalSelectionStart = FlowDoc.Selection.Start;
      int newSelPoint = originalSelectionStart;
            

      var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
      if (clipboard == null) return;

      var richTextFormat = DataFormat.CreateBytesPlatformFormat("Rich Text Format");

      var formats = await clipboard.GetDataFormatsAsync();
      if (formats.Contains(richTextFormat))
      {
         if (await clipboard.TryGetValueAsync(richTextFormat) is byte[] rtfbytes)
         {
            string rtfstring = System.Text.Encoding.Default.GetString(rtfbytes!);
            RTFDomDocument dom = new();
            dom.LoadRTFText(rtfstring);
            List<IEditable> insertInlines = RtfConversions.GetInlinesFromRtf(dom);
            insertInlines.Reverse();
            int addedchars = FlowDoc.PasteInlinesIntoRange(FlowDoc.Selection, insertInlines);

            newSelPoint = Math.Min(newSelPoint + addedchars, FlowDoc.DocEndPoint - 1);

            TextPasted = true;
         }
      }
          
      else if (await clipboard.TryGetTextAsync() is string pasteText)
      {
         FlowDoc.SetRangeToText(FlowDoc.Selection, pasteText);
         newSelPoint = Math.Min(newSelPoint + pasteText.Length, FlowDoc.DocEndPoint - 1);
         TextPasted = true;
      }

      if (TextPasted)
      {
         this.DocIC.UpdateLayout();
         await Task.Delay(100); //necessary for following operations

         FlowDoc.Selection.EndParagraph.CallRequestInlinesUpdate();  // important
         FlowDoc.Selection.EndParagraph.UpdateEditableRunPositions();

         FlowDoc.Select(newSelPoint, 0);
         FlowDoc.UpdateSelection();

         FlowDoc.Selection.BiasForwardStart = false;
         FlowDoc.Selection.BiasForwardEnd = false;
         FlowDoc.SelectionExtendMode = ExtendMode.ExtendModeNone;

         CreateClient();


      }

   }


}
