﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DocumentFormat.OpenXml.Drawing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace AvRichTextBox;

public class Paragraph : Block
{

   public ObservableCollection<IEditable> Inlines { get; set; } = [];

   public Paragraph()
   {
      Inlines.CollectionChanged += Inlines_CollectionChanged;
   }

   private void Inlines_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
   {
      foreach (IEditable ied in Inlines)
         ied.myParagraph = this;
            
   }

   public string ParToolTip => $"Background: {Background}\nLineSpacing: {LineSpacing}\nLineHeight: {LineHeight}";
   //public string ParToolTip => $"Background: {Background}\nLineHeight: {LineHeight}";
   
   //public string Text => string.Join("", Inlines.ToList().ConvertAll(ied => ied.InlineText));

   private Thickness _BorderThickness = new Thickness(0);
   public Thickness BorderThickness { get => _BorderThickness; set { _BorderThickness = value; NotifyPropertyChanged(nameof(BorderThickness)); } }

   private SolidColorBrush _BorderBrush = new (Colors.Transparent);
   public SolidColorBrush BorderBrush { get => _BorderBrush; set { _BorderBrush = value; NotifyPropertyChanged(nameof(BorderBrush)); } }

   private SolidColorBrush _Background = new (Colors.Transparent);
   public SolidColorBrush Background { get => _Background; set { _Background = value; NotifyPropertyChanged(nameof(Background)); } }

   //private FontFamily _FontFamily = new ("ＭＳ 明朝, Times New Roman");
   private FontFamily _FontFamily = new ("Meiryo");
   //private FontFamily _FontFamily = "Meiryo";
   public FontFamily FontFamily { get => _FontFamily; set { _FontFamily = value; NotifyPropertyChanged(nameof(FontFamily)); } }

   private double _FontSize = 16D;
   public double FontSize { get => _FontSize; set { _FontSize = value; NotifyPropertyChanged(nameof(FontSize)); } }

   private double _LineHeight = 18.666D;  // fontsize normally
   public double LineHeight { get => _LineHeight; set { _LineHeight = value; NotifyPropertyChanged(nameof(LineHeight)); CallRequestInlinesUpdate(); CallRequestTextLayoutInfoStart(); } }

   private double _LineSpacing = 0D;
   public double LineSpacing { get => _LineSpacing; set { _LineSpacing = value; NotifyPropertyChanged(nameof(LineSpacing)); CallRequestInlinesUpdate(); CallRequestTextLayoutInfoStart(); } }

   private FontWeight _FontWeight = FontWeight.Normal;
   public FontWeight FontWeight { get => _FontWeight; set { _FontWeight = value; NotifyPropertyChanged(nameof(FontWeight)); } }

   private FontStyle _FontStyle = FontStyle.Normal;
   public FontStyle FontStyle{ get => _FontStyle; set { _FontStyle = value; NotifyPropertyChanged(nameof(FontStyle)); } }

   private TextAlignment _TextAlignment = TextAlignment.Left;
   public TextAlignment TextAlignment { get => _TextAlignment; set { _TextAlignment = value; NotifyPropertyChanged(nameof(TextAlignment)); } }

   //private SolidColorBrush _SelectionForegroundBrush = new (Colors.Black);  // in Avalonia > 11.1, setting this alters the selection font for some reason
   //public SolidColorBrush SelectionForegroundBrush { get => _SelectionForegroundBrush; set { _SelectionForegroundBrush = value; NotifyPropertyChanged(nameof(SelectionForegroundBrush)); } }

   private SolidColorBrush _SelectionBrush = LightBlueBrush;
   public SolidColorBrush SelectionBrush { get => _SelectionBrush; set { _SelectionBrush = value; NotifyPropertyChanged(nameof(SelectionBrush)); } }
   internal static SolidColorBrush LightBlueBrush = new(Colors.LightBlue);

   internal double DistanceSelectionEndFromLeft = 0;
   internal double DistanceSelectionStartFromLeft = 0;
   internal int CharNextLineEnd = 0;
   internal int CharPrevLineEnd = 0;
   internal int CharNextLineStart = 0;
   internal int CharPrevLineStart = 0;
   internal int FirstIndexStartLine = 0;  //For home key
   internal int LastIndexEndLine = 0;  //For end key
   internal int FirstIndexLastLine = 0;  //For moving to previous paragraph

   internal bool IsStartAtFirstLine = false;
   internal bool IsEndAtFirstLine = false;
   internal bool IsStartAtLastLine = false;
   internal bool IsEndAtLastLine = false;

   private bool _RequestInlinesUpdate;
   internal bool RequestInlinesUpdate { get => _RequestInlinesUpdate; set { _RequestInlinesUpdate = value; NotifyPropertyChanged(nameof(RequestInlinesUpdate)); } }

   private bool _RequestInvalidateVisual;
   internal bool RequestInvalidateVisual { get => _RequestInvalidateVisual; set { _RequestInvalidateVisual = value; NotifyPropertyChanged(nameof(RequestInvalidateVisual)); } }

   private bool _RequestTextLayoutInfoStart;
   internal bool RequestTextLayoutInfoStart { get => _RequestTextLayoutInfoStart; set { _RequestTextLayoutInfoStart = value; NotifyPropertyChanged(nameof(RequestTextLayoutInfoStart)); } }

   private bool _RequestTextLayoutInfoEnd;
   internal bool RequestTextLayoutInfoEnd { get => _RequestTextLayoutInfoEnd; set { _RequestTextLayoutInfoEnd = value; NotifyPropertyChanged(nameof(RequestTextLayoutInfoEnd)); } }

   private bool _RequestTextBoxFocus;
   public bool RequestTextBoxFocus { get => _RequestTextBoxFocus; set { _RequestTextBoxFocus = value; NotifyPropertyChanged(nameof(RequestTextBoxFocus)); } }

   //private int _RequestRectOfCharacterIndex;
   //public int RequestRectOfCharacterIndex { get => _RequestRectOfCharacterIndex; set { _RequestRectOfCharacterIndex = value; NotifyPropertyChanged(nameof(RequestRectOfCharacterIndex)); } }

   internal void CallRequestTextBoxFocus() { RequestTextBoxFocus = true; RequestTextBoxFocus = false; }
   internal void CallRequestInvalidateVisual() { RequestInvalidateVisual = true; RequestInvalidateVisual = false; }
   internal void CallRequestInlinesUpdate() { RequestInlinesUpdate = true; RequestInlinesUpdate = false; }
   internal void CallRequestTextLayoutInfoStart() { RequestTextLayoutInfoStart = true; RequestTextLayoutInfoStart = false; }
   internal void CallRequestTextLayoutInfoEnd() { RequestTextLayoutInfoEnd = true; RequestTextLayoutInfoEnd = false; }
   //internal void CallRequestTextLayoutInfoStart() { RequestTextLayoutInfoStart = false; RequestTextLayoutInfoStart = true; }
   //internal void CallRequestTextLayoutInfoEnd() { RequestTextLayoutInfoEnd = false; RequestTextLayoutInfoEnd = true; }

   internal void UpdateEditableRunPositions()
   {
      int sum = 0;
      for (int edx = 0; edx < Inlines.Count; edx++)
      {
         Inlines[edx].TextPositionOfInlineInParagraph = sum;
         sum += Inlines[edx].InlineLength;
      }
   }

   internal void UpdateUIContainersSelected()
   {
      if (this.Inlines != null)
      {

         IEditable? startInline = Inlines.FirstOrDefault(il => il.IsStartInline);
         IEditable? endInline = Inlines.FirstOrDefault(il => il.IsEndInline);
         foreach (EditableInlineUIContainer iuc  in this.Inlines.OfType<EditableInlineUIContainer>())
         {
            int stidx = startInline == null ? -1 : this.Inlines.IndexOf(startInline);
            int edidx = endInline == null ? Int32.MaxValue : this.Inlines.IndexOf(endInline);
            int thisidx = this.Inlines.IndexOf(iuc);
            iuc.IsSelected = (thisidx > stidx && thisidx < edidx);
         }
      }

   }

   internal bool RemoveEmptyInlines()
   {
      for (int iedno = this.Inlines.Count - 1; iedno >= 0; iedno -= 1)
         if (this.Inlines[iedno].InlineText == "")
            this.Inlines.RemoveAt(iedno);

      return this.Inlines.Count == 0;

   }

   internal Paragraph PropertyClone()
   {
      return new Paragraph() 
      { 
         TextAlignment = this.TextAlignment,
         LineSpacing = this.LineSpacing,
         BorderBrush = this.BorderBrush,
         BorderThickness = this.BorderThickness,
         LineHeight = this.LineHeight,
         Margin= this.Margin,
         Background = this.Background,
         FontFamily = this.FontFamily,
         FontSize = this.FontSize,
         FontStyle = this.FontStyle,
         FontWeight = this.FontWeight
      }; 
   }

   internal Paragraph FullClone()
   {
      return new Paragraph() 
      { 
         TextAlignment = this.TextAlignment,
         LineSpacing = this.LineSpacing,
         BorderBrush = this.BorderBrush,
         BorderThickness = this.BorderThickness,
         LineHeight = this.LineHeight,
         Margin= this.Margin,
         Background = this.Background,
         FontFamily = this.FontFamily,
         FontSize = this.FontSize,
         FontStyle = this.FontStyle,
         FontWeight = this.FontWeight,
         Inlines = new ObservableCollection<IEditable>(this.Inlines.Select(il=>il.Clone()))
      }; 
   }

   

}
