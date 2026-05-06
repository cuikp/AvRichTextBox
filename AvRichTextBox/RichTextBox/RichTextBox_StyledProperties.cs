using Avalonia.Data;
using Avalonia.Media;

namespace AvRichTextBox;

public partial class RichTextBox
{
   public static readonly StyledProperty<double> ZoomProperty = AvaloniaProperty.Register<RichTextBox, double>(nameof(Zoom), defaultValue: 1);
   public double Zoom
   {
      get => GetValue(ZoomProperty);
      set => SetValue(ZoomProperty, value);
   }
      
   public static readonly StyledProperty<FlowDocument> FlowDocumentProperty = AvaloniaProperty.Register<RichTextBox, FlowDocument>(nameof(FlowDocument), defaultValue: null!, defaultBindingMode: BindingMode.TwoWay);
   public FlowDocument FlowDocument
   {
      get => GetValue(FlowDocumentProperty);
      set => SetValue(FlowDocumentProperty, value);
   }
      
   public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(IsReadOnly), false);
   public bool IsReadOnly
   {
      get => GetValue(IsReadOnlyProperty);
      set { SetValue(IsReadOnlyProperty, value);  }
   }

   public static readonly StyledProperty<bool> DisableUserCopyProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(DisableUserCopy), false);
   public bool DisableUserCopy
   {
      get => GetValue(DisableUserCopyProperty);
      set { SetValue(DisableUserCopyProperty, value);  }
   }

   public static readonly StyledProperty<bool> LineBreakOnShiftEnterProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(LineBreakOnShiftEnter), false);
   public bool LineBreakOnShiftEnter
   {
      get => GetValue(LineBreakOnShiftEnterProperty);
      set { SetValue(LineBreakOnShiftEnterProperty, value);  }
   }

   public static readonly StyledProperty<bool> CtrlKeyOpensHyperlinkProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(CtrlKeyOpensHyperlink), false);
   public bool CtrlKeyOpensHyperlink
   {
      get => GetValue(CtrlKeyOpensHyperlinkProperty);
      set { SetValue(CtrlKeyOpensHyperlinkProperty, value);  }
   }

   public static readonly StyledProperty<bool> ShowDebuggerPanelInDebugModeProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(ShowDebuggerPanelInDebugMode), false);
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


   public static readonly StyledProperty<IBrush> SelectionBrushProperty = AvaloniaProperty.Register<RichTextBox, IBrush>(nameof(SelectionBrush), defaultValue: Brushes.DeepSkyBlue, defaultBindingMode: BindingMode.OneWay);
   public IBrush SelectionBrush
   {
      get => GetValue(SelectionBrushProperty);
      set => SetValue(SelectionBrushProperty, value);
   }

public static readonly StyledProperty<IBrush?> CaretBrushProperty = AvaloniaProperty.Register<RichTextBox, IBrush?>(nameof(CaretBrush), defaultValue: null, defaultBindingMode: BindingMode.OneWay);
    public IBrush? CaretBrush
    {
       get => GetValue(CaretBrushProperty);
       set => SetValue(CaretBrushProperty, value);
    }

    public static readonly StyledProperty<bool> IsCaretVisibleProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(IsCaretVisible), defaultValue: true, defaultBindingMode: BindingMode.OneWay);
    public bool IsCaretVisible
    {
       get => GetValue(IsCaretVisibleProperty);
       set => SetValue(IsCaretVisibleProperty, value);
    }


}
