using Avalonia.Data;
using Avalonia.Media;

namespace AvRichTextBox;

public partial class RichTextBox
{
   internal static readonly StyledProperty<double> ZoomProperty = AvaloniaProperty.Register<RichTextBox, double>(nameof(Zoom), defaultValue: 1);
   public double Zoom
   {
      get => GetValue(ZoomProperty);
      set => SetValue(ZoomProperty, value);
   }
      
   internal static readonly StyledProperty<FlowDocument> FlowDocumentProperty = AvaloniaProperty.Register<RichTextBox, FlowDocument>(nameof(FlowDocument), defaultValue: null!, defaultBindingMode: BindingMode.TwoWay);
   public FlowDocument FlowDocument
   {
      get => GetValue(FlowDocumentProperty);
      set => SetValue(FlowDocumentProperty, value);
   }
      
   internal static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(IsReadOnly), false);
   public bool IsReadOnly
   {
      get => GetValue(IsReadOnlyProperty);
      set { SetValue(IsReadOnlyProperty, value);  }
   }

   internal static readonly StyledProperty<bool> DisableUserCopyProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(DisableUserCopy), false);
   public bool DisableUserCopy
   {
      get => GetValue(DisableUserCopyProperty);
      set { SetValue(DisableUserCopyProperty, value);  }
   }

   internal static readonly StyledProperty<bool> LineBreakOnShiftEnterProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(LineBreakOnShiftEnter), false);
   public bool LineBreakOnShiftEnter
   {
      get => GetValue(LineBreakOnShiftEnterProperty);
      set { SetValue(LineBreakOnShiftEnterProperty, value);  }
   }

   internal static readonly StyledProperty<bool> CtrlKeyOpensHyperlinkProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(CtrlKeyOpensHyperlink), false);
   public bool CtrlKeyOpensHyperlink
   {
      get => GetValue(CtrlKeyOpensHyperlinkProperty);
      set { SetValue(CtrlKeyOpensHyperlinkProperty, value);  }
   }
   
   internal static readonly StyledProperty<bool> DisableEditingShortcutsProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(DisableEditingShortcuts), false);
   public bool DisableEditingShortcuts
   {
      get => GetValue(DisableEditingShortcutsProperty);
      set { SetValue(DisableEditingShortcutsProperty, value);  }
   }

   internal static readonly StyledProperty<bool> ShowDebuggerPanelInDebugModeProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(ShowDebuggerPanelInDebugMode), false);
   public bool ShowDebuggerPanelInDebugMode
   {
      get => GetValue(ShowDebuggerPanelInDebugModeProperty);
      set 
      { 
         SetValue(ShowDebuggerPanelInDebugModeProperty, value);
#if DEBUG
         ToggleDebuggerPanel(value); 
#endif
      }
   }


   internal static readonly StyledProperty<IBrush> SelectionBrushProperty = AvaloniaProperty.Register<RichTextBox, IBrush>(nameof(SelectionBrush), defaultValue: Brushes.DeepSkyBlue, defaultBindingMode: BindingMode.OneWay);
   public IBrush SelectionBrush
   {
      get => GetValue(SelectionBrushProperty);
      set => SetValue(SelectionBrushProperty, value);
   }

    internal static readonly StyledProperty<IBrush?> CaretBrushProperty = AvaloniaProperty.Register<RichTextBox, IBrush?>(nameof(CaretBrush), defaultValue: null, defaultBindingMode: BindingMode.OneWay);
    public IBrush? CaretBrush
    {
       get => GetValue(CaretBrushProperty);
       set => SetValue(CaretBrushProperty, value);
    }

    internal static readonly StyledProperty<bool> IsCaretVisibleProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(IsCaretVisible), defaultValue: true, defaultBindingMode: BindingMode.OneWay);
    public bool IsCaretVisible
    {
       get => GetValue(IsCaretVisibleProperty);
       set => SetValue(IsCaretVisibleProperty, value);
    }


}
