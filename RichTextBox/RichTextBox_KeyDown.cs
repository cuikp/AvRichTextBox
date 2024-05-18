using Avalonia.Input;


namespace AvRichTextBox;

public partial class RichTextBox
{
   //int CursorPosText = 0;


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
               rtbVM.FlowDoc.MoveToDocStart();
               break;

            case Key.End: // Ctrl-End 
               rtbVM.FlowDoc.MoveToDocEnd();
               break;

            case Key.Z:
               rtbVM.FlowDoc.Undo();
               break;

            case Key.A:
               rtbVM.FlowDoc.SelectAll();
               break;

            case Key.Delete:
               rtbVM.FlowDoc.DeleteWord();
               break;

            case Key.Back:
               rtbVM.FlowDoc.BackWord();
               break;

            case Key.Right:
               rtbVM.FlowDoc.MoveRightWord();
               break;

            case Key.Left:
               rtbVM.FlowDoc.MoveLeftWord();
               break;
         }
      }
      else
      {

         switch (e.Key)
         {
            case Key.Escape:
               //Do nothing
               break;

            case Key.Enter:
               InsertParagraph();
               break;
            case Key.Home:
               rtbVM.FlowDoc.MoveToStartOfLine(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
               break;

            case Key.End:
               rtbVM.FlowDoc.MoveToEndOfLine(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
               break;

            case Key.Right:
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  rtbVM.FlowDoc.ExtendSelectionRight();
               else
                  rtbVM.FlowDoc.MoveSelectionRight(true);
               break;

            case Key.Left:
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  rtbVM.FlowDoc.ExtendSelectionLeft();
               else
                  rtbVM.FlowDoc.MoveSelectionLeft(false);
               break;

            case Key.Up:
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  rtbVM.FlowDoc.ExtendSelectionUp();
               else
                  rtbVM.FlowDoc.MoveSelectionUp(false);
               break;

            case Key.Down:
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  rtbVM.FlowDoc.ExtendSelectionDown();
               else
                  rtbVM.FlowDoc.MoveSelectionDown(true);
               break;

            case Key.Back:
               Backspace();
               break;

            case Key.Delete:
               if (rtbVM.FlowDoc.Selection!.Length > 0)
                  rtbVM.FlowDoc.DeleteSelection();
               else
                  DeleteChar();
               break;

         }
      }


   }



}


