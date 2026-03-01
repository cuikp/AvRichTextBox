using Avalonia.Media;
using System.ComponentModel;

namespace AvRichTextBox;

public class InlineVisualizationProperties : INotifyPropertyChanged
{
   //CLASS FOR DEBUGGER PANEL
   public event PropertyChangedEventHandler? PropertyChanged;
   private void InvokeProperty(PropertyChangedEventArgs pceArgs) { PropertyChanged?.Invoke(this, pceArgs); }

   private static readonly PropertyChangedEventArgs BackBrushChangedArgs = new(nameof(BackBrush));
   private static readonly PropertyChangedEventArgs InlineSelectedBorderThicknessChangedArgs = new(nameof(InlineSelectedBorderThickness));
   private static readonly PropertyChangedEventArgs IsTableCellInlineChangedArgs = new(nameof(IsTableCellInline));

   public bool IsStartInline { get; set { field = value; InvokeProperty(BackBrushChangedArgs); InvokeProperty(InlineSelectedBorderThicknessChangedArgs); } }
   public bool IsEndInline { get; set { field = value; InvokeProperty(BackBrushChangedArgs); InvokeProperty(InlineSelectedBorderThicknessChangedArgs); } }
   public bool IsWithinSelectionInline { get; set { field = value; InvokeProperty(BackBrushChangedArgs); } }
   public bool IsTableCellInline { get; set { field = value; InvokeProperty(IsTableCellInlineChangedArgs); } }

   public Thickness InlineSelectedBorderThickness => (IsStartInline || IsEndInline) ? new Thickness(3) : new Thickness(1);

   readonly SolidColorBrush startInlineBrush = new(Colors.LawnGreen);
   readonly SolidColorBrush endInlineBrush = new(Colors.Pink);
   readonly SolidColorBrush withinSelectionInlineBrush = new(Colors.LightGray);
   readonly SolidColorBrush transparentBrush = new(Colors.Transparent);

   public SolidColorBrush BackBrush
   {
      get
      {
         if (IsStartInline) return startInlineBrush;
         else if (IsEndInline) return endInlineBrush;
         else if (IsWithinSelectionInline) return withinSelectionInlineBrush;
         else return transparentBrush;
      }
   }

}
