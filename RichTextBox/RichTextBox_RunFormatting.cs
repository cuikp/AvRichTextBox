using Avalonia.Controls;

namespace AvRichTextBox;

public partial class RichTextBox
{
    
   private void ToggleItalics()
   {
      FlowDoc.ToggleItalic();

   }

   private void ToggleBold()
   {
      FlowDoc.ToggleBold();

   }

   private void ToggleUnderlining()
   {
      FlowDoc.ToggleUnderlining();

   }

   private void CopyToClipboard()
   {
      TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(FlowDoc.Selection.GetText());
         

   }

   private async void PasteFromClipboard()
   {
      string? clipboardText = await TopLevel.GetTopLevel(this)!.Clipboard!.GetTextAsync();
      //clipboardText = clipboardText.Replace("\r\n", "\v");
      if (clipboardText != null)
         FlowDoc.SetText(FlowDoc.Selection, clipboardText);

   }

}
