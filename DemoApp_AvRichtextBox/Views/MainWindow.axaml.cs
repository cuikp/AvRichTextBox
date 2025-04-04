using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvRichTextBox;
using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DemoApp_AvRichtextBox.Views;

public partial class MainWindow : Window
{

   public List<string> GetAllFonts 
   {
      get
      {
         List<string> returnList = [];
         foreach (var font in FontManager.Current.SystemFonts)
         {
            returnList.Add(font.Name);
         }
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
      
   }

   private void FindButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      PerformFind();

   }

   private void PerformFind()
   {
      FindTB.Background = Brushes.White;

      if (string.IsNullOrEmpty(FindTB.Text)) return;

      MatchCollection foundMatches = Regex.Matches(MainRTB.FlowDocument.Text, FindTB.Text);
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


   private void FlowDocument_Selection_Changed(TextRange selection)
   {
      FontSizeNS.Value = Math.Round((double)(selection.GetFormatting(FontSizeProperty) ?? 14D));

   }

   internal void FontSizeNS_UserValueChanged(double value)
   {
      MainRTB.FlowDocument.Selection.ApplyFormatting(FontSizeProperty, value);

   }

   internal void LineSpacingNS_ValueChanged(double value)
   {
      foreach (Paragraph p in MainRTB.FlowDocument.GetSelectedParagraphs)
         p.LineSpacing = value;

   }

   internal void ParagraphBorderNS_ValueChanged(double value)
   {
      foreach (Paragraph p in MainRTB.FlowDocument.GetSelectedParagraphs)
      {
         p.BorderBrush = new SolidColorBrush(Colors.Red);
         p.BorderThickness = new Thickness(value);
      }
         
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

   private void DebugPanelCB_CheckedUnchecked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      CheckBox? thisCB = sender as CheckBox;
      if (thisCB != null && MainRTB != null)
         //MainRTB.ToggleDebuggerPanel((bool)thisCB.IsChecked!);
         MainRTB.ShowDebuggerPanelInDebugMode = (bool)thisCB.IsChecked!;
   }
}