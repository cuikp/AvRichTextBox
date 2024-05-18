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
      FlowDoc.InsertChar(e.Text);
      UpdateCurrentParagraphLayout();

      
   }

   private void DeleteSelection()
   {
      FlowDoc.DeleteSelection();

   }

   private void DeleteChar()
   {
      FlowDoc.DeleteChar();
      UpdateCurrentParagraphLayout();
   }

   internal void UpdateCurrentParagraphLayout()
   {
      this.UpdateLayout();
      rtbVM.UpdateCursor();
   }

   internal void InsertParagraph()
   {
      FlowDoc.InsertParagraph(true);
      UpdateCurrentParagraphLayout();

   }

   public void SearchText(string searchText)
   {        
      MatchCollection matches = Regex.Matches(FlowDoc.Text, searchText);

      if (matches.Count > 0)
         FlowDoc.Select(matches[0].Index, matches[0].Length);

      //foreach (Match m in matches)
      //   FlowDoc.ApplyFormattingRange(Inline.BackgroundProperty, Brushes.Red, new TextRange(FlowDoc, m.Index, m.Index + m.Length));


   }
    
   private void Backspace()
   {
      if (FlowDoc.Selection!.Length > 0)
         FlowDoc.DeleteSelection();
      else
      {
         if (FlowDoc.Selection.Start == 0) return;

         if (FlowDoc.Selection.StartParagraph.SelectionStartInBlock == 0)
         {
            FlowDoc.MoveSelectionLeft(true);
            FlowDoc.MergeParagraphForward(FlowDoc.Selection.StartParagraph, true);
         }
         else
         {
            FlowDoc.MoveSelectionLeft(true);
            DeleteChar();
         }
      }

   }
}
