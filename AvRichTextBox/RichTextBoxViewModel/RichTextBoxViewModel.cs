using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static AvRichTextBox.FlowDocument;

namespace AvRichTextBox;

public class RichTextBoxViewModel : INotifyPropertyChanged
{
   public event PropertyChangedEventHandler? PropertyChanged;
   private void NotifyPropertyChanged([CallerMemberName] String propertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

   public delegate void FlowDocChanged_Handler();
   internal event FlowDocChanged_Handler? FlowDocChanged;
      
   public Vector RTBScrollOffset { get; set { if (field != value) { field = value; NotifyPropertyChanged(nameof(RTBScrollOffset)); } } }

   public FlowDocument FlowDoc { get; set { field = value; NotifyPropertyChanged(nameof(FlowDoc)); FlowDocChanged?.Invoke(); } } = null!;

   public bool RunDebuggerVisible { get; set { field = value; NotifyPropertyChanged(nameof(RunDebuggerVisible)); } }
   public double MinWidth => RunDebuggerVisible ? 500 : 100;

   public RichTextBoxViewModel() {  }

   internal double ScrollViewerHeight = 10;
   
   public double CaretHeight { get; set { field = value; NotifyPropertyChanged(nameof(CaretHeight)); } } = 5;
   public Thickness CaretMargin { get; set { field = value; NotifyPropertyChanged(nameof(CaretMargin)); } } = new(0);
   public bool CaretVisible { get; set { field = value; NotifyPropertyChanged(nameof(CaretVisible)); } } = true;


   internal void CalculateCaretHeightAndPosition(TextLine currTextLine, double caretMLeft, double glyphRunHeight, bool offsetTopFromHeight, BaselineAlignment balign)
   {

      CaretHeight = currTextLine.Extent;
      if (CaretHeight == 0)
         CaretHeight = currTextLine.Height;
      CaretHeight *= 1.3; // give it a little extra vertical hangover

      if (balign != BaselineAlignment.Baseline)
         CaretHeight = glyphRunHeight;

      //Paragraph thisPar = FlowDoc.Selection.StartParagraph;


      double caretMTop = currTextLine.Start;


      //if (currTextLine.GetTextBounds(0, 1).FirstOrDefault() is TextBounds tbounds)
      //   caretMTop = tbounds.Rectangle.Top;

      //double textTopY = FlowDoc.Selection.StartRect.Top + (currTextLine.Extent == 0 ? 0 : Math.Max(0, currTextLine.Baseline - currTextLine.Extent));
      double textTopY = FlowDoc.Selection.StartRect.Top; 

      if (FlowDoc.Selection.IsAtEndOfLineSpace)
      {
         caretMLeft = FlowDoc.Selection.PrevCharRect.Right;
         caretMTop = FlowDoc.Selection.PrevCharRect.Top + 1;
      }
      else
         caretMTop = textTopY;


      if (offsetTopFromHeight)
         caretMTop += (currTextLine.Height - glyphRunHeight - 1);

      CaretMargin = new Thickness(caretMLeft, caretMTop, 0, 0);



   }

   //// FOR VISUAL CARET TESTING////////////////////////////////////////
   //public double LineHeightRectHeight { get; set { field = value; NotifyPropertyChanged(nameof(LineHeightRectHeight)); } } = 5;
   //public Thickness LineHeightRectMargin { get; set { field = value; NotifyPropertyChanged(nameof(LineHeightRectMargin)); } } = new(0);
   //public double BaseLineRectHeight { get; set { field = value; NotifyPropertyChanged(nameof(BaseLineRectHeight)); } } = 5; 
   //public Thickness BaseLineRectMargin { get; set { field = value; NotifyPropertyChanged(nameof(BaseLineRectMargin)); } } = new(0);
   ////////////////////////////////////////////////////////////////////

   internal void FlowDoc_UpdateRTBCaret() { UpdateCaretVisible(); }

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
         double checkPointY = FlowDoc.Selection.EndRect.Y;

         if (FlowDoc.SelectionExtendMode == ExtendMode.ExtendModeLeft)
            checkPointY = FlowDoc.Selection.StartRect.Y;

         if (checkPointY > RTBScrollOffset.Y + ScrollViewerHeight - scrollPadding)
            RTBScrollOffset = RTBScrollOffset.WithY(checkPointY - ScrollViewerHeight + scrollPadding);
      }
      else
      {
         double checkPointY = FlowDoc.Selection.StartRect.Y;
         if (FlowDoc.SelectionExtendMode == ExtendMode.ExtendModeRight)
            checkPointY = FlowDoc.Selection.EndRect.Y;

         if (checkPointY < RTBScrollOffset.Y)
            RTBScrollOffset = RTBScrollOffset.WithY(checkPointY);
      }
   }


}
