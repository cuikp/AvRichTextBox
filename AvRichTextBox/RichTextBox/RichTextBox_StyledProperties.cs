using Avalonia.Data;
using Avalonia.Media;

namespace AvRichTextBox;

public partial class RichTextBox
{
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


   public static readonly StyledProperty<IBrush> SelectionBrushProperty = AvaloniaProperty.Register<RichTextBox, IBrush>(nameof(SelectionBrush), defaultValue: Brushes.LightSteelBlue, defaultBindingMode: BindingMode.OneWay);
   public IBrush SelectionBrush
   {
      get => GetValue(SelectionBrushProperty);
      set => SetValue(SelectionBrushProperty, value);
   }


}
