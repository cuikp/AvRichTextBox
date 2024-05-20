using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using DocumentFormat.OpenXml.Packaging;
using DynamicData;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Linq;
using static AvRichTextBox.WordConversions;

namespace AvRichTextBox;

public partial class RichTextBox : UserControl
{
   
   //public delegate void Status_ChangedHandler(string statusText);
   //public event Status_ChangedHandler? Status_Changed;

   public FlowDocument FlowDoc => rtbVM.FlowDoc;

   private Rectangle? _CursorRect = new Rectangle()
   {
      StrokeThickness = 2,
      Stroke = Brushes.Black,
      Height = 20,
      Width = 1.5,
      IsVisible = false,
      HorizontalAlignment = HorizontalAlignment.Left,
      VerticalAlignment = VerticalAlignment.Top
   };

   RichTextBoxViewModel rtbVM { get; set; } = new();

   public RichTextBox()
   {
      InitializeComponent();

      this.DataContext = rtbVM;

      this.Loaded += RichTextBox_Loaded;
      this.Initialized += RichTextBox_Initialized;

      FlowDocSV.SizeChanged += FlowDocSV_SizeChanged;

      AdornerLayer.SetAdorner(DocIC, _CursorRect);

      InitializeBlinkAnimation();

      blinkAnimation!.RunAsync(_CursorRect);
      _CursorRect.Bind(IsVisibleProperty, new Binding("CursorVisible"));
      _CursorRect.Bind(MarginProperty, new Binding("CursorMargin"));
      _CursorRect.Bind(HeightProperty, new Binding("CursorHeight"));
      _CursorRect.DataContext = rtbVM;

      this.TextInput += RichTextBox_TextInput;

      //Necessary to capture pagedown/up events for scrollviewer
      FlowDocSV.AddHandler(KeyDownEvent, FlowDocSV_KeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);

      

   }

   public void CloseDocument()
   {

      FlowDoc.NewDocument();

      rtbVM.RTBScrollOffset = new Vector(0, 0);
      this.UpdateLayout();

      InitializeDocument();

   }

   private void InitializeDocument()
   {
      this.Focus();

#if DEBUG
      RunDebugger.DataContext = FlowDoc;
#else
      RunDebugger.DataContext = null;
#endif

      FlowDoc.InitializeDocument();

      this.UpdateLayout();
      this.InvalidateVisual();

   }

   public void LoadWordDoc(string fileName)
   {
      FlowDoc.LoadWordDoc(fileName);
      
   }

   public void SaveAsWord(string filename)
   {
      FlowDoc.SaveWordDoc(filename);
   }

   public void LoadXaml (string fileName)
   {
      FlowDoc.LoadXaml(fileName);
   }

   public void SaveXamlPackage (string fileName)
   {
      FlowDoc.SaveXamlPackage(fileName);
   }

   public void SaveXaml (string fileName)
   {
      FlowDoc.SaveXaml(fileName);
   }

   public void LoadXamlPackage (string fileName)
   {
      FlowDoc.LoadXamlPackage(fileName);

      //FlowDocSV.Focus();
      //FlowDoc.Selection.StartParagraph = (Paragraph)FlowDoc.Blocks[0];
      //FlowDoc.UpdateSelection();
      
   }

   private void FlowDocSV_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
   {
      if (e.Key == Avalonia.Input.Key.PageDown || e.Key == Avalonia.Input.Key.PageUp)
         e.Handled = true;

   }

   private void MovePage(int direction, bool extend)
   {
      double currentY = 0;
      switch (FlowDoc.SelectionExtendMode)
      {
         case FlowDocument.ExtendMode.ExtendModeRight:
         case FlowDocument.ExtendMode.ExtendModeNone:
            currentY = FlowDoc.Selection!.EndRect!.Y;
            break;

         case FlowDocument.ExtendMode.ExtendModeLeft:
            currentY = FlowDoc.Selection!.StartRect!.Y;
            break;
      }

      double distanceFromTop = currentY - rtbVM.RTBScrollOffset.Y;
      double distanceFromLeft = FlowDoc.Selection!.StartRect!.X + FlowDocSV.Margin.Left;
      double newScrollY = rtbVM.RTBScrollOffset.Y + FlowDocSV.Bounds.Height * direction;
      rtbVM.RTBScrollOffset = rtbVM.RTBScrollOffset.WithY(newScrollY);
      double newCaretY = newScrollY + distanceFromTop;

      //Debug.WriteLine("\nnewCaretY = " + newCaretY + "\nnewscrollY= " + newScrollY + "\ndistanceTop=" + distanceFromTop);

      EditableParagraph? thisEP = DocIC.GetVisualDescendants().OfType<EditableParagraph>().Where(ep => ep.TranslatePoint(ep.Bounds.Position, DocIC)!.Value.Y <= newCaretY).LastOrDefault();

      if (thisEP == null)
      {
         if (direction == -1)
            FlowDoc.Select(0, 0);
      }
      else
      {
         double relYInEP = newCaretY - thisEP!.TranslatePoint(thisEP!.Bounds.Position, DocIC)!.Value.Y + 18;
         TextHitTestResult tres = thisEP.TextLayout.HitTestPoint(new Point(distanceFromLeft, relYInEP));
         int newCharIndexInDoc = ((Paragraph)thisEP.DataContext!).StartInDoc + tres.CharacterHit.FirstCharacterIndex;
         FlowDoc.MovePageSelection(direction, extend, newCharIndexInDoc);
      }

   }


   private void FlowDocSV_SizeChanged(object? sender, SizeChangedEventArgs e)
   {
      rtbVM.ScrollViewerHeight = e.NewSize.Height;

   }
     
   private Animation blinkAnimation;

   private void InitializeBlinkAnimation()
   {
      blinkAnimation = new Animation()
      {
         Duration = TimeSpan.FromSeconds(0.85),
         FillMode = FillMode.Forward,
         IterationCount = IterationCount.Infinite,
         Children =
            {
                new KeyFrame { Cue = new Cue(0.0), Setters = { new Setter(Rectangle.OpacityProperty, 0.0) } },
                new KeyFrame { Cue = new Cue(0.5), Setters = { new Setter(Rectangle.OpacityProperty, 1.0) } },
                new KeyFrame { Cue = new Cue(1.0), Setters = { new Setter(Rectangle.OpacityProperty, 0.0) } }
            }
      };
   }


   private void RichTextBox_Initialized(object? sender, EventArgs e)
   {
      //selRect.Points = RecreatePolygonPoints();
   }

   private void RichTextBox_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
   
      this.Focus();
      
      InitializeDocument();

   }

   
   private void EditableParagraph_CharIndexRect_Notified(EditableParagraph edPar, Rect selStartRect)
   {
      //Point? selStartPoint = edPar.TranslatePoint(selStartRect.Position, DocIC);
      //if (selStartPoint != null)
      //   FlowDoc.Selection.StartRect = new Rect((Point)selStartPoint!, selStartRect.Size);

   }
   
   private void SelectionStart_RectChanged(EditableParagraph edPar)
   {
      edPar.UpdateLayout();

      Rect selStartRect = edPar.TextLayout.HitTestTextPosition(edPar.SelectionStart);

      Point? selStartPoint = edPar.TranslatePoint(selStartRect.Position, DocIC);
      if (selStartPoint != null)
         FlowDoc.Selection.StartRect = new Rect((Point)selStartPoint!, selStartRect.Size);

      Paragraph thisPar = (Paragraph)edPar.DataContext!;


      thisPar.DistanceSelectionStartFromLeft = edPar.TextLayout.HitTestTextPosition(edPar.SelectionStart).Left;

      int lineNo = edPar.TextLayout.GetLineIndexFromCharacterIndex(edPar.SelectionStart, false);
      thisPar.IsStartAtFirstLine = lineNo == 0;

      //Debug.WriteLine("istartfirstline? " + thisPar.IsStartAtFirstLine);

      thisPar.IsStartAtLastLine = lineNo == edPar.TextLayout.TextLines.Count - 1;
      
      if (thisPar.IsStartAtFirstLine)
         thisPar.CharPrevLineStart = edPar.SelectionStart;
      else
         thisPar.CharPrevLineStart = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionStartFromLeft, -1);

      if (thisPar.IsStartAtLastLine)
         thisPar.CharNextLineStart = edPar.SelectionEnd - edPar.TextLayout.TextLines[lineNo].FirstTextSourceIndex;
      else
         thisPar.CharNextLineStart = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionStartFromLeft, 1);

      thisPar.FirstIndexStartLine = edPar.TextLayout.TextLines[lineNo].FirstTextSourceIndex;
      thisPar.FirstIndexLastLine = edPar.TextLayout.TextLines[^1].FirstTextSourceIndex;


      rtbVM.UpdateCursor();

   }

   private void SelectionEnd_RectChanged(EditableParagraph edPar)
   {

      edPar.UpdateLayout();

      Rect selEndRect = edPar.TextLayout.HitTestTextPosition(edPar.SelectionEnd);

      Point? selEndPoint = edPar.TranslatePoint(selEndRect.Position, DocIC);
      if (selEndPoint != null)
         FlowDoc.Selection.EndRect = new Rect((Point)selEndPoint!, selEndRect.Size);

      Paragraph thisPar = (Paragraph)edPar.DataContext!;

      thisPar.DistanceSelectionEndFromLeft = edPar.TextLayout.HitTestTextPosition(edPar.SelectionEnd).Left;
      int lineNo = edPar.TextLayout.GetLineIndexFromCharacterIndex(edPar.SelectionEnd, false);
      thisPar.IsEndAtLastLine = lineNo == edPar.TextLayout.TextLines.Count - 1;

      thisPar.IsEndAtFirstLine = lineNo == 0;
      if (thisPar.IsEndAtLastLine)
      {
         thisPar.LastIndexEndLine = thisPar.BlockLength; 
         thisPar.CharNextLineEnd = edPar.Text!.Length + 1 + edPar.SelectionEnd - edPar.TextLayout.TextLines[lineNo].FirstTextSourceIndex;
      }
      else
      {
         thisPar.LastIndexEndLine = edPar.TextLayout.TextLines[lineNo + 1].FirstTextSourceIndex - 1;
         thisPar.CharNextLineEnd = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionEndFromLeft, 1);
      }

      if (!thisPar.IsEndAtFirstLine)
         thisPar.CharPrevLineEnd = GetClosestIndex(edPar, lineNo, thisPar.DistanceSelectionEndFromLeft, -1);


      thisPar.FirstIndexLastLine = edPar.TextLayout.TextLines[^1].FirstTextSourceIndex;

      rtbVM.UpdateCursor();

   }

   private int GetClosestIndex(EditableParagraph edPar, int lineNo, double distanceFromLeft, int direction)
   {
      CharacterHit chit = edPar.TextLayout.TextLines[lineNo + direction].GetCharacterHitFromDistance(distanceFromLeft);

      double CharDistanceDiffThis = Math.Abs(distanceFromLeft - edPar.TextLayout.HitTestTextPosition(chit.FirstCharacterIndex).Left);
      double CharDistanceDiffNext = Math.Abs(distanceFromLeft - edPar.TextLayout.HitTestTextPosition(chit.FirstCharacterIndex + 1).Left);

      if (CharDistanceDiffThis > CharDistanceDiffNext)
         return chit.FirstCharacterIndex + 1;
      else
         return chit.FirstCharacterIndex;


   }

   private void ScrollViewer_ScrollChanged(object? sender, Avalonia.Controls.ScrollChangedEventArgs e)
   {
      rtbVM.RTBScrollOffset = FlowDocSV.Offset;

   }


}


