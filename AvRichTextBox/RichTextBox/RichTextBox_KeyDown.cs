using Avalonia.Input;

namespace AvRichTextBox;

public partial class RichTextBox
{
   private readonly record struct KeyCombo(
      Key Key,
      bool Ctrl = false,
      bool Shift = false,
      bool Alt = false
   );

   private Dictionary<KeyCombo, Action>? _keyActions;

   private Dictionary<KeyCombo, Action> KeyActions => _keyActions ??= new()
   {
      // Ctrl shortcuts
      { new(Key.B, Ctrl: true), () => ToggleBold() },
      { new(Key.I, Ctrl: true), () => ToggleItalics() },
      { new(Key.U, Ctrl: true), () => ToggleUnderlining() },

      { new(Key.C, Ctrl: true), () => CopyToClipboard() },
      { new(Key.X, Ctrl: true), () => CutToClipboard() },
      { new(Key.V, Ctrl: true), () => PasteFromClipboard() },
      { new(Key.V, Ctrl: true, Shift: true), () => PasteFromClipboardAsPlainText() },

      { new(Key.Z, Ctrl: true), () =>
         {
            if (IsReadOnly) return;
            FlowDoc.Undo();
         }
      },

      { new(Key.A, Ctrl: true), () => FlowDoc.SelectAll() },
      { new(Key.K, Ctrl: true), () => OpenHyperlinkPopup() },

      { new(Key.Delete, Ctrl: true), () =>
         {
            if (IsReadOnly) return;
            FlowDoc.DeleteWord(false);
         }
      },

      { new(Key.Back, Ctrl: true), () =>
         {
            if (IsReadOnly) return;
            FlowDoc.DeleteWord(true);
         }
      },

      // Ctrl navigation
      { new(Key.Home, Ctrl: true), () =>
         {
            FlowDoc.MoveToDocStart();
            FlowDocSV.ScrollToHome();
         }
      },

      { new(Key.Home, Ctrl: true, Shift: true), () =>
         {
            FlowDoc.ExtendSelectionToDocStart();
         }
      },

      { new(Key.End, Ctrl: true), () =>
         {
            FlowDoc.MoveToDocEnd();
         }
      },

      { new(Key.End, Ctrl: true, Shift: true), () =>
         {
            FlowDoc.ExtendSelectionToDocEnd();
         }
      },

      { new(Key.Right, Ctrl: true), () =>
         {
            FlowDoc.MoveRightWord();
         }
      },

      { new(Key.Right, Ctrl: true, Shift: true), () =>
         {
            FlowDoc.ExtendSelectionRightWord();
         }
      },

      { new(Key.Left, Ctrl: true), () =>
         {
            FlowDoc.MoveLeftWord();
         }
      },

      { new(Key.Left, Ctrl: true, Shift: true), () =>
         {
            FlowDoc.ExtendSelectionLeftWord();
         }
      },

      // Normal keys
      { new(Key.Escape), () =>
         {
            if (HyperlinkPopupOpen)
               CloseHyperlinkPopup();
            else if (PreeditOverlay.IsVisible)
               HideIMEOverlay();
         }
      },

      { new(Key.Tab), () =>
         {
            if (FlowDoc.Selection.GetStartPar() is Paragraph p && p.IsTableCellBlock)
            {
               if (FlowDoc.GetNextParagraph(p) is Paragraph nextPar)
                  FlowDoc.Select(nextPar.StartInDoc, 0);
            }
            else
            {
               InsertTab();
            }
         }
      },

      { new(Key.Tab, Shift: true), () =>
         {
            if (FlowDoc.Selection.GetStartPar() is Paragraph p && p.IsTableCellBlock)
            {
               if (FlowDoc.GetPreviousParagraph(p) is Paragraph prevPar)
                  FlowDoc.Select(prevPar.StartInDoc, 0);
            }
            else
            {
               InsertTab();
            }
         }
      },

      { new(Key.Enter), () =>
         {
            InsertParagraph();
         }
      },

      { new(Key.Enter, Shift: true), () =>
         {
            if (LineBreakOnShiftEnter)
               InsertLineBreak();
            else
               InsertParagraph();
         }
      },

      { new(Key.Home), () =>
         {
            FlowDoc.MoveToStartOfLine(false);
         }
      },

      { new(Key.Home, Shift: true), () =>
         {
            FlowDoc.MoveToStartOfLine(true);
         }
      },

      { new(Key.End), () =>
         {
            FlowDoc.MoveToEndOfLine(false);
         }
      },

      { new(Key.End, Shift: true), () =>
         {
            FlowDoc.MoveToEndOfLine(true);
         }
      },

      { new(Key.Right), () =>
         {
            FlowDoc.MoveSelectionRight();
            FlowDoc.ResetInsertFormatting();
         }
      },

      { new(Key.Right, Shift: true), () =>
         {
            FlowDoc.ExtendSelectionRight();
            FlowDoc.ResetInsertFormatting();
         }
      },

      { new(Key.Left), () =>
         {
            FlowDoc.MoveSelectionLeft();
            FlowDoc.ResetInsertFormatting();
         }
      },

      { new(Key.Left, Shift: true), () =>
         {
            FlowDoc.ExtendSelectionLeft();
            FlowDoc.ResetInsertFormatting();
         }
      },

      { new(Key.Up), () =>
         {
            FlowDoc.MoveSelectionUp(false);
         }
      },

      { new(Key.Up, Shift: true), () =>
         {
            FlowDoc.ExtendSelectionUp();
         }
      },

      { new(Key.Down), () =>
         {
            FlowDoc.MoveSelectionDown(true);
         }
      },

      { new(Key.Down, Shift: true), () =>
         {
            FlowDoc.ExtendSelectionDown();
         }
      },

      { new(Key.Back), () =>
         {
            PerformDelete(true);
         }
      },

      { new(Key.Delete), () =>
         {
            PerformDelete(false);
         }
      },

      { new(Key.PageDown), () =>
         {
            MovePage(1, false);
         }
      },

      { new(Key.PageDown, Shift: true), () =>
         {
            MovePage(1, true);
         }
      },

      { new(Key.PageUp), () =>
         {
            MovePage(-1, false);
         }
      },

      { new(Key.PageUp, Shift: true), () =>
         {
            MovePage(-1, true);
         }
      },
   };

   private static KeyCombo GetCombo(KeyEventArgs e) =>
      new(
         e.Key,
         Ctrl: e.KeyModifiers.HasFlag(KeyModifiers.Control),
         Shift: e.KeyModifiers.HasFlag(KeyModifiers.Shift),
         Alt: e.KeyModifiers.HasFlag(KeyModifiers.Alt)
      );

   private bool TryHandleKeyAction(KeyEventArgs e)
   {
      if (KeyActions.TryGetValue(GetCombo(e), out var action))
      {
         action();
         e.Handled = true;
         return true;
      }

      return false;
   }

   private void RichTextBox_KeyDown(object? sender, KeyEventArgs e)
   {
      TryHandleKeyAction(e);

      RtbVm.CaretVisible = (RtbVm.FlowDoc.Selection.Length == 0);
      if (client != null)
         UpdatePreeditOverlay();
   }
}