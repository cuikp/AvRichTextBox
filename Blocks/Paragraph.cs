using Avalonia.Media;
using System.Collections.ObjectModel;

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

   //public string Text => string.Join("", Inlines.ToList().ConvertAll(ied => ied.InlineText));


   private SolidColorBrush _Background = new SolidColorBrush(Colors.Transparent);
   public SolidColorBrush Background { get => _Background; set { _Background = value; NotifyPropertyChanged(nameof(Background)); } }

   private FontFamily _FontFamily = new FontFamily("Times New Roman, ＭＳ 明朝");
   public FontFamily FontFamily { get => _FontFamily; set { _FontFamily = value; NotifyPropertyChanged(nameof(FontFamily)); } }

   private double _FontSize = 16D;
   public double FontSize { get => _FontSize; set { _FontSize = value; NotifyPropertyChanged(nameof(FontSize)); } }

   private FontWeight _FontWeight = FontWeight.Normal;
   public FontWeight FontWeight { get => _FontWeight; set { _FontWeight = value; NotifyPropertyChanged(nameof(FontWeight)); } }

   private TextAlignment _TextAlignment = TextAlignment.Left;
   public TextAlignment TextAlignment { get => _TextAlignment; set { _TextAlignment = value; NotifyPropertyChanged(nameof(TextAlignment)); } }

   //private double _LineHeight = 36; // 18.666D;
   //public double LineHeight { get => _LineHeight; set { _LineHeight = value; NotifyPropertyChanged(nameof(LineHeight)); } }

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


   internal void UpdateTextLayoutInfoStart() { RequestTextLayoutInfoStart = true; }
   internal void UpdateTextLayoutInfoEnd() { RequestTextLayoutInfoEnd = true; }

   internal void UpdateEditableRunPositions()
   {
      int sum = 0;
      for (int edx = 0; edx < Inlines.Count; edx++)
      {
         Inlines[edx].TextPositionOfInlineInParagraph = sum;
         sum += Inlines[edx].InlineLength;
      }
   }

   internal Paragraph Clone()
   {
      return new Paragraph() { TextAlignment = this.TextAlignment }; //Add other properties later
   }



}
