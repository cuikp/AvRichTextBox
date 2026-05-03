using Avalonia.Media;

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

      //force hyperlink visual formatting
      this.Foreground = displayBrush;
      this.TextDecorations = displayDecoration;

      
   }

   //public EditableHyperlink() { }

   protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
   {
      base.OnPropertyChanged(change);

      //Prevent user change to hyperlink formatting
      if (change.Property == ForegroundProperty && !Equals(change.NewValue, displayBrush))
      {
         SetCurrentValue(ForegroundProperty, displayBrush);
      }
      else if (change.Property == TextDecorationsProperty && !Equals(change.NewValue, displayDecoration))
      {
         SetCurrentValue(TextDecorationsProperty, displayDecoration);
      }
   }

   public string NavigateUri { get; set; } = "";

   public override IEditable Clone() =>

     new EditableHyperlink(this.Text!, this.NavigateUri)
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
        BaselineAlignment = this.BaselineAlignment,
        Foreground = this.Foreground,
     };


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

