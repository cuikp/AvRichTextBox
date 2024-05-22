using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using DynamicData;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Linq;

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
  
   private void ScrollViewer_ScrollChanged(object? sender, Avalonia.Controls.ScrollChangedEventArgs e)
   {
      rtbVM.RTBScrollOffset = FlowDocSV.Offset;

   }


}


