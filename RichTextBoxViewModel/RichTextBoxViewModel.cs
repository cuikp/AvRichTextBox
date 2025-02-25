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

   private Vector _RTBScrollOffset = new (0, 0);
   public Vector RTBScrollOffset { get => _RTBScrollOffset; set { if (_RTBScrollOffset != value) _RTBScrollOffset = value; NotifyPropertyChanged(nameof(RTBScrollOffset)); } }

   private FlowDocument _FlowDoc = new();
   public FlowDocument FlowDoc { get => _FlowDoc;  set { _FlowDoc = value; NotifyPropertyChanged(nameof(FlowDoc)); } }

   public bool RunDebuggerVisible { get; set; } = false;

   public RichTextBoxViewModel()
   {

      FlowDoc.ScrollInDirection += FlowDoc_ScrollInDirection;
   
      //Create initial empty paragraph
      Paragraph hPar = new ();
      FlowDoc.Blocks.Add(hPar);
      hPar.Inlines.Add(new EditableRun(""));

#if DEBUG
      RunDebuggerVisible = true;
#endif


      

   }

   internal double ScrollViewerHeight = 10;
   
   private double _CursorHeight = 5;
   public double CursorHeight { get => _CursorHeight; set { _CursorHeight = value; NotifyPropertyChanged(nameof(CursorHeight)); } }

   private Thickness _CursorMargin = new (0);
   public Thickness CursorMargin { get => _CursorMargin; set { _CursorMargin = value; NotifyPropertyChanged(nameof(CursorMargin)); } }

   private bool _CursorVisible = true;
   public bool CursorVisible { get => _CursorVisible; set { _CursorVisible = value; NotifyPropertyChanged(nameof(CursorVisible)); } }

   internal void UpdateCursor()
   {
      //Debug.WriteLine("...and updating cursor");

      double cursorML = FlowDoc.Selection.IsAtEndOfLineSpace ? FlowDoc.Selection!.PrevCharRect!.Right : FlowDoc.Selection.StartRect!.Left;
      double cursorMT = FlowDoc.Selection.IsAtEndOfLineSpace ? FlowDoc.Selection!.PrevCharRect.Top + 2 : FlowDoc.Selection.StartRect.Top + 2;
      CursorHeight = FlowDoc.Selection.IsAtEndOfLineSpace ? FlowDoc.Selection.PrevCharRect.Height * 0.85 : FlowDoc.Selection.StartRect.Height * 0.85;

      CursorMargin = new Thickness(cursorML, cursorMT, 0, 0);

      if (FlowDoc.Selection.StartParagraph != null) 
         FlowDoc.Selection.StartParagraph.CallRequestInvalidateVisual();

      //Debug.WriteLine("sel length = " + FlowDoc.Selection.Length);

      CursorVisible = FlowDoc.Selection.Length == 0;


      //Debug.WriteLine("Cursormargin = " + cursorML);

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
