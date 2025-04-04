using Avalonia.Input;
using System.Diagnostics;


namespace AvRichTextBox;

public partial class RichTextBox
{  

   private void RichTextBox_KeyDown(object? sender, KeyEventArgs e)
   {

      if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
      {
         e.Handled = true;

         switch (e.Key)
         {
            case Key.I:
               ToggleItalics();
               break;

            case Key.B:
               ToggleBold();
               break;

            case Key.U:
               ToggleUnderlining();
               break;

            case Key.C:
               CopyToClipboard();
               break;

            case Key.V:
               PasteFromClipboard();
               break;

            case Key.Home: // Ctrl-Home 
               FlowDoc.MoveToDocStart();
               FlowDocSV.ScrollToHome();
               break;

            case Key.End: // Ctrl-End 
               FlowDoc.MoveToDocEnd();
               break;

            case Key.Z:
               FlowDoc.Undo();
               break;

            case Key.A:
               FlowDoc.SelectAll();
               break;

            case Key.Delete:
               FlowDoc.DeleteWord();
               break;

            case Key.Back:
               FlowDoc.BackWord();
               break;

            case Key.Right:
               FlowDoc.MoveRightWord();
               break;

            case Key.Left:
               FlowDoc.MoveLeftWord();
               break;
         }
      }
      else
      {

         switch (e.Key)
         {
            case Key.Escape:

               if (PreeditOverlay.IsVisible)
                  HideIMEOverlay();

               break;

            case Key.Enter:
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  InsertLineBreak();
               else
                  InsertParagraph();
               break;
            case Key.Home:
               FlowDoc.MoveToStartOfLine(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
               break;

            case Key.End:
               FlowDoc.MoveToEndOfLine(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
               break;

            case Key.Right:
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  FlowDoc.ExtendSelectionRight();
               else
                  FlowDoc.MoveSelectionRight(false);
               FlowDoc.ResetInsertFormatting();
               break;

            case Key.Left:
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  FlowDoc.ExtendSelectionLeft();
               else
                  FlowDoc.MoveSelectionLeft(false);
               FlowDoc.ResetInsertFormatting();
               break;

            case Key.Up:
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  FlowDoc.ExtendSelectionUp();
               else
                  FlowDoc.MoveSelectionUp(false);
               break;

            case Key.Down:
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  FlowDoc.ExtendSelectionDown();
               else
                  FlowDoc.MoveSelectionDown(true);
               break;

            case Key.Back:
               Backspace();
               break;

            case Key.Delete:
               if (FlowDoc.Selection!.Length > 0)
                  FlowDoc.DeleteSelection();
               else
                  DeleteChar();
               break;

            case Key.PageDown:
               MovePage(1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
               break;

            case Key.PageUp:
               
               MovePage(-1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));

               break;

         }

         rtbVM.CursorVisible = (rtbVM.FlowDoc.Selection.Length == 0);
         if (client != null)
            UpdatePreeditOverlay();

      }


   }



}


