using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using DynamicData;
using System.Collections.ObjectModel;

namespace AvRichTextBox;

[DebuggerDisplay("Text: {Text}, Inlines: {Inlines}")]
public class Paragraph : Block
{
    internal string ParToolTip => $"Background: {Background}\nLineHeight: {LineHeight}";

    public ObservableCollection<IEditable> Inlines { get; set; } = [];

    //public Paragraph() { }

    public Paragraph(FlowDocument owningFlowDoc)
    {
        MyFlowDoc = owningFlowDoc;

        Inlines.CollectionChanged += Inlines_CollectionChanged;
        Id = ++FlowDocument.ParagraphIdCounter;

    }

    private void Inlines_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        for (int ilineno = 0; ilineno < Inlines.Count; ilineno++)
        {
            IEditable ied = Inlines[ilineno];
            ied.MyParagraphId = this.Id;
            ied.MyFlowDoc = this.MyFlowDoc;
            ied.IsTableCellInline = this.IsTableCellBlock;
            ied.IsFirstInlineOfParagraph = ilineno == 0;
            ied.IsLastInlineOfParagraph = ilineno == Inlines.Count - 1;
            ied.PreviousInline = ilineno == 0 ? null! : Inlines[ilineno - 1];
            ied.NextInline = ilineno == Inlines.Count - 1 ? null! : Inlines[ilineno + 1];
        }

    }

    public Thickness BorderThickness { get; set { field = value; NotifyPropertyChanged(nameof(BorderThickness)); } } = new(0);
    public ISolidColorBrush BorderBrush { get; set { field = value; NotifyPropertyChanged(nameof(BorderBrush)); } } = new SolidColorBrush(Colors.Transparent);
    public ISolidColorBrush Background { get; set { field = value; NotifyPropertyChanged(nameof(Background)); } } = new SolidColorBrush(Colors.Transparent);
    public FontFamily FontFamily { get; set { field = value; NotifyPropertyChanged(nameof(FontFamily)); } } = new("Meiryo");
    public double FontSize { get; set { field = value; NotifyPropertyChanged(nameof(FontSize)); } } = 16D;
    public double LineHeight { get; set { field = value; NotifyPropertyChanged(nameof(LineHeight)); } } = 0;  // based on fontsize 

    internal TextLayout TextLayout = null!;
    internal double DocICRelativeTop = 0;
    internal double DocICRelativeLeft = 0;

    //public double LineSpacing 
    //{ 
    //   get; 
    //   set
    //   { 
    //      field = value; NotifyPropertyChanged(nameof(LineSpacing));
    //      CallRequestInlinesUpdate();
    //      CallRequestTextLayoutInfoStart();
    //      CallRequestInvalidateVisual();
    //   } 
    //} = 0D;

    public FontWeight FontWeight { get; set { field = value; NotifyPropertyChanged(nameof(FontWeight)); } } = FontWeight.Normal;
    public FontStyle FontStyle { get; set { field = value; NotifyPropertyChanged(nameof(FontStyle)); } } = FontStyle.Normal;
    public TextAlignment TextAlignment { get; set { field = value; NotifyPropertyChanged(nameof(TextAlignment)); } } = TextAlignment.Left;

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
    internal bool IsEmptyInlineOrUICPar => Inlines.Count == 1 && (Inlines[0].IsUIContainer || Inlines[0].IsEmpty);
    internal bool IsEmptyInlinePar => Inlines.Count == 1 && Inlines[0].IsEmpty;

    internal bool RequestInlinesUpdate { get; set { field = value; NotifyPropertyChanged(nameof(RequestInlinesUpdate)); } } = false;
    internal bool RequestInvalidateVisual { get; set { field = value; NotifyPropertyChanged(nameof(RequestInvalidateVisual)); } } = false;
    internal bool RequestTextLayoutInfoStart { get; set { field = value; NotifyPropertyChanged(nameof(RequestTextLayoutInfoStart)); } } = false;
    internal bool RequestTextLayoutInfoEnd { get; set { field = value; NotifyPropertyChanged(nameof(RequestTextLayoutInfoEnd)); } } = false;
    public bool RequestTextBoxFocus { get; set { field = value; NotifyPropertyChanged(nameof(RequestTextBoxFocus)); } } = false;

    internal void CallRequestTextBoxFocus() { RequestTextBoxFocus = true; RequestTextBoxFocus = false; }
    internal void CallRequestInvalidateVisual() { RequestInvalidateVisual = true; RequestInvalidateVisual = false; }
    internal void CallRequestInlinesUpdate() { RequestInlinesUpdate = true; RequestInlinesUpdate = false; }
    internal void CallRequestTextLayoutInfoStart() { RequestTextLayoutInfoStart = true; RequestTextLayoutInfoStart = false; }
    internal void CallRequestTextLayoutInfoEnd() { RequestTextLayoutInfoEnd = true; RequestTextLayoutInfoEnd = false; }

    internal void EnsureProperEnd()
    {
        if (SelectionEndInBlock < SelectionStartInBlock)
            SelectionStartInBlock = SelectionEndInBlock;

    }

    internal void UpdateEditableRunPositions()
    {
        int sum = 0;
        for (int edx = 0; edx < Inlines.Count; edx++)
        {
            Inlines[edx].TextPositionOfInlineInParagraph = sum;
            sum += Inlines[edx].InlineLength;
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
        return new Paragraph(MyFlowDoc)
        {
            TextAlignment = this.TextAlignment,
            //LineSpacing = this.LineSpacing,
            BorderBrush = this.BorderBrush,
            BorderThickness = this.BorderThickness,
            LineHeight = this.LineHeight,
            Margin = this.Margin,
            Background = this.Background,
            FontFamily = this.FontFamily,
            FontSize = this.FontSize,
            FontStyle = this.FontStyle,
            FontWeight = this.FontWeight,
            IsTableCellBlock = this.IsTableCellBlock
        };
    }

    internal override Paragraph FullClone()
    {
        Paragraph newPar = new(this.MyFlowDoc)
        {
            Id = this.Id,
            StartInDoc = this.StartInDoc,
            TextAlignment = this.TextAlignment,
            //LineSpacing = this.LineSpacing,
            BorderBrush = this.BorderBrush,
            BorderThickness = this.BorderThickness,
            LineHeight = this.LineHeight,
            Margin = this.Margin,
            Background = this.Background,
            FontFamily = this.FontFamily,
            FontSize = this.FontSize,
            FontStyle = this.FontStyle,
            FontWeight = this.FontWeight,
            IsTableCellBlock = this.IsTableCellBlock,
            OwningTable = this.OwningTable,
            OwningCell = this.OwningCell
        };


        newPar.Inlines.CollectionChanged += Inlines_CollectionChanged;
        newPar.Inlines.AddRange(this.Inlines.Select(il => il.CloneWithId()));

        return newPar;
    }

    internal void EnsureEmptyRuns()
    {

        if (this.Inlines.Count == 0)
            this.Inlines.Add(new EditableRun(""));

        // linebreaks must have empty runs between them
        for (int i = this.Inlines.Count - 1; i >= 0; i--)
        {
            if (!this.Inlines[i].IsLineBreak)
                continue;

            bool addBefore = i == 0 || this.Inlines[i - 1].IsLineBreak;
            bool addAfter = i == this.Inlines.Count - 1;

            if (addAfter)
                this.Inlines.Insert(i + 1, new EditableRun(""));
            if (addBefore)
                this.Inlines.Insert(i, new EditableRun(""));
        }
    }

}
