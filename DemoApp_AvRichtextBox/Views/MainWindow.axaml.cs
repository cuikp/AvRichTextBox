using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvRichTextBox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

   }

   private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      MainRTB.FlowDocument.Selection_Changed += FlowDocument_Selection_Changed;

#if DEBUG
      
      DockPanel debugCBPanel = new () { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom };
      TextBlock debugTB = new () { Text = "DebugPanel" };
      CheckBox debugCB = new();
      debugCB.IsCheckedChanged += DebugPanelCB_CheckedUnchecked;
      debugCB.IsChecked = true;
      debugCBPanel.Children.Add(debugTB);
      debugCBPanel.Children.Add(debugCB);
      DockPanel.SetDock(debugTB, Dock.Top);
      TopStackPanel.Children.Add(debugCBPanel);
      
#endif

      progChange = false;

      //Test Table
      MainRTB.FlowDocument.Blocks.Add(new Table(7, 5) { BorderThickness = new(1), TableAlignment = Avalonia.Layout.HorizontalAlignment.Center });
      

   }

   private void CreateNewDocumentMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      MainRTB.CloseDocument();
      
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

      if (e.Key == Avalonia.Input.Key.Enter)
      {
         PerformFind();
         e.Handled = true;
      }
   }

   private void FindTextBox_GotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
   {
      FindTB.Background = Brushes.White;
      this.FindTB.Focus();
   }

   private void FindTextBox_LostFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
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
      FontSizeNS.Value = Math.Round((double)(selection.GetFormatting(FontSizeProperty) ?? 14D));

      object? selFFP = selection.GetFormatting(FontFamilyProperty);
      if (selFFP != null)
      {
         FontFamily ffamily = (FontFamily)selFFP;
         FontsCB.SelectedItem = ffamily.ToString();
      }

      if (selection.GetStartPar() is not Paragraph selPar) return;
      if (!progChange)
      {
         progChange = true;
         LineSpacingNS.Value = selPar.LineSpacing;
         ParagraphBorderNS.Value = selPar.BorderThickness.Left;
         ParBorderCP.Color = selPar.BorderBrush.Color;
         ParBackgroundCP.Color = selPar.Background.Color;
         progChange = false;
      }

   }

   internal void FontSizeNS_UserValueChanged(double value)
   {
      MainRTB.FlowDocument.Selection.ApplyFormatting(FontSizeProperty, value);

   }

   internal void LineSpacingNS_UserValueChanged(double value)
   {
      foreach (Paragraph p in MainRTB.FlowDocument.GetSelectedParagraphs)
         p.LineSpacing = value;
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
}