using Avalonia.Controls.Documents;
using Avalonia.Media;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AvRichTextBox;

public class EditableHyperlink : EditableRun
{

    private static readonly ISolidColorBrush displayBrush = Brushes.Blue;
    private static readonly TextDecorationCollection displayDecoration =
       [ new() {
         Location = TextDecorationLocation.Underline,
         Stroke = displayBrush,
         StrokeThicknessUnit = TextDecorationUnit.Pixel,
         StrokeThickness = 1
      }];

    public EditableHyperlink(string displayText, string navigateUri)
    {
        Id = ++FlowDocument.InlineIdCounter;
        Text = displayText;
        NavigateUri = navigateUri;

        ForceFormatting();

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

    private void ForceFormatting()
    {
        this.Foreground = displayBrush;
        this.TextDecorations = displayDecoration;
    }

    internal EditableHyperlink() { ForceFormatting(); }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);


        switch (e.Property)
        {
            
            case AvaloniaProperty tp when tp == InlineUIContainer.ForegroundProperty:
                //Prevent user change to hyperlink formatting
                if (!Equals(e.NewValue, displayBrush))
                {
                    SetCurrentValue(ForegroundProperty, displayBrush);
                }
                break;

            case AvaloniaProperty tp when tp == InlineUIContainer.TextDecorationsProperty:
                //Prevent user change to hyperlink formatting
                if (!Equals(e.NewValue, displayDecoration))
                {
                    SetCurrentValue(TextDecorationsProperty, displayDecoration);
                }
                break;

        }

    }

    public string NavigateUri { get; set; } = "";

    public override IEditable Clone()
    {
        MyFlowDoc.disableUndoStack = true;

        EditableHyperlink newEHL = new(this.Text!, this.NavigateUri)
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
            IsLastInlineOfParagraph = this.IsLastInlineOfParagraph,
            IsTableCellInline = this.IsTableCellInline,
            BaselineAlignment = this.BaselineAlignment,
            Foreground = this.Foreground,
        };

        MyFlowDoc.disableUndoStack = false;

        return newEHL;
    }

    public override IEditable CloneWithId()
    {
        IEditable IdClone = this.Clone();
        IdClone.Id = this.Id;
        return IdClone;
    }


#if DEBUG
    // FOR DEBUGGER PANEL
    public override string DisplayInlineText => "{>HYPERLINK<}" + $" \"{Text}\"";
#endif

}

