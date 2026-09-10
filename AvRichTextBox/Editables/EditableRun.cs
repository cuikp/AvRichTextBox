using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace AvRichTextBox;

[DebuggerDisplay("Text: {Text}")]
public class EditableRun : Run, IEditable
{

    public EditableRun() { InitializeRun(); }

    public EditableRun(string text) : this() { this.Text = text; }

    private void InitializeRun()
    {
        Id = ++FlowDocument.InlineIdCounter;
        BaselineAlignment = BaselineAlignment.Baseline;
        //FontFamily = "Meiryo";
        FontSize = 16;
        
        PropertyChanged += EditableRun_PropertyChanged;
    }

    private void EditableRun_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        //Debug.WriteLine($"ieditablerun {e.Property.Name} set");

        switch (e.Property)
        {
            case AvaloniaProperty tp when tp == Run.FontWeightProperty:
                
                break;

            case AvaloniaProperty tp when tp == Run.FontFamilyProperty:
                
                break;

            case AvaloniaProperty tp when tp == Run.FontSizeProperty:
                
                break;

            case AvaloniaProperty tp when tp == Run.FontStretchProperty:
                
                break;

            case AvaloniaProperty tp when tp == Run.FontStyleProperty:
                
                break;

            case AvaloniaProperty tp when tp == Run.ForegroundProperty:
                
                break;

            case AvaloniaProperty tp when tp == Run.BackgroundProperty:
                
                break;

            case AvaloniaProperty tp when tp == Run.BaselineAlignmentProperty:
                
                break;

            case AvaloniaProperty tp when tp == Run.TextDecorationsProperty:
                
                break;

            case AvaloniaProperty tp when tp == Run.TextProperty:

                
                //if (MyFlowDoc == null || MyFlowDoc.disableRunTextUndo) return;

                //if (e.Property == Run.TextProperty && e.Sender is EditableRun run)
                //{
                //   var oldText = (string?)e.OldValue ?? "";
                //   var newText = (string?)e.NewValue ?? "";
                //   //Debug.WriteLine("\noldText: " + oldText + "\n" + "newText: " + newText);

                //   var (start, deleteLen, insertText, deletedText) = GetDiff(oldText, newText);

                //   if (deleteLen == 0 && insertText.Length == 0)
                //      return;

                //   run.MyFlowDoc.Undos.Add(new TextChangedUndo(run.MyFlowDoc, run.MyParagraphId, run.Id, start, deletedText, insertText, run.MyFlowDoc.Selection.Start));
                //}
                break;
        }

    }

    internal int Id { get; set; }
    int IEditable.Id { get => Id; set => Id = value; }

    internal int MyParagraphId { get; set; }
    int IEditable.MyParagraphId { get => MyParagraphId; set => MyParagraphId = value; }

    internal FlowDocument MyFlowDoc { get; set; } = null!;
    FlowDocument IEditable.MyFlowDoc { get => MyFlowDoc; set => MyFlowDoc = value; }

    internal int TextPositionOfInlineInParagraph { get; set; }
    int IEditable.TextPositionOfInlineInParagraph { get => TextPositionOfInlineInParagraph; set => TextPositionOfInlineInParagraph = value; }
    public int GetTextPositionOfInlineInParagraph => TextPositionOfInlineInParagraph;

    public virtual int InlineLength => InlineText.Length;

    internal string InlineText { get => Text!; set => Text = value; }
    string IEditable.InlineText { get => InlineText; set => InlineText  = value; }
    
    public double InlineHeight => FontSize;

    public bool IsEmpty => InlineText.Length == 0;
    public string FontName => FontFamily?.Name == null ? "" : FontFamily?.Name!;

    bool IEditable.IsFirstInlineOfParagraph { get; set; }

    internal bool IsLastInlineOfParagraph { get; set; }
    bool IEditable.IsLastInlineOfParagraph { get => IsLastInlineOfParagraph; set => IsLastInlineOfParagraph = value; }

    internal bool IsTableCellInline { get; set; }
    bool IEditable.IsTableCellInline { get => IsTableCellInline; set => IsTableCellInline = value; }

    IEditable? PreviousInline { get; set; } = null!;
    IEditable? IEditable.PreviousInline { get => PreviousInline; set => PreviousInline = value; } 
    IEditable? NextInline { get; set; } = null!;
    IEditable? IEditable.NextInline { get => NextInline; set => NextInline = value; }

    public IEditable? GetPreviousInline => PreviousInline;
    public IEditable? GetNextInline => NextInline;


    public virtual IEditable Clone()
    {
        MyFlowDoc.disableUndoStack = true;

        EditableRun clonedRun = new (this.Text!)
        {
            FontStyle = this.FontStyle,
            FontWeight = this.FontWeight,
            TextDecorations = this.TextDecorations,
            FontSize = this.FontSize,
            FontFamily = this.FontFamily,
            Background = this.Background,
            MyParagraphId = this.MyParagraphId,
            MyFlowDoc = this.MyFlowDoc,
            TextPositionOfInlineInParagraph = this.TextPositionOfInlineInParagraph,  //necessary because clone is produced when calculating range inline positions
            BaselineAlignment = this.BaselineAlignment,
            Foreground = this.Foreground,
            IsLastInlineOfParagraph = this.IsLastInlineOfParagraph,
            IsTableCellInline = this.IsTableCellInline,
        };

        MyFlowDoc.disableUndoStack = false;

        return clonedRun;
    }

    public virtual IEditable CloneWithId()
    {
        IEditable IdClone = this.Clone();
        IdClone.Id = this.Id;
        return IdClone;

    }
       

#if DEBUG
    // FOR DEBUGGER PANEL
    internal InlineVisualizationProperties InlineVP { get; set; } = new();
    InlineVisualizationProperties IEditable.InlineVP { get => InlineVP; set => InlineVP = value; }
    public string InlineToolTip => $"Background: {Background}\nForeground: {Foreground}\nFontFamily: {FontFamily}\nFontSize: {FontSize}\nPrevInlineLineBreak?: {PreviousInline?.IsLineBreak}\nNextInlineLineBreak?: {NextInline?.IsLineBreak}";
    public virtual string DisplayInlineText => IsEmpty ? "{>EMPTY<}" : (InlineText.Length == 1 ? Text!.Replace(" ", "{>SPACE<}").Replace("\t", "{>TAB<}") : Text!.Replace("\t", "{>TAB<}"));

#endif


    internal static (int start, int deleteLen, string insertText, string deletedText) GetDiff(string oldText, string newText)
    {
        oldText ??= "";
        newText ??= "";

        int start = 0;
        int oldLen = oldText.Length;
        int newLen = newText.Length;

        while (start < oldLen && start < newLen && oldText[start] == newText[start])
            start++;

        int endOld = oldLen - 1;
        int endNew = newLen - 1;

        while (endOld >= start && endNew >= start && oldText[endOld] == newText[endNew])
        {
            endOld--;
            endNew--;
        }

        int deleteLen = Math.Max(0, endOld - start + 1);
        int insertLen = Math.Max(0, endNew - start + 1);

        string deletedText = deleteLen == 0 ? "" : oldText.Substring(start, deleteLen);
        string insertText = insertLen == 0 ? "" : newText.Substring(start, insertLen);

        //Debug.WriteLine("insertedText = " + insertText + ", deletedText = " + deletedText + ", deleteLen = " + deleteLen + ", start: " + start);

        return (start, deleteLen, insertText, deletedText);

    }


}




