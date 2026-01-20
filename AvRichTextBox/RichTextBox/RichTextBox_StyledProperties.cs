using Avalonia;
using Avalonia.Data;

namespace AvRichTextBox;

public partial class RichTextBox
{
   public static readonly StyledProperty<FlowDocument> FlowDocumentProperty =
   AvaloniaProperty.Register<RichTextBox, FlowDocument>(nameof(FlowDocument), defaultValue: new FlowDocument(), defaultBindingMode: BindingMode.TwoWay);

   public FlowDocument FlowDocument
   {
      get => GetValue(FlowDocumentProperty);
      set => SetValue(FlowDocumentProperty, value);
   }

   public static readonly StyledProperty<bool> ShowDebuggerPanelInDebugModeProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(ShowDebuggerPanelInDebugMode), false);
   public bool ShowDebuggerPanelInDebugMode
   {

      get => GetValue(ShowDebuggerPanelInDebugModeProperty);
      set { SetValue(ShowDebuggerPanelInDebugModeProperty, value); ToggleDebuggerPanel(value); }
   }

   public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(IsReadOnly), false);
   public bool IsReadOnly
   {
      get => GetValue(IsReadOnlyProperty);
      set { SetValue(IsReadOnlyProperty, value);  }
   }

   public static readonly StyledProperty<bool> LineBreakOnShiftEnterProperty = AvaloniaProperty.Register<RichTextBox, bool>(nameof(LineBreakOnShiftEnter), false);
   public bool LineBreakOnShiftEnter
   {
      get => GetValue(LineBreakOnShiftEnterProperty);
      set { SetValue(LineBreakOnShiftEnterProperty, value);  }
   }


}
