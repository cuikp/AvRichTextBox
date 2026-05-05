using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

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
         ? existing!.Text ?? ""
         : FlowDoc.Selection.GetText();

      HyperlinkUrlBox.Text = existing?.NavigateUri ?? "";

      // Update popup title and button visibility
      HyperlinkPopupTitle.Text = isEdit ? "Edit Hyperlink" : "Insert Hyperlink";
      HyperlinkDeleteButton.IsVisible = isEdit;

      // Show the popup
      HyperlinkPopup.IsOpen = true;

      // Focus URL field when editing an existing link (text is already known),
      // otherwise focus the text field (or URL field if text is pre-filled from selection).
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
      FlowDoc.RemoveHyperlink();
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
      FlowDoc.RemoveHyperlink();
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

      FlowDoc.InsertOrUpdateHyperlink(text, url);
      this.Focus();
   }

   private void CloseHyperlinkPopup()
   {
      HyperlinkPopup.IsOpen = false;
   }
}
