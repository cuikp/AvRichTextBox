using Avalonia;
using Avalonia.Threading;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static AvRichTextBox.FlowDocument;

namespace AvRichTextBox;

public class RichTextBoxViewModel : INotifyPropertyChanged
{
   public event PropertyChangedEventHandler? PropertyChanged;
   private void NotifyPropertyChanged([CallerMemberName] String propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   public delegate void FlowDocChanged_Handler();
   internal event FlowDocChanged_Handler? FlowDocChanged;
      
   private Vector _RTBScrollOffset = new (0, 0);
   public Vector RTBScrollOffset { get => _RTBScrollOffset; set { if (_RTBScrollOffset != value) _RTBScrollOffset = value; NotifyPropertyChanged(nameof(RTBScrollOffset)); } }

   public double MinWidth => RunDebuggerVisible ? 500 : 100;

   private FlowDocument _FlowDoc = null!;
   //public FlowDocument FlowDoc { get => _FlowDoc;  set { _FlowDoc = value; NotifyPropertyChanged(nameof(FlowDoc));  } }
   public FlowDocument FlowDoc { get => _FlowDoc;  set { _FlowDoc = value; NotifyPropertyChanged(nameof(FlowDoc)); FlowDocChanged?.Invoke();  } }

   private bool _RunDebuggerVisible = false;
   public bool RunDebuggerVisible { get => _RunDebuggerVisible; set { _RunDebuggerVisible = value; NotifyPropertyChanged(nameof(RunDebuggerVisible)); } }

   public RichTextBoxViewModel()
   {
      //FlowDoc.ScrollInDirection += FlowDoc_ScrollInDirection;
      //FlowDoc.UpdateRTBCursor += FlowDoc_UpdateRTBCursor;
   }

   internal void FlowDoc_UpdateRTBCursor()
   {
      UpdateCursorVisible();
   }

   internal double ScrollViewerHeight = 10;
   
   private double _CursorHeight = 5;
   public double CursorHeight { get => _CursorHeight; set { _CursorHeight = value; NotifyPropertyChanged(nameof(CursorHeight)); } }

   private Thickness _CursorMargin = new (0);
   public Thickness CursorMargin { get => _CursorMargin; set { _CursorMargin = value; NotifyPropertyChanged(nameof(CursorMargin)); } }

   private bool _CursorVisible = true;
   public bool CursorVisible { get => _CursorVisible; set { _CursorVisible = value; NotifyPropertyChanged(nameof(CursorVisible)); } }

   internal void UpdateCursorVisible()
   {

      FlowDoc.Selection.StartParagraph?.CallRequestInvalidateVisual();

      CursorVisible = FlowDoc.Selection.Length == 0;

   }


   internal void FlowDoc_ScrollInDirection(int direction)
   {

      double scrollPadding = 30;
      if (direction == 1)
      {
         double checkPointY = FlowDoc.Selection!.EndRect!.Y;

         if (FlowDoc.SelectionExtendMode == ExtendMode.ExtendModeLeft)
            checkPointY = FlowDoc.Selection!.StartRect!.Y;

         if (checkPointY > RTBScrollOffset.Y + ScrollViewerHeight - scrollPadding)
            RTBScrollOffset = RTBScrollOffset.WithY(checkPointY - ScrollViewerHeight + scrollPadding);
            //RTBScrollOffset = RTBScrollOffset.WithY(checkPointY + scrollPadding);
      }
      else
      {
         double checkPointY = FlowDoc.Selection!.StartRect!.Y;
         if (FlowDoc.SelectionExtendMode == ExtendMode.ExtendModeRight)
            checkPointY = FlowDoc.Selection!.EndRect!.Y;


         if (checkPointY < RTBScrollOffset.Y)
            RTBScrollOffset = RTBScrollOffset.WithY(checkPointY);
      }

   }


}
