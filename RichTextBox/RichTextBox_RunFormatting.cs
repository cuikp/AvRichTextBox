using Avalonia.Controls;
using System;
using System.Diagnostics;
using static AvRichTextBox.FlowDocument;

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
      {
         int newSelPoint = FlowDoc.Selection.Start + clipboardText.Length;
         
         FlowDoc.SetText(FlowDoc.Selection, clipboardText);
         newSelPoint = Math.Min(newSelPoint, FlowDoc.Text.Length - 1);

         FlowDoc.Select(newSelPoint, 0);
         FlowDoc.Selection.BiasForward = false;
         FlowDoc.SelectionExtendMode = ExtendMode.ExtendModeNone;

         CreateClient();

      }


   }

}
