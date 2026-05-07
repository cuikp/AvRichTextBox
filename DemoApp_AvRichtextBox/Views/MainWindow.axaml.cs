using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvRichTextBox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DemoApp_AvRichtextBox.Views;

public partial class MainWindow : Window
{

   public static List<string> GetAllFonts 
   {
      get
      {
         List<string> returnList = [];
         foreach (var font in FontManager.Current.SystemFonts)
            returnList.Add(font.Name);
         return returnList;
      }
      
   }

   public MainWindow()
   {
      InitializeComponent();


      Loaded += MainWindow_Loaded;
    
      FontsCB.ItemsSource = GetAllFonts;

      UnderCB.AddHandler(InputElement.PointerReleasedEvent, UnderCB_PointerReleased, RoutingStrategies.Tunnel);
      StrikeCB.AddHandler(InputElement.PointerReleasedEvent, StrikeCB_PointerReleased, RoutingStrategies.Tunnel);
      OverCB.AddHandler(InputElement.PointerReleasedEvent, OverCB_PointerReleased, RoutingStrategies.Tunnel);
      

   }

   private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      MainRTB.FlowDocument.SelectionChanged += FlowDocument_Selection_Changed;
      

#if DEBUG
      
      DockPanel debugCBPanel = new () { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom };
      TextBlock debugTB = new () { Text = "DebugPanel" };
      CheckBox debugCB = new() { Focusable = false };
      debugCB.IsCheckedChanged += DebugPanelCB_CheckedUnchecked;
      debugCB.IsChecked = true;
      debugCBPanel.Children.Add(debugTB);
      debugCBPanel.Children.Add(debugCB);
      DockPanel.SetDock(debugTB, Dock.Top);
      TopStackPanel.Children.Add(debugCBPanel);
      
#endif

      progChange = false;


      //DEBUG
      //CreateTestDocumentWithTable();
      OpenTestDocument();

   }

   internal void OpenTestDocument()
   {
      string testdoc = Path.Combine(AppContext.BaseDirectory, "TestFiles\\TestDocumentXamlPackage.xamlp");
      MainRTB.LoadXamlPackage(testdoc);
      OpenFilePath = testdoc;
   }

   internal void CreateTestDocumentWithTable()
   {
      MainRTB.FlowDocument.Blocks.Clear();

      Paragraph newPar = new(MainRTB.FlowDocument);
      newPar.Inlines.Add(new EditableRun("A "));
      newPar.Inlines.Add(new EditableRun("first"));
      newPar.Inlines.Add(new EditableRun(" H"));

      newPar.Inlines.Add(new EditableRun("2") { BaselineAlignment = BaselineAlignment.Subscript });
      newPar.Inlines.Add(new EditableRun("O"));
      newPar.Inlines.Add(new EditableRun("3") { BaselineAlignment = BaselineAlignment.Superscript });

      newPar.Inlines.Add(new EditableRun(" simple "));
      newPar.Inlines.Add(new EditableHyperlink("go to google", @"https://www.google.com"));
      newPar.Inlines.Add(new EditableRun(" line."));
      MainRTB.FlowDocument.Blocks.Add(newPar);

      //Test Table
      MainRTB.FlowDocument.Blocks.Add(new Table(5, 4, MainRTB.FlowDocument) { BorderThickness = new(1), BorderBrush = Brushes.ForestGreen, TableAlignment = Avalonia.Layout.HorizontalAlignment.Center });

      Paragraph newPar2 = new(MainRTB.FlowDocument);
      newPar2.Inlines.Add(new EditableRun("Some extra text after the table."));
      MainRTB.FlowDocument.Blocks.Add(newPar2);


      Dispatcher.UIThread.Post(() =>
      {
         MainRTB.UpdateLayout();
         MainRTB.FlowDocument.Select(0, 0);
      });

      //InitializeDocument();

   }


   private void CreateNewDocumentMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      MainRTB.CreateNewDocument();
      
   }

   private void ShowPagePaddingValue()
   {
      PagePaddingNSL.Value = MainRTB.FlowDocument.PagePadding.Left;
      PagePaddingNSR.Value = MainRTB.FlowDocument.PagePadding.Right;
      PagePaddingNST.Value = MainRTB.FlowDocument.PagePadding.Top;
      PagePaddingNSB.Value = MainRTB.FlowDocument.PagePadding.Bottom;
   }


   private void FindTextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
   {
      FindTB.Background = Brushes.White;

      if (e.Key == Key.Enter)
      {
         PerformFind();
         e.Handled = true;
      }
         
   }

   private void FindTextBox_GotFocus(object? sender, FocusChangedEventArgs e)
   {
      FindTB.Background = Brushes.White;
      this.FindTB.Focus();
   }

   private void FindTextBox_LostFocus(object? sender, FocusChangedEventArgs e)
   {
      FindTB.Background = Brushes.LightGray;
      
   }

   private void FindButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      PerformFind();

   }

   private void PerformFind()
   {
      FindTB.Background = Brushes.White;

      if (string.IsNullOrEmpty(FindTB.Text)) return;

      MatchCollection foundMatches = Regex.Matches(MainRTB.FlowDocument.Text.Replace("\r\n", "\r"), FindTB.Text);  
      Match? firstMatch = foundMatches.FirstOrDefault(m => m.Index >= MainRTB.FlowDocument.Selection.End);
      if (firstMatch != null)
      {
         MainRTB.FlowDocument.Select(firstMatch.Index, FindTB.Text.Length);
         MainRTB.ScrollToSelection();
      }
      else
      {
         FindTB.Background = Brushes.Coral;
         FindBut.Focus();
      }
         

   }

   internal void PagePaddingNSL_ValueChanged(double value)
   {
      Thickness p = MainRTB.FlowDocument.PagePadding;
      MainRTB.FlowDocument.PagePadding = new Thickness(PagePaddingNSL.Value, p.Top, p.Right, p.Bottom);
   }

   internal void PagePaddingNST_ValueChanged(double value)
   {
      Thickness p = MainRTB.FlowDocument.PagePadding;
      MainRTB.FlowDocument.PagePadding = new Thickness(p.Left, PagePaddingNST.Value, p.Right, p.Bottom);
   }

   internal void PagePaddingNSR_ValueChanged(double value)
   {
      Thickness p = MainRTB.FlowDocument.PagePadding;
      MainRTB.FlowDocument.PagePadding = new Thickness(p.Left, p.Top, PagePaddingNSR.Value, p.Bottom);
   }

   internal void PagePaddingNSB_ValueChanged(double value)
   {
      Thickness p = MainRTB.FlowDocument.PagePadding;
      MainRTB.FlowDocument.PagePadding = new Thickness(p.Left, p.Top, p.Right, PagePaddingNSB.Value);
   }


   bool progChange = true;

   private void FlowDocument_Selection_Changed(TextRange selection)
   {

      FontFamily fontFamily = MainRTB.FontFamily;

      if (!progChange)
      {
         progChange = true;


         if (selection.GetStartPar() is Paragraph selPar)
         {
            LineHeightNS.Value = selPar.LineHeight;
            ParagraphBorderNS.Value = selPar.BorderThickness.Left;
            ParBorderCP.Color = selPar.BorderBrush == null ? Colors.Transparent : selPar.BorderBrush.Color;
            ParBackgroundCP.Color = selPar.Background == null ? Colors.Transparent : selPar.Background.Color;
            fontFamily = selPar.FontFamily;

         }

         if (selection.GetFormatting(FontFamilyProperty) is FontFamily selFFP)
            fontFamily = selFFP;

         if (fontFamily.ToString() is string fontFamilyString)
         {
            if (FontsCB.Items.FirstOrDefault(it => it?.ToString() == fontFamilyString) is string foundFF)
               FontsCB.SelectedItem = foundFF; // fontFamily.ToString();
            else
               FontsCB.SelectedItem = null!;
         }

         if (selection.GetFormatting(BackgroundProperty) is ISolidColorBrush selBackground)
            HighlightCP.Color = selBackground == null ? Colors.Transparent : selBackground.Color;
         else
            HighlightCP.Color = Colors.Transparent;

         if (selection.GetFormatting(ForegroundProperty) is ISolidColorBrush selForeground)
            FontColorCP.Color = selForeground == null ? Colors.Transparent : selForeground.Color;
         else
            FontColorCP.Color = Colors.Black;


         FontSizeNS.Value = Math.Round((double)(selection.GetFormatting(FontSizeProperty) ?? 14D));

         if (selection.GetFormatting(Inline.TextDecorationsProperty) is TextDecorationCollection tdc)
         {
            StrikeCB.IsChecked = tdc.Any(tc => tc.Location == TextDecorationLocation.Strikethrough);
            UnderCB.IsChecked = tdc.Any(tc => tc.Location == TextDecorationLocation.Underline);
            OverCB.IsChecked = tdc.Any(tc => tc.Location == TextDecorationLocation.Overline);
         }
         else
         {
            StrikeCB.IsChecked = false;
            UnderCB.IsChecked = false;
            OverCB.IsChecked = false;
         }

         progChange = false;
      }

   } 
      

   internal void FontSizeNS_UserValueChanged(double value)
   {
      MainRTB.FlowDocument.Selection.ApplyFormatting(FontSizeProperty, value);

   }

   internal void LineHeightNS_UserValueChanged(double value)
   {
      foreach (Paragraph p in MainRTB.FlowDocument.GetSelectedParagraphs)
      {
         //p.LineSpacing *= 2;
         p.LineHeight = value;
      }
         
                  
   }

   internal void ParagraphBorderNS_UserValueChanged(double value)
   {
      foreach (Paragraph p in MainRTB.FlowDocument.GetSelectedParagraphs)
         p.BorderThickness = new Thickness(value);
         
   }

   private void ParBorder_ColorChanged(object? sender, ColorChangedEventArgs e)
   {
      if (progChange) return;
      SolidColorBrush hBrush = new(e.NewColor);
      foreach (Paragraph p in MainRTB.FlowDocument.GetSelectedParagraphs)
         p.BorderBrush = hBrush;
            
   }

   private void ParBackground_ColorChanged(object? sender, ColorChangedEventArgs e)
   {
      if (progChange) return;
      SolidColorBrush hBrush = new(e.NewColor);
      foreach (Paragraph p in MainRTB.FlowDocument.GetSelectedParagraphs)
         p.Background = hBrush;
      
   }


   private void FontCP_ColorChanged(object? sender, ColorChangedEventArgs e)
   {
      SolidColorBrush hBrush = new (e.NewColor);
      MainRTB.FlowDocument.Selection.ApplyFormatting(ForegroundProperty, hBrush);
   }
   
   private void HighlightCP_ColorChanged(object? sender, ColorChangedEventArgs e)
   {
      SolidColorBrush hBrush = new (e.NewColor);
      MainRTB.FlowDocument.Selection.ApplyFormatting(BackgroundProperty, hBrush);
   }

#if DEBUG
   private void DebugPanelCB_CheckedUnchecked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      if (sender is CheckBox thisCB && MainRTB != null)
         MainRTB.ShowDebuggerPanelInDebugMode = thisCB.IsChecked is bool b && b;
   }
#endif

   private void FontsComboBox_DropDownClosed(object? sender, System.EventArgs e)
   {
      if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
      {
         string? newFont = comboBox.SelectedItem.ToString();
         if (newFont != null)
            MainRTB.FlowDocument.Selection.ApplyFormatting(FontFamilyProperty, new FontFamily(newFont));
      }

   }

   private void JustificationComboBox_DropDownClosed(object? sender, System.EventArgs e)
   {
      if (sender is ComboBox cbox && cbox.SelectedItem is ComboBoxItem cbitem)
      {
         if (cbitem.Content is string selJust && MainRTB.FlowDocument.Selection.GetStartPar() is Paragraph p)
         {
            p.TextAlignment = selJust switch
            {
               "Left" => TextAlignment.Left,
               "Center" => TextAlignment.Center,
               "Right" => TextAlignment.Right,
               "Justified" => TextAlignment.Justify,
               _ => TextAlignment.Left
            };
         }
      }
      

   }

   internal void RTBZoomNS_UserValueChanged(double value)
   {
      MainRTB.Zoom = value;
   }

   private void CheckBox_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      if (sender is CheckBox cbox && cbox.IsChecked is bool b)
         MainRTB.IsReadOnly = b;
   }

   private void StrikeCB_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
   {
      if (MainRTB.FlowDocument.Selection.Length == 0) return;
      if (sender is CheckBox cb && cb.IsChecked is bool b)
      {
         ApplyDecoration(!b, TextDecorationLocation.Strikethrough); // checkbox not yet changed on PointerReleased
      }

   }

   private void UnderCB_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
   {
      if (MainRTB.FlowDocument.Selection.Length == 0) return;
      if (sender is CheckBox cb && cb.IsChecked is bool b)
      {
         ApplyDecoration(!b, TextDecorationLocation.Underline); // checkbox not yet changed on PointerReleased
      }
   }

   private void OverCB_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
   {
      if (MainRTB.FlowDocument.Selection.Length == 0) return;
      if (sender is CheckBox cb && cb.IsChecked is bool b)
      {
         ApplyDecoration(!b, TextDecorationLocation.Overline); // checkbox not yet changed on PointerReleased
      }

   }

   private void ApplyDecoration(bool on, TextDecorationLocation textDecLoc)
   {
      if (MainRTB.FlowDocument.Selection.GetFormatting(Inline.TextDecorationsProperty) is TextDecorationCollection tdc)
      {
         switch (on)  
         {
            case true:
               tdc.Add(new() { Location = textDecLoc });
               MainRTB.FlowDocument.Selection.ApplyFormatting(Inline.TextDecorationsProperty, tdc);
               break;

            case false:
               if (tdc.Where(td => td.Location == textDecLoc) is TextDecoration removeTD)
                  tdc.Remove(removeTD);
               MainRTB.FlowDocument.Selection.ApplyFormatting(Inline.TextDecorationsProperty, tdc);
               break;
         }
      }
      else
      {
         //Debug.WriteLine("textdecloc = " + textDecLoc.ToString());

         var tdec = textDecLoc switch
         {
            TextDecorationLocation.Underline => TextDecorations.Underline,
            TextDecorationLocation.Strikethrough => TextDecorations.Strikethrough,
            TextDecorationLocation.Overline => TextDecorations.Overline,
            TextDecorationLocation.Baseline => TextDecorations.Baseline,
            _ => TextDecorations.Underline
         };

         MainRTB.FlowDocument.Selection.ApplyFormatting(Inline.TextDecorationsProperty, tdec);
      }
   }


}