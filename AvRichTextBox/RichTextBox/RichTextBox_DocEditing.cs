using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AvRichTextBox;

public partial class RichTextBox
{

   private void RichTextBox_TextInput(object? sender, Avalonia.Input.TextInputEventArgs e)
   {
      if (IsReadOnly) return;

      FlowDoc.InsertText(e.Text);
      UpdateCurrentParagraphLayout();
      
      if (PreeditOverlay.IsVisible)
         HideIMEOverlay();
         
   }

   private void HideIMEOverlay()
   {
      _preeditText = "";
      PreeditOverlay.IsVisible = false;

   }

   internal void UpdateCurrentParagraphLayout()
   {
      this.UpdateLayout();
      RtbVm.UpdateCaretVisible();
   }

   internal void InsertParagraph()
   {
      if (IsReadOnly) return;

      FlowDoc.InsertParagraph(true, FlowDoc.Selection.Start);
      UpdateCurrentParagraphLayout();

   }

   internal void InsertLineBreak()
   {
      if (IsReadOnly) return;

      FlowDoc.InsertLineBreak();
      UpdateCurrentParagraphLayout();

   }

   internal void InsertTab()
   {
      if (IsReadOnly) return;
      FlowDoc.InsertText("\t");
      UpdateCurrentParagraphLayout();

   }


   private void PerformDelete(bool backspace)
   {
      if (IsReadOnly) return;

      if (FlowDoc.Selection.Length > 0)
         FlowDoc.DeleteSelection();
      else
      {
         if (backspace)
            if (FlowDoc.Selection.Start == 0) return;
         else
            if (FlowDoc.Selection.Start >= FlowDoc.Selection.StartParagraph.StartInDoc + FlowDoc.Selection.StartParagraph.BlockLength)
               return;

         FlowDoc.DeleteChar(backspace);
      }

      UpdateCurrentParagraphLayout();
   }

   
}
