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

         if (!(e.KeyModifiers.HasFlag(KeyModifiers.Alt) || e.KeyModifiers.HasFlag(KeyModifiers.Shift)))
         {
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

               case Key.X:
                  CutToClipboard();
                  break;

               case Key.V:
                  PasteFromClipboard();
                  break;

               case Key.Z:
                  if (IsReadOnly) return;
                  FlowDoc.Undo();
                  break;

               case Key.A:
                  FlowDoc.SelectAll();
                  break;

               case Key.K:
                  OpenHyperlinkPopup();
                  break;

               case Key.Delete:
                  if (IsReadOnly) return;
                  FlowDoc.DeleteWord(false);
                  break;

               case Key.Back:
                  FlowDoc.DeleteWord(true);
                  break;

            }
         }

         switch (e.Key)
         {

            case Key.Home: // Ctrl-Home / Ctrl-Shift-Home
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  FlowDoc.ExtendSelectionToDocStart();
               else
               {
                  FlowDoc.MoveToDocStart();
                  FlowDocSV.ScrollToHome();
               }
               break;

            case Key.End: // Ctrl-End / Ctrl-Shift-End
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  FlowDoc.ExtendSelectionToDocEnd();
               else
                  FlowDoc.MoveToDocEnd();
               break;

            case Key.Right:
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  FlowDoc.ExtendSelectionRightWord();
               else
                  FlowDoc.MoveRightWord();
               break;

            case Key.Left:
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  FlowDoc.ExtendSelectionLeftWord();
               else
                  FlowDoc.MoveLeftWord();
               break;
         }
      }

      else
      {
         switch (e.Key)
         {
            case Key.Escape:
               if (HyperlinkPopupOpen)
                  CloseHyperlinkPopup();
               else if (PreeditOverlay.IsVisible)
                  HideIMEOverlay();
               break;

            case Key.Tab:
               e.Handled = true;
               if (FlowDoc.Selection.GetStartPar() is Paragraph p && p.IsTableCellBlock)
               {
                  if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  {
                     if (FlowDoc.GetPreviousParagraph(p) is Paragraph prevPar)
                        FlowDoc.Select(prevPar.StartInDoc, 0);
                  }
                  else
                  {
                     if (FlowDoc.GetNextParagraph(p) is Paragraph nextPar)
                        FlowDoc.Select(nextPar.StartInDoc, 0);
                  }
               }
               else
                  InsertTab();
               break;

            case Key.Enter:
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
               {
                  if (LineBreakOnShiftEnter)
                     InsertLineBreak();
                  else
                     InsertParagraph();
               }
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
                  FlowDoc.MoveSelectionRight();
               FlowDoc.ResetInsertFormatting();
               break;

            case Key.Left:
               if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                  FlowDoc.ExtendSelectionLeft();
               else
                  FlowDoc.MoveSelectionLeft();
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
               PerformDelete(true);
               break;

            case Key.Delete:
               PerformDelete(false);
               break;

            case Key.PageDown:
               MovePage(1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
               break;

            case Key.PageUp:
               MovePage(-1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
               break;

         }

         RtbVm.CaretVisible = (RtbVm.FlowDoc.Selection.Length == 0);
         if (client != null)
            UpdatePreeditOverlay();

      }


   }



}


