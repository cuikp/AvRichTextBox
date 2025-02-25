using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using System;
using System.Linq;

namespace AvRichTextBox;

public partial class RichTextBox : UserControl
{
   public FlowDocument FlowDoc => rtbVM.FlowDoc;
   RichTextBoxViewModel rtbVM { get; set; } = new();

   public RichTextBox()
   {
      InitializeComponent();


      MainDP.DataContext = rtbVM;  // bind to child DockPanel, not the UserControl itself


      this.Loaded += RichTextBox_Loaded;
      this.Initialized += RichTextBox_Initialized;

      this.GotFocus += RichTextBox_GotFocus;

      //this.PropertyChanged += RichTextBox_PropertyChanged;

      FlowDocSV.SizeChanged += FlowDocSV_SizeChanged;

      AdornerLayer.SetAdorner(DocIC, _CursorRect);

      InitializeBlinkAnimation();

      blinkAnimation!.RunAsync(_CursorRect);
      _CursorRect.Bind(IsVisibleProperty, new Binding("CursorVisible"));
      _CursorRect.Bind(MarginProperty, new Binding("CursorMargin"));
      _CursorRect.Bind(HeightProperty, new Binding("CursorHeight"));
      _CursorRect.DataContext = rtbVM;

      this.TextInput += RichTextBox_TextInput;

      this.Focusable = true;


   }

   private void RichTextBox_GotFocus(object? sender, GotFocusEventArgs e)
   {
   }

   public void UpdateAllInlines()
   {
      
      foreach (Paragraph p in FlowDoc.Blocks.Where(b => b.IsParagraph))
      {
         p.CallRequestInlinesUpdate();
         p.CallRequestInvalidateVisual();

      }
         

   }


   internal void CreateClient()
   {
      InputMethod.SetIsInputMethodEnabled(this, true);
      this.TextInputMethodClientRequested += RichTextBox_TextInputMethodClientRequested;

      client = new RichTextBoxTextInputClient(this);

      //Debug.WriteLine("created new client)");

      this.Focus();

   }

   RichTextBoxTextInputClient client = null!;

   private void RichTextBox_TextInputMethodClientRequested(object? sender, TextInputMethodClientRequestedEventArgs e)
   {
     
      if (e.GetType() == typeof(TextInputMethodClientRequestedEventArgs))
      {
         if (client == null)
            client = new RichTextBoxTextInputClient(this);
        
         e.Client = client;

         //Debug.WriteLine("e.Client requested = " + e.Client.Selection.ToString());

      }

   }

   string _preeditText = "";

   public void InsertPreeditText(string preeditText)
   {      
      _preeditText = preeditText;
      //Debug.WriteLine("preditexttext = *" + _preeditText + "*");
      UpdatePreeditOverlay();
      
   }

   internal Point CursorPosition { get; set; }
   public Point GetCurrentPosition() => CursorPosition;


   private void UpdatePreeditOverlay()
   {
      if (!string.IsNullOrEmpty(_preeditText))
      {

         double cX = _CursorRect!.Margin.Left + 5;
         double cY = _CursorRect!.Margin.Top + 7;
                  
         PreeditOverlay.Text = _preeditText;
         PreeditOverlay.Margin = new Thickness(cX, cY, 0, 0);
         PreeditOverlay.IsVisible = true;
         CursorPosition = new Point(cX, cY - rtbVM.RTBScrollOffset.Y);
         client.UpdateCursorPosition();

      }
      else
      {
         PreeditOverlay.IsVisible = false;
      }
   }

   
   
   private readonly Rectangle? _CursorRect = new()
   {
      StrokeThickness = 2,
      Stroke = Brushes.Black,
      Height = 20,
      Width = 1.5,
      IsVisible = false,
      HorizontalAlignment = HorizontalAlignment.Left,
      VerticalAlignment = VerticalAlignment.Top
   };


   public void InvalidateCursor()
   {
      rtbVM.CursorVisible = true;
         
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

      //#if DEBUG
      //      RunDebugger.DataContext = FlowDoc;
      //#else
      //      RunDebugger.DataContext = null;
      //#endif

      FlowDoc.InitializeDocument();


      this.UpdateLayout();
      this.InvalidateVisual();

   }

   public void LoadRtfDoc(string fileName)
   {
      FlowDoc.LoadRtf(fileName);
      
   }

   public void SaveRtfDoc(string fileName)
   {
      FlowDoc.SaveRtf(fileName);
      
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
                new KeyFrame { Cue = new (0.0), Setters = { new Setter(Rectangle.OpacityProperty, 0.0) } },
                new KeyFrame { Cue = new (0.5), Setters = { new Setter(Rectangle.OpacityProperty, 1.0) } },
                new KeyFrame { Cue = new (1.0), Setters = { new Setter(Rectangle.OpacityProperty, 0.0) } }
            }
      };
   }


   private void RichTextBox_Initialized(object? sender, EventArgs e)
   {
      //selRect.Points = RecreatePolygonPoints();
   }

   private void RichTextBox_Loaded(object? sender, RoutedEventArgs e)
   {
   
      this.Focus();
      
      InitializeDocument();

      CreateClient();


   }

   private void ScrollViewer_ScrollChanged(object? sender, Avalonia.Controls.ScrollChangedEventArgs e)
   {
      rtbVM.RTBScrollOffset = FlowDocSV.Offset;

   }


}

