using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace AvRichTextBox;

public class EditableHyperlink : EditableRun
{

    public EditableHyperlink(string displayText, string navigateUri) : this()
    {
        Id = ++FlowDocument.InlineIdCounter;
        LinkDisplayText = displayText;
        NavigateUri = navigateUri;

        this.LinkOpening += EditableHyperlink_LinkOpening;

    }

    private void EditableHyperlink_LinkOpening(object? sender, LinkOpeningEventArgs e)
    {
        //e.Handled = true;

    }

    public sealed class LinkOpeningEventArgs(string navigateUri) : EventArgs
    {
        public string NavigateUri { get; } = navigateUri;
        public bool Handled { get; set; }
    }

    public event EventHandler<LinkOpeningEventArgs>? LinkOpening;

    internal void OpenLink()
    {
        var args = new LinkOpeningEventArgs(NavigateUri);

        LinkOpening?.Invoke(this, args);

        if (args.Handled)
            return;

        var psi = new ProcessStartInfo { FileName = NavigateUri, UseShellExecute = true };
        Process.Start(psi);
    }

    internal EditableHyperlink()
    {
        Foreground = Brushes.Blue;
        TextDecorations = [new()
        {
            Location = TextDecorationLocation.Underline,
            StrokeThicknessUnit = TextDecorationUnit.Pixel,
            StrokeThickness = 1
        }];
    }

    [Obsolete("get/set LinkDisplayText instead.", true)]
    public new string? Text
    {
        get => base.Text;
        set { base.Text = value ?? null!; }
    }

    public string LinkDisplayText
    {
        get => base.Text!;
        set
        {
            string oldText = base.Text!;
            base.Text = value;
            if (MyFlowDoc != null && !DisableUndoStack && IsAttachedToDocument)
            {
                MyFlowDoc.Undos.Add(new HyperlinkDisplayTextChangedUndo(this.MyParagraphId, this.Id, oldText, value, MyFlowDoc));
            }
        }
    }

    public string NavigateUri 
    { 
        get;
        set
        {
            string oldUri = base.Text!;
            field = value;
            if (MyFlowDoc != null && !DisableUndoStack && IsAttachedToDocument)
            {
                MyFlowDoc.Undos.Add(new HyperlinkNavigateUriChangedUndo(this.MyParagraphId, this.Id, oldUri, value, MyFlowDoc));
            }
        }
    } = "";


    public override EditableHyperlink Clone()
    {
        bool keepDisableUndoStack = DisableUndoStack;
        DisableUndoStack =  true;

        EditableHyperlink newEHL = new(this.LinkDisplayText!, this.NavigateUri)
        {
            MarkdownInfo = this.MarkdownInfo,
            FontStyle = this.FontStyle,
            FontWeight = this.FontWeight,
            TextDecorations = this.TextDecorations,
            FontSize = this.FontSize,
            FontFamily = this.FontFamily,
            Background = this.Background,
            MyParagraphId = this.MyParagraphId,
            MyFlowDoc = this.MyFlowDoc,
            TextPositionOfInlineInParagraph = this.TextPositionOfInlineInParagraph,  //necessary because clone is produced when calculating range inline positions
            IsLastInlineOfParagraph = this.IsLastInlineOfParagraph,
            IsTableCellInline = this.IsTableCellInline,
            BaselineAlignment = this.BaselineAlignment,
            Foreground = this.Foreground,
        };

        DisableUndoStack = keepDisableUndoStack;

        return newEHL;
    }

    public override EditableHyperlink CloneWithId()
    {
        EditableHyperlink IdClone = this.Clone();
        IdClone.Id = this.Id;
        return IdClone;
    }


#if DEBUG
    // FOR DEBUGGER PANEL
    public override string DisplayInlineText => "{>HYPERLINK<}" + $" \"{LinkDisplayText}\"";
#endif

}

