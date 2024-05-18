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
      rtbVM.FlowDoc.InsertChar(e.Text);
      UpdateCurrentParagraphLayout();

      
   }

   private void DeleteSelection()
   {
      rtbVM.FlowDoc.DeleteSelection();

   }

   private void DeleteChar()
   {
      rtbVM.FlowDoc.DeleteChar();
      UpdateCurrentParagraphLayout();
   }

   internal void UpdateCurrentParagraphLayout()
   {
      this.UpdateLayout();
      rtbVM.UpdateCursor();
   }

   internal void InsertParagraph()
   {
      rtbVM.FlowDoc.InsertParagraph(true);
      UpdateCurrentParagraphLayout();

   }

   public void SearchText(string searchText)
   {        
      MatchCollection matches = Regex.Matches(rtbVM.FlowDoc.Text, searchText);

      if (matches.Count > 0)
         rtbVM.FlowDoc.Select(matches[0].Index, matches[0].Length);

      //foreach (Match m in matches)
      //   rtbVM.FlowDoc.ApplyFormattingRange(Inline.BackgroundProperty, Brushes.Red, new TextRange(rtbVM.FlowDoc, m.Index, m.Index + m.Length));


   }
    
   private void Backspace()
   {
      if (rtbVM.FlowDoc.Selection!.Length > 0)
         rtbVM.FlowDoc.DeleteSelection();
      else
      {
         if (rtbVM.FlowDoc.Selection.Start == 0) return;

         if (rtbVM.FlowDoc.Selection.StartParagraph.SelectionStartInBlock == 0)
         {
            rtbVM.FlowDoc.MoveSelectionLeft(true);
            rtbVM.FlowDoc.MergeParagraphForward(rtbVM.FlowDoc.Selection.StartParagraph, true);
         }
         else
         {
            rtbVM.FlowDoc.MoveSelectionLeft(true);
            DeleteChar();
         }
      }

   }
}
