using Avalonia.Controls;

namespace AvRichTextBox;

public partial class RichTextBox
{
    
   private void ToggleItalics()
   {
      rtbVM.FlowDoc.ToggleItalic();

   }

   private void ToggleBold()
   {
      rtbVM.FlowDoc.ToggleBold();

   }

   private void ToggleUnderlining()
   {
      rtbVM.FlowDoc.ToggleUnderlining();

   }

   private void CopyToClipboard()
   {
      TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(rtbVM.FlowDoc.Selection.GetText());
         

   }

   private async void PasteFromClipboard()
   {
      string? clipboardText = await TopLevel.GetTopLevel(this)!.Clipboard!.GetTextAsync();
      //clipboardText = clipboardText.Replace("\r\n", "\v");
      if (clipboardText != null)
         rtbVM.FlowDoc.SetText(rtbVM.FlowDoc.Selection, clipboardText);

   }

}
