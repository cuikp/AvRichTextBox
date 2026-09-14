using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using static AvRichTextBox.FlowDocument;

namespace AvRichTextBox;

public partial class RichTextBox
{
   // Popup state 

   /// <summary>True while the hyperlink popup is open.</summary>
   private bool HyperlinkPopupOpen => HyperlinkPopup?.IsOpen == true;

   // Public entry points 

   /// <summary>
   /// Opens the hyperlink insert/edit popup.
   /// Called from Ctrl+K, context menu "Insert Hyperlink" and context menu "Edit Hyperlink".
   /// </summary>
   internal void OpenHyperlinkPopup()
   {
      if (IsReadOnly) return;

      // Pre-fill fields
      EditableHyperlink? existing = FlowDoc.GetHyperlinkAtSelection();
      bool isEdit = existing != null;

      HyperlinkTextBox.Text = isEdit
         ? existing!.LinkDisplayText ?? ""
         : FlowDoc.Selection.GetText();

      HyperlinkUrlBox.Text = existing?.NavigateUri ?? "";

      HyperlinkPopupTitle.Text = isEdit ? "EDIT Hyperlink" : "INSERT Hyperlink";
      HyperlinkDeleteButton.IsVisible = isEdit;

      HyperlinkPopup.IsOpen = true;
      
      if (isEdit)
         HyperlinkUrlBox.Focus();
      else if (string.IsNullOrEmpty(HyperlinkTextBox.Text))
         HyperlinkTextBox.Focus();
      else
         HyperlinkUrlBox.Focus();
   }

   // XAML event handlers (wired in XAML code-behind)

   internal void HyperlinkPopup_KeyDown(object? sender, KeyEventArgs e)
   {
      if (e.Key == Key.Escape)
      {
         CloseHyperlinkPopup();
         e.Handled = true;
      }
      else if (e.Key == Key.Enter)
      {
         ConfirmHyperlink();
         e.Handled = true;
      }
   }

   internal void HyperlinkOkButton_Click(object? sender, RoutedEventArgs e)
   {
      ConfirmHyperlink();
   }

   internal void HyperlinkDeleteButton_Click(object? sender, RoutedEventArgs e)
   {
      CloseHyperlinkPopup();
      FlowDoc.RemoveHyperlinkAtSelection();
      this.Focus();
   }

   internal void HyperlinkCancelButton_Click(object? sender, RoutedEventArgs e)
   {
      CloseHyperlinkPopup();
      this.Focus();
   }

   // Context menu handlers

   internal void InsertHyperlinkMenuItem_Click(object? sender, RoutedEventArgs e)
   {
      OpenHyperlinkPopup();
   }

   internal void EditHyperlinkMenuItem_Click(object? sender, RoutedEventArgs e)
   {
      OpenHyperlinkPopup();
   }

   internal void RemoveHyperlinkMenuItem_Click(object? sender, RoutedEventArgs e)
   {
      FlowDoc.RemoveHyperlinkAtSelection();
      this.Focus();
   }

   //  Private helpers 

   private void ConfirmHyperlink()
   {
      string text = HyperlinkTextBox.Text?.Trim() ?? "";
      string url = HyperlinkUrlBox.Text?.Trim() ?? "";

      CloseHyperlinkPopup();

      if (string.IsNullOrEmpty(url)) 
      {
         this.Focus();
         return;
      }

      // Use the URL as display text if text field was left empty
      if (string.IsNullOrEmpty(text))
         text = url;

        // Normalize URI – add https:// scheme if none is present
        if (!url.Contains("://") && !url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            url = $"https://{url}";

        // Case 1: caret is inside an existing hyperlink → update in place ──────
        if (FlowDoc.GetHyperlinkAtSelection() is EditableHyperlink existingHyperlink)
        {
            FlowDoc.UpdateHyperlink(existingHyperlink, text, url);
        }
        else
        {   // Insert new hyperlink from the current selection
            if (FlowDoc.Selection.GetStartPar() is Paragraph startPar)
            {
                var newHyperlink = new EditableHyperlink(text, url);
                FlowDoc.InsertHyperlinkAt(FlowDoc.Selection, newHyperlink);

                // Move caret to end of inserted hyperlink
                //FlowDoc.Select(FlowDoc.Selection.Start + text.Length, 0);
                FlowDoc.SelectionExtendMode = ExtendMode.ExtendModeNone;
            }
        }
                
      this.Focus();
   }

   private void CloseHyperlinkPopup()
   {
      HyperlinkPopup.IsOpen = false;
   }
}
