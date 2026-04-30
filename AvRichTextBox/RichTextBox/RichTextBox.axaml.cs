using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Skia;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AvRichTextBox;

public partial class RichTextBox : UserControl
{
   internal FlowDocument FlowDoc => RtbVm.FlowDoc;
   private RichTextBoxViewModel RtbVm { get; set; } = new();

#if DEBUG
   //VISUAL DEBUGGER -Panel for visualization of runs. Only created in Debug mode, default hidden but settable by: RunDebuggerVisible
   private DebuggerPanel debuggerPanel = null!;
   private void ToggleDebuggerPanel(bool visible) { debuggerPanel?.IsVisible = visible; }
#endif


   public void ScrollToSelection()
   {
      RtbVm.RTBScrollOffset = RtbVm.RTBScrollOffset.WithY(FlowDoc.Selection.StartRect.Y - 50);

   }

   public RichTextBox()
   {
      InitializeComponent();

      this.PropertyChanged += RichTextBox_PropertyChanged;
      this.Loaded += RichTextBox_Loaded;
      this.Initialized += RichTextBox_Initialized;
      this.TextInput += RichTextBox_TextInput;
      this.GotFocus += RichTextBox_GotFocus;
      this.LostFocus += RichTextBox_LostFocus;
      this.ActualThemeVariantChanged += RichTextBox_ActualThemeVariantChanged;

      RtbVm.FlowDocChanged += RtbVM_FlowDocChanged;

      MainDP.DataContext = RtbVm;  // bind to child DockPanel, not the UserControl itself

      FlowDocSV.SizeChanged += FlowDocSV_SizeChanged;


      InitializeAdornerElements();

      this.Focusable = true;

   }

   private void InitializeAdornerElements()
   {
      InitializeBlinkAnimation();

      blinkAnimation.RunAsync(_CaretRect);

      _CaretRect.Bind(IsVisibleProperty, new Binding("CaretVisible"));
      _CaretRect.Bind(MarginProperty, new Binding("CaretMargin"));
      _CaretRect.Bind(HeightProperty, new Binding("CaretHeight"));
      _CaretRect.DataContext = RtbVm;
            
      SelectionPath.Data = _geometry;

      var panel = new Canvas();
      panel.Children.Add(SelectionPath);
      panel.Children.Add(_CaretRect);
      AdornerLayer.SetAdorner(DocIC, panel);
      //AdornerLayer.SetIsClipEnabled(panel, false);


   }


   private void RichTextBox_Initialized(object? sender, EventArgs e)
   {
      if (FlowDocument == null)
      { // only create initial FlowDocument if not already existing
         FlowDocument = new();
         FlowDoc.NewDocument();
      }

      FlowDoc.SelectionChanged += FlowDoc_Selection_Changed;
      FlowDoc.PagePaddingChanged += FlowDoc_PagePadding_Changed;

   }


   private void RichTextBox_Loaded(object? sender, RoutedEventArgs e)
   {

      ////FOR DEBUGGING
      //FlowDoc.CreateTestDocumentWithTable();


#if DEBUG
      if (ShowDebuggerPanelInDebugMode)
      {
         //Create Debugger Panel only in debug mode and if show
         debuggerPanel = new() { Width = 400, DataContext = FlowDoc };
         DockPanel.SetDock(debuggerPanel, Dock.Right);
         MainDP.Children.Insert(0, debuggerPanel);
         debuggerPanel.DataContext = RtbVm;
         debuggerPanel.Bind(Visual.IsVisibleProperty, new Binding("RunDebuggerVisible"));
         debuggerPanel.SelEndTB.Bind(TextBlock.TextProperty, new Binding("FlowDoc.Selection.End") { StringFormat = "DocSelEnd={0}" });
         debuggerPanel.SelStartTB.Bind(TextBlock.TextProperty, new Binding("FlowDoc.Selection.Start") { StringFormat = "DocSelStart={0}" });
         debuggerPanel.ParagraphsLB.ItemsSource = FlowDoc.SelectionParagraphs;
         RtbVm.RunDebuggerVisible = ShowDebuggerPanelInDebugMode;
         this.Width += (RtbVm.RunDebuggerVisible ? 400 : 0);
         FlowDoc.ShowDebugger = RtbVm.RunDebuggerVisible;
      }
#endif

      this.Focus();

      SelectionPath.Fill = this.SelectionBrush;
      UpdateSelectionIndicators();

   }


   private void RtbVM_FlowDocChanged()
   {
      DocIC.DataContext = RtbVm.FlowDoc;
      UpdateAllInlines();
   }

   private void RichTextBox_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
   {
      if (e.Property == FlowDocumentProperty)
      {
         if (FlowDoc != null)
         {
            FlowDoc.ScrollInDirection -= RtbVm.FlowDoc_ScrollInDirection;
            FlowDoc.UpdateRTBCaret -= RtbVm.FlowDoc_UpdateRTBCaret;
         }

         RtbVm.FlowDoc = FlowDocument;

         RtbVm.FlowDoc.ScrollInDirection += RtbVm.FlowDoc_ScrollInDirection;
         RtbVm.FlowDoc.UpdateRTBCaret += RtbVm.FlowDoc_UpdateRTBCaret;

         RtbVm.FlowDoc.SelectionBrush = this.SelectionBrush;

         RtbVm.FlowDoc.InitializeDocument();
         CreateClient();

      }
   
      else if (e.Property == CaretBrushProperty)
      {
         UpdateCaretBrush();
      }
   }

   private void RichTextBox_ActualThemeVariantChanged(object? sender, EventArgs e)
   {
      UpdateCaretBrush();
   }
   

   private void RichTextBox_GotFocus(object? sender, FocusChangedEventArgs e)
   {
      //Debug.WriteLine("Got focus rtb");
   }

   private void RichTextBox_LostFocus(object? sender, FocusChangedEventArgs e)
   {
      //Debug.WriteLine("lost focus rtb");
   }

   internal void UpdateAllInlines()
   {
      foreach (Paragraph p in FlowDoc.AllParagraphs)
      {
         p.CallRequestInlinesUpdate();
         p.CallRequestInvalidateVisual();

      }
   }


   public void InvalidateCaret() { RtbVm.CaretVisible = true; }
   public void NewDocument() => FlowDoc.NewDocument();
   public void CreateNewDocument() { FlowDoc.NewDocument(); RtbVm.RTBScrollOffset = new Vector(0, 0); }
   //Load/save
   public void LoadRtf(string rtf) => FlowDoc.LoadRtf(rtf);
   public void LoadRtfDoc(string fileName) => FlowDoc.LoadRtfFromFile(fileName);

   public string SaveRtf() => FlowDoc.SaveRtf();
   public void SaveRtfDoc(string fileName) => FlowDoc.SaveRtfToFile(fileName);
   public void LoadWordDoc(string fileName) => FlowDoc.LoadWordDocFromFile(fileName);
   public void SaveWordDoc(string filename) => FlowDoc.SaveWordDocToFile(filename);
   public void LoadHtml(string html) => FlowDoc.LoadHtml(html);

   public string SaveHtml() => FlowDoc.SaveHtml();
   public void LoadHtmlDoc(string fileName) => FlowDoc.LoadHtmlDocFromFile(fileName);
   public void SaveHtmlDoc(string filename) => FlowDoc.SaveHtmlDocToFile(filename);

   public void LoadXaml(string fileName) => FlowDoc.LoadXamlFromFile(fileName);
   public void SaveXamlPackage(string fileName) => FlowDoc.SaveXamlPackage(fileName);
   public void LoadXamlString(string xaml) => FlowDoc.LoadXaml(xaml);
   public string SaveXamlString() => FlowDoc.SaveXaml();
   public void SaveXaml(string fileName) => FlowDoc.SaveXamlToFile(fileName);
   public void LoadXamlPackage(string fileName) => FlowDoc.LoadXamlPackage(fileName);

   private void MovePage(int direction, bool extend)
   {
      double currentY = 0;
      switch (FlowDoc.SelectionExtendMode)
      {
         case FlowDocument.ExtendMode.ExtendModeRight:
         case FlowDocument.ExtendMode.ExtendModeNone:
            currentY = FlowDoc.Selection.EndRect.Y;
            break;

         case FlowDocument.ExtendMode.ExtendModeLeft:
            currentY = FlowDoc.Selection.StartRect.Y;
            break;
      }

      double distanceFromTop = currentY - RtbVm.RTBScrollOffset.Y;
      double distanceFromLeft = FlowDoc.Selection.StartRect.X + FlowDocSV.Margin.Left;
      double newScrollY = RtbVm.RTBScrollOffset.Y + FlowDocSV.Bounds.Height * direction;
      RtbVm.RTBScrollOffset = RtbVm.RTBScrollOffset.WithY(newScrollY);
      double newCaretY = newScrollY + distanceFromTop;
      //Debug.WriteLine("\nnewCaretY = " + newCaretY + "\nnewscrollY= " + newScrollY + "\ndistanceTop=" + distanceFromTop);
      EditableParagraph? thisEP = DocIC.GetVisualDescendants().OfType<EditableParagraph>().Where(ep => ep.TranslatePoint(ep.Bounds.Position, DocIC)!.Value.Y <= newScrollY).LastOrDefault();

      if (thisEP == null)
      {
         if (direction == -1)
         {
            if (FlowDoc.SelectionExtendMode == FlowDocument.ExtendMode.ExtendModeRight)
            {
               FlowDoc.Select(0, 0);
               FlowDoc.SelectionExtendMode = FlowDocument.ExtendMode.ExtendModeNone;
            }
            else
               FlowDoc.MovePageSelection(-1, extend, 0);

            this.Focus();
         }
      }
      else
      {
         double relYInEP = newCaretY - thisEP!.TranslatePoint(thisEP!.Bounds.Position, DocIC)!.Value.Y + 18;
         TextHitTestResult tres = thisEP.TextLayout.HitTestPoint(new Point(distanceFromLeft, relYInEP));
         int newCharIndexInDoc = ((Paragraph)thisEP.DataContext!).StartInDoc + tres.CharacterHit.FirstCharacterIndex;
         FlowDoc.MovePageSelection(direction, extend, newCharIndexInDoc + (int)(FlowDocSV.Bounds.Height / 2));

      }


   }

   private void FlowDocSV_SizeChanged(object? sender, SizeChangedEventArgs e)
   {
      RtbVm.ScrollViewerHeight = e.NewSize.Height;

      UpdateSelectionIndicators();
   }

   private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
   {
      RtbVm.RTBScrollOffset = FlowDocSV.Offset;

   }

   private Animation blinkAnimation = null!;

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

   
   private void CopySelectionMenuItem_Click(object? sender, RoutedEventArgs e)
   {
      if (DisableUserCopy) return;
      CopyToClipboard();
   }
   
   private void PasteSelectionMenuItem_Click(object? sender, RoutedEventArgs e)
   {
      if (IsReadOnly) return;
      PasteFromClipboard();
   }

   private void CutSelectionMenuItem_Click(object? sender, RoutedEventArgs e)
   {
      if (IsReadOnly) return;
      CopyToClipboard();
      FlowDoc.DeleteSelection();
   }
   
   private void DeleteSelectionMenuItem_Click(object? sender, RoutedEventArgs e)
   {
      if (IsReadOnly) return;
      FlowDoc.DeleteSelection();
   }

}

