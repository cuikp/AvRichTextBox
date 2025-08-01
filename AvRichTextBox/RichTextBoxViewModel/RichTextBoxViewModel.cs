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
      //FlowDoc.UpdateRTBCaret += FlowDoc_UpdateRTBCaret;
   }

   internal void FlowDoc_UpdateRTBCaret()
   {
      UpdateCaretVisible();
   }

   internal double ScrollViewerHeight = 10;
   
   private double _CaretHeight = 5;
   public double CaretHeight { get => _CaretHeight; set { _CaretHeight = value; NotifyPropertyChanged(nameof(CaretHeight)); } }

   private Thickness _CaretMargin = new (0);
   public Thickness CaretMargin { get => _CaretMargin; set { _CaretMargin = value; NotifyPropertyChanged(nameof(CaretMargin)); } }

   private bool _CaretVisible = true;
   public bool CaretVisible { get => _CaretVisible; set { _CaretVisible = value; NotifyPropertyChanged(nameof(CaretVisible)); } }

   internal void UpdateCaretVisible()
   {

      FlowDoc.Selection.StartParagraph?.CallRequestInvalidateVisual();

      CaretVisible = FlowDoc.Selection.Length == 0;

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
