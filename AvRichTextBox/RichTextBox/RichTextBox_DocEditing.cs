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
      rtbVM.UpdateCaretVisible();
   }

   internal void InsertParagraph()
   {
      FlowDoc.InsertParagraph(true, FlowDoc.Selection.Start);
      UpdateCurrentParagraphLayout();

   }

   internal void InsertLineBreak()
   {
      FlowDoc.InsertLineBreak();
      UpdateCurrentParagraphLayout();

   }

   public void SearchText(string searchText)
   {        
      MatchCollection matches = Regex.Matches(FlowDoc.Text, searchText);

      if (matches.Count > 0)
         FlowDoc.Select(matches[0].Index, matches[0].Length);

      
      foreach (Match m in matches)
      {
         TextRange trange = new (FlowDoc, m.Index, m.Index + m.Length);
         FlowDoc.ApplyFormattingRange(Inline.FontStretchProperty, FontStretch.UltraCondensed, trange);
         FlowDoc.ApplyFormattingRange(Inline.ForegroundProperty, new SolidColorBrush(Colors.BlueViolet), trange);
         FlowDoc.ApplyFormattingRange(Inline.BackgroundProperty, new SolidColorBrush(Colors.Wheat), trange);
      }
         


   }


   private void PerformDelete(bool backspace)
   {

      if (FlowDoc.Selection!.Length > 0)
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
