using Avalonia.Media;
using DynamicData;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AvRichTextBox;

public class Paragraph : Block
{

   public ObservableCollection<IEditable> Inlines { get; set; } = [];

   public Paragraph()
   {
      Inlines.CollectionChanged += Inlines_CollectionChanged;
      Id = ++FlowDocument.BlockIdCounter;
   }

   private void Inlines_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
   {
      foreach (IEditable ied in Inlines) 
         ied.MyParagraphId = this.Id;

      //if (e.NewItems != null)
      //{
      //   foreach (IEditable ied in e.NewItems)
      //   {
      //      if (ied is EditableLineBreak elb)
      //      {
      //         int elbIdx = Inlines.IndexOf(elb);
      //         Debug.WriteLine("ndex = " + elbIdx);
      //         if (elbIdx == 0 || (Inlines[elbIdx - 1] is EditableRun erun && erun.Text != ""))
      //            Inlines.Insert(elbIdx, new EditableRun(""));
      //      }
      //   }
      //}

   }

   public string ParToolTip => $"Background: {Background}\nLineSpacing: {LineSpacing}\nLineHeight: {LineHeight}";

   public Thickness BorderThickness { get;  set { field = value; NotifyPropertyChanged(nameof(BorderThickness)); } } = new(0);
   public SolidColorBrush BorderBrush { get; set { field = value; NotifyPropertyChanged(nameof(BorderBrush)); } } = new(Colors.Transparent);
   public SolidColorBrush Background { get; set { field = value; NotifyPropertyChanged(nameof(Background)); } } = new(Colors.Transparent);
   //private FontFamily _FontFamily = new ("ＭＳ 明朝, Times New Roman");
   public FontFamily FontFamily { get; set { field = value; NotifyPropertyChanged(nameof(FontFamily)); } } = new("Meiryo");
   public double FontSize { get; set { field = value; NotifyPropertyChanged(nameof(FontSize)); } } = 16D;
   public double LineHeight { get; set { field = value; NotifyPropertyChanged(nameof(LineHeight)); } } = 18.666D;  // fontsize normally
   public double LineSpacing { get; set { field = value; NotifyPropertyChanged(nameof(LineSpacing)); } } = 0D;
   public FontWeight FontWeight { get; set { field = value; NotifyPropertyChanged(nameof(FontWeight)); } } = FontWeight.Normal;
   public FontStyle FontStyle{ get; set { field = value; NotifyPropertyChanged(nameof(FontStyle)); } } = FontStyle.Normal;
   public TextAlignment TextAlignment { get; set { field = value; NotifyPropertyChanged(nameof(TextAlignment)); } } = TextAlignment.Left;
     
   public SolidColorBrush SelectionBrush { get; set { field = value; NotifyPropertyChanged(nameof(SelectionBrush)); } } = LightBlueBrush;
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

   internal bool RequestInlinesUpdate { get; set { field = value; NotifyPropertyChanged(nameof(RequestInlinesUpdate)); } } = false;
   internal bool RequestInvalidateVisual { get; set { field = value; NotifyPropertyChanged(nameof(RequestInvalidateVisual)); } } = false;
   internal bool RequestTextLayoutInfoStart { get; set { field = value; NotifyPropertyChanged(nameof(RequestTextLayoutInfoStart)); } } = false;
   internal bool RequestTextLayoutInfoEnd { get; set { field = value; NotifyPropertyChanged(nameof(RequestTextLayoutInfoEnd)); } } = false;
   public bool RequestTextBoxFocus { get; set { field = value; NotifyPropertyChanged(nameof(RequestTextBoxFocus)); } } = false;

   //private int _RequestRectOfCharacterIndex;
   //public int RequestRectOfCharacterIndex { get => _RequestRectOfCharacterIndex; set { _RequestRectOfCharacterIndex = value; NotifyPropertyChanged(nameof(RequestRectOfCharacterIndex)); } }

   internal void CallRequestTextBoxFocus() { RequestTextBoxFocus = true; RequestTextBoxFocus = false; }
   internal void CallRequestInvalidateVisual() { RequestInvalidateVisual = true; RequestInvalidateVisual = false; }
   internal void CallRequestInlinesUpdate() { RequestInlinesUpdate = true; RequestInlinesUpdate = false; }
   internal void CallRequestTextLayoutInfoStart() { RequestTextLayoutInfoStart = true; RequestTextLayoutInfoStart = false; }
   internal void CallRequestTextLayoutInfoEnd() { RequestTextLayoutInfoEnd = true; RequestTextLayoutInfoEnd = false; }

   internal void UpdateEditableRunPositions()
   {
      int sum = 0;
      for (int edx = 0; edx < Inlines.Count; edx++)
      {
         Inlines[edx].TextPositionOfInlineInParagraph = sum;
         sum += Inlines[edx].InlineLength;
      }
   }

   internal void UpdateUIContainersSelected(int start, int end)
   {
      if (this.Inlines != null)
      {
         foreach (EditableInlineUIContainer iuc in Inlines.OfType<EditableInlineUIContainer>())
            iuc.IsSelected = (iuc.TextPositionOfInlineInParagraph >= start && iuc.TextPositionOfInlineInParagraph < end);
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
      Paragraph newPar = new() 
      { 
         Id = this.Id,
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
      };
                 
      newPar.Inlines.CollectionChanged += Inlines_CollectionChanged;
      newPar.Inlines.AddRange(this.Inlines.Select(il => il.CloneWithId()));

      return newPar;
   }

 
}
