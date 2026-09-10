using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using DynamicData;
using System.Collections.ObjectModel;

namespace AvRichTextBox;

[DebuggerDisplay("Text: {Text}, Inlines: {Inlines}")]
public class Paragraph : Block
{
    internal string ParToolTip => $"Background: {Background}\nLineHeight: {LineHeight}";

    internal ObservableCollection<IEditable> Inlines { get; } = [];
    public IEnumerable<IEditable> GetInlines => Inlines;

    public Paragraph(FlowDocument owningFlowDoc)
    {
        //this.PropertyChanged += Paragraph_PropertyChanged;
        MyFlowDoc = owningFlowDoc;

        Inlines.CollectionChanged += Inlines_CollectionChanged;
        Id = ++FlowDocument.BlockIdCounter;

    }

    private void Paragraph_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        //if (e.PropertyName == "FontWeight")
            //Debug.WriteLine("par: " + e.PropertyName); 
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

        this.CallRequestInlinesUpdate();
    }

    public TextAlignment TextAlignment 
    { 
        get; 
        set 
        { 
            TextAlignment oldTextAlign = field;

            field = value;

            if (!IsAttachedToDocument) return;

            if (!MyFlowDoc.disableUndoStack)
                MyFlowDoc.Undos.Add(new ParagraphTextAlignmentChangeUndo(this.Id, oldTextAlign, MyFlowDoc));

            NotifyPropertyChanged(nameof(TextAlignment)); 
        } 
    } = TextAlignment.Left;

    public double LineHeight 
    { 
        get; 
        set 
        { 
            double oldLineHeight = field;
            field = value;

            if (!IsAttachedToDocument) return;

            if (!MyFlowDoc.disableUndoStack)
                MyFlowDoc.Undos.Add(new ParagraphLineHeightChangeUndo(this.Id, oldLineHeight, MyFlowDoc));

            NotifyPropertyChanged(nameof(LineHeight));

            MyFlowDoc.InvokeSelectionChanged();

        } 
    } = 0;  // based on fontsize 

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
    internal bool RequestTextBoxFocus { get; set { field = value; NotifyPropertyChanged(nameof(RequestTextBoxFocus)); } } = false;
    internal bool RequestSizeChanged { get; set { field = value; NotifyPropertyChanged(nameof(RequestSizeChanged)); } } = false;

    internal void CallRequestTextBoxFocus() { RequestTextBoxFocus = true; RequestTextBoxFocus = false; }
    internal void CallRequestInvalidateVisual() { RequestInvalidateVisual = true; RequestInvalidateVisual = false; }
    internal void CallRequestInlinesUpdate() { RequestInlinesUpdate = true; RequestInlinesUpdate = false; }
    internal void CallRequestTextLayoutInfoStart() { RequestTextLayoutInfoStart = true; RequestTextLayoutInfoStart = false; }
    internal void CallRequestTextLayoutInfoEnd() { RequestTextLayoutInfoEnd = true; RequestTextLayoutInfoEnd = false; }
    internal void CallRequestSizeChanged() { RequestSizeChanged = true; RequestSizeChanged = false; }

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

    public void InsertInlinesAt(int index, IEnumerable<IEditable> inlinesToAdd)
    {
        if (!inlinesToAdd.Any()) return;

        if (inlinesToAdd.FirstOrDefault(il=> il == null) is IEditable nullIED)
            throw new Exception("The passed inlinesToAdd collection contains a null IEditable");

        for (int inlineno = inlinesToAdd.Count() - 1; inlineno >= 0; inlineno --)
            this.Inlines.Insert(index, inlinesToAdd.ElementAt(inlineno));

        bool addUndo = !MyFlowDoc.disableUndoStack && this.IsAttachedToDocument;
        
        if (addUndo)
            MyFlowDoc.Undos.Add(new InsertInlinesAtUndo(this.Id, [.. inlinesToAdd.Select(il=> il.Id)], MyFlowDoc));

    }

    public void InsertInlineAt(int index, IEditable inlineToAdd)
    {
        if (inlineToAdd == null) return;

        if (index < 0 || index > this.Inlines.Count)
            throw new Exception("IEditable index is out of bounds of paragraph Inlines");

        this.Inlines.Insert(index, inlineToAdd);

        bool addUndo = !MyFlowDoc.disableUndoStack && this.IsAttachedToDocument;

        if (addUndo)
            MyFlowDoc.Undos.Add(new InsertInlineAtUndo(this.Id, inlineToAdd.Id, MyFlowDoc));

    }

    public void AddInline(IEditable inlineToAdd)
    {
        if (inlineToAdd == null) return;
        InsertInlineAt(this.Inlines.Count, inlineToAdd);
    }

    public void RemoveInlineAt(int index)
    {
        if (index < 0 || index >= this.Inlines.Count)
            throw new Exception("IEditable index is out of bounds of paragraph Inlines");

        if (this.Inlines[index] is IEditable inlineToRemove)
        {
            bool addUndo = !MyFlowDoc.disableUndoStack && this.IsAttachedToDocument;

            if (addUndo)
                MyFlowDoc.Undos.Add(new RemoveInlineUndo(this.Id, index, inlineToRemove.CloneWithId(), MyFlowDoc));

            RemoveInline(inlineToRemove);

            if (Inlines.Count == 0)
                AddDefaultRun();
        }
    }

    internal void AddDefaultRun()
    {
        MyFlowDoc.disableUndoStack = true;
        this.Inlines.Add(new EditableRun(""));
        MyFlowDoc.disableUndoStack = false;
    }

    public void RemoveInline(IEditable inlineToRemove)
    {
        if (inlineToRemove == null) return;
        int removeIndex = this.Inlines.IndexOf(inlineToRemove);
        RemoveInlineAt(removeIndex);
    }

    internal override Paragraph PropertyClone()
    {
        MyFlowDoc.disableUndoStack = true;

        Paragraph newPar = new(MyFlowDoc)
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
            IsTableCellBlock = this.IsTableCellBlock,
            OwningTable = this.OwningTable,
            OwningCell = this.OwningCell,
            StartInDoc = this.StartInDoc

        };

        MyFlowDoc.disableUndoStack = false;

        return newPar;
    }

    internal override Paragraph FullClone(bool keepId)
    {
        MyFlowDoc.disableUndoStack = true;

        Paragraph newPar = new(MyFlowDoc)
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
            IsTableCellBlock = this.IsTableCellBlock,
            OwningTable = this.OwningTable,
            OwningCell = this.OwningCell,
            StartInDoc = this.StartInDoc

        };

        if (keepId)
            newPar.Id = this.Id;

        newPar.Inlines.AddRange(this.Inlines.Select(il => il.CloneWithId()));

        MyFlowDoc.disableUndoStack = false;

        return newPar;
    }

    internal void CopyPropertiesFromParagraph(Paragraph sourceP)
    {
        MyFlowDoc.disableUndoStack = true;

        this.TextAlignment = sourceP.TextAlignment;
        //this.LineSpacing = sourceP.LineSpacing;
        this.BorderBrush = sourceP.BorderBrush;
        this.BorderThickness = sourceP.BorderThickness;
        this.LineHeight = sourceP.LineHeight;
        this.Margin = sourceP.Margin;
        this.Background = sourceP.Background;
        this.FontFamily = sourceP.FontFamily;
        this.FontSize = sourceP.FontSize;
        this.FontStyle = sourceP.FontStyle;
        this.FontWeight = sourceP.FontWeight;
        this.IsTableCellBlock = sourceP.IsTableCellBlock;
        //this.OwningTable = sourceP.OwningTable;
        //this.OwningCell = sourceP.OwningCell;

        MyFlowDoc.disableUndoStack = false;
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
