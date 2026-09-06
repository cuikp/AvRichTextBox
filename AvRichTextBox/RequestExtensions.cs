using Avalonia;
using Avalonia.Controls;
using System;
using System.Diagnostics;

namespace AvRichTextBox;

internal static class RequestExtensions
{

    internal static readonly AttachedProperty<bool> TextBoxFocusRequestedProperty = AvaloniaProperty.RegisterAttached<EditableParagraph, bool>("TextBoxFocusRequested", typeof(RequestExtensions));
    public static void SetTextBoxFocusRequested(AvaloniaObject element, bool value) => element.SetValue(TextBoxFocusRequestedProperty, value);
    public static bool GetTextBoxFocusRequested(AvaloniaObject element) => (bool)element.GetValue(TextBoxFocusRequestedProperty);

    internal static readonly AttachedProperty<bool> IsInlineUpdateRequestedProperty = AvaloniaProperty.RegisterAttached<EditableParagraph, bool>("IsInlineUpdateRequested", typeof(RequestExtensions));
    public static void SetIsInlineUpdateRequested(AvaloniaObject element, bool value) => element.SetValue(IsInlineUpdateRequestedProperty, value);
    public static bool GetIsInlineUpdateRequested(AvaloniaObject element) => (bool)element.GetValue(IsInlineUpdateRequestedProperty);

    internal static readonly AttachedProperty<bool> InvalidateVisualRequestedProperty = AvaloniaProperty.RegisterAttached<EditableParagraph, bool>("InvalidateVisualRequested", typeof(RequestExtensions));
    public static void SetInvalidateVisualRequested(AvaloniaObject element, bool value) => element.SetValue(InvalidateVisualRequestedProperty, value);
    public static bool GetInvalidateVisualRequested(AvaloniaObject element) => (bool)element.GetValue(InvalidateVisualRequestedProperty);

    internal static readonly AttachedProperty<bool> SizeChangedRequestedProperty = AvaloniaProperty.RegisterAttached<EditableParagraph, bool>("SizeChangedRequested", typeof(RequestExtensions));
    public static void SetSizeChangedRequested(AvaloniaObject element, bool value) => element.SetValue(SizeChangedRequestedProperty, value);
    public static bool GetSizeChangedRequested(AvaloniaObject element) => (bool)element.GetValue(SizeChangedRequestedProperty);

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
            else if (args.Sender is EditableTable edTable && (bool)args.NewValue.Value)
            {
                edTable.UpdateLayout();
                edTable.UpdateBordersCanvas();
                edTable.InvalidateVisual();
                edTable.SetValue(InvalidateVisualRequestedProperty, false);
            }
        });

        SizeChangedRequestedProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is EditableParagraph edPar && (bool)args.NewValue.Value)
            {
                edPar.RecalculateFullRowHeight();
                edPar.SetValue(SizeChangedRequestedProperty, false);
            }
        });





    }
}


