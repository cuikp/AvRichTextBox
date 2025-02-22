using Avalonia;
using Avalonia.Controls;
using System;
using System.Diagnostics;

namespace AvRichTextBox;

public static class RequestExtensions
{
   //public static readonly AttachedProperty<FlowDocument> FlowDocumentProperty = AvaloniaProperty.RegisterAttached<RichTextBox, FlowDocument>("FlowDocument", typeof(RequestExtensions));
   //public static void SetFlowDocument(AvaloniaObject element, FlowDocument value) => element.SetValue(FlowDocumentProperty, value);
   //public static FlowDocument GetFlowDocument(AvaloniaObject element) => (FlowDocument)element.GetValue(FlowDocumentProperty);


   public static readonly AttachedProperty<bool> TextBoxFocusRequestedProperty = AvaloniaProperty.RegisterAttached<EditableParagraph, bool>("TextBoxFocusRequested", typeof(RequestExtensions));
   public static void SetTextBoxFocusRequested(AvaloniaObject element, bool value) => element.SetValue(TextBoxFocusRequestedProperty, value);
   public static bool GetTextBoxFocusRequested(AvaloniaObject element) => (bool)element.GetValue(TextBoxFocusRequestedProperty);

   public static readonly AttachedProperty<bool> IsInlineUpdateRequestedProperty = AvaloniaProperty.RegisterAttached<EditableParagraph, bool>("IsInlineUpdateRequested", typeof(RequestExtensions));
   public static void SetIsInlineUpdateRequested(AvaloniaObject element, bool value) => element.SetValue(IsInlineUpdateRequestedProperty, value);
   public static bool GetIsInlineUpdateRequested(AvaloniaObject element) => (bool)element.GetValue(IsInlineUpdateRequestedProperty);

   public static readonly AttachedProperty<bool> InvalidateVisualRequestedProperty = AvaloniaProperty.RegisterAttached<EditableParagraph, bool>("InvalidateVisualRequested", typeof(RequestExtensions));
   public static void SetInvalidateVisualRequested(AvaloniaObject element, bool value) => element.SetValue(InvalidateVisualRequestedProperty, value);
   public static bool GetInvalidateVisualRequested(AvaloniaObject element) => (bool)element.GetValue(InvalidateVisualRequestedProperty);

   static RequestExtensions()
   {
      TextBoxFocusRequestedProperty.Changed.Subscribe(args =>
      {
         if (args.Sender is EditableParagraph edPar && (bool)args.NewValue.Value)
         {
            edPar.Focus();
            edPar.SetValue(TextBoxFocusRequestedProperty, false);
         }
      });

      IsInlineUpdateRequestedProperty.Changed.Subscribe(args =>
      {
         if (args.Sender is EditableParagraph edPar && (bool)args.NewValue.Value)
         {
            edPar.UpdateInlines();
            edPar.SetValue(IsInlineUpdateRequestedProperty, false);
         }
      });

      InvalidateVisualRequestedProperty.Changed.Subscribe(args =>
      {
         if (args.Sender is EditableParagraph edPar && (bool)args.NewValue.Value)
         {
            edPar.UpdateLayout();
            edPar.InvalidateVisual();
            edPar.SetValue(InvalidateVisualRequestedProperty, false);
         }
      });


           


   }
}


