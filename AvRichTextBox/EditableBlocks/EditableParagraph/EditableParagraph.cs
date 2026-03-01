using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvRichTextBox;

internal partial class EditableParagraph : SelectableTextBlock
{
   public static readonly StyledProperty<bool> TextLayoutInfoStartRequestedProperty = AvaloniaProperty.Register<EditableParagraph, bool>(nameof(TextLayoutInfoStartRequested));
   public bool TextLayoutInfoStartRequested { get => GetValue(TextLayoutInfoStartRequestedProperty); set { SetValue(TextLayoutInfoStartRequestedProperty, value); } }

   public static readonly StyledProperty<bool> TextLayoutInfoEndRequestedProperty = AvaloniaProperty.Register<EditableParagraph, bool>(nameof(TextLayoutInfoEndRequested));
   public bool TextLayoutInfoEndRequested { get => GetValue(TextLayoutInfoEndRequestedProperty); set { SetValue(TextLayoutInfoEndRequestedProperty, value); } }

   public bool IsEditable { get; set; } = true;

   public int SelectionLength => SelectionEnd - SelectionStart;

   Paragraph? ThisPar => this.DataContext as Paragraph;

   public int RectCharacterIndex = 0;
   
   public EditableParagraph()
   {
      this.Loaded += EditableParagraph_Loaded;
      this.PropertyChanged += EditableParagraph_PropertyChanged;

      //this.KeyDown += EditableParagraph_KeyDown;
   }
   
   private void EditableParagraph_Loaded(object? sender, RoutedEventArgs e)
   {
      UpdateInlines();
      
      if (this.DataContext is not Paragraph thisPar) return;

      if (Inlines?.Count == 0)
         thisPar.Inlines.Add(new EditableRun(""));

      List<int> lineBreakIndexes = Inlines.OfType<EditableLineBreak>().ToList().ConvertAll(elb => Inlines.IndexOf(elb));
      for (int idx = lineBreakIndexes.Count - 1; idx >= 0; idx--)
      {
         int elbIdx = lineBreakIndexes[idx];
         if (elbIdx == 0 || (Inlines[elbIdx - 1] is EditableRun erun && erun.Text != ""))
            thisPar.Inlines.Insert(elbIdx + 1, new EditableRun(""));
      }
      thisPar.UpdateEditableRunPositions();
      UpdateInlines();

   }
     
   private void EditableParagraph_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
   {      
      //Debug.WriteLine("e.propertyName = " + e.Property.Name);

      if (ThisPar != null)  //because this may be called right after paragraph has been deleted
      {
         switch (e.Property.Name)
         {
            case "Bounds":
               //Necessary for initial setting for each created paragraph
               ThisPar.FirstIndexLastLine = this.TextLayout.TextLines[^1].FirstTextSourceIndex;
               break;

            //case "Inlines":
            //   UpdateVMFromEPStart();
            //   UpdateVMFromEPEnd();
            //   break;
                    
            case "LineSpacing":
               this.UpdateLayout();

               if (TextLayout != null && TextLayout.TextLines.Count > 0)
               {
                  double maxLineHeight = Math.Max(TextLayout.TextLines[0].Height, TextLayout.TextLines[^1].Height);
                  ThisPar.LineHeight = maxLineHeight;
               }
               //Debug.WriteLine("\nline spacing changed: LINESpacing = " + this.LineSpacing);

               break;

            case "SelectionStart":
               UpdateVMFromEPStart();
               break;

            case "SelectionEnd":
               UpdateVMFromEPEnd();
               break;

            case "SelectedText":
               ThisPar.UpdateUIContainersSelected(SelectionStart, SelectionEnd);  // changes image opacity to visualize its selection
               break;

            case "TextLayoutInfoStartRequested":
               this.SetValue(TextLayoutInfoStartRequestedProperty, false);
               if (ThisPar == null)
                  Dispatcher.UIThread.Post(() => UpdateVMFromEPStart(), DispatcherPriority.Background);
                else
                  UpdateVMFromEPStart();
               break;

            case "TextLayoutInfoEndRequested":
               this.SetValue(TextLayoutInfoEndRequestedProperty, false);
               if (ThisPar == null)
                  Dispatcher.UIThread.Post(() => UpdateVMFromEPEnd(), DispatcherPriority.Background);
               else   
                  UpdateVMFromEPEnd();
               break;
         }

      }

   }

   protected override void OnPointerPressed(PointerPressedEventArgs e) { /*Prevent default behavior*/  }
   protected override void OnPointerReleased(PointerReleasedEventArgs e) { /* Prevent default behavior*/ }

   protected override void OnKeyDown(KeyEventArgs e)
   {
      //Keep to override default behavior
      
      //if (!this.IsFocused)
      //   e.Handled = true;
      //UpdateVMFromEPEnd();
   }

   protected override void OnLostFocus(RoutedEventArgs e)
   {
      base.OnLostFocus(e);
      this.Focusable = false;
   }

   protected override void OnGotFocus(GotFocusEventArgs e)
   {
      base.OnGotFocus(e);
      this.Focusable = true;
   }
   
   protected override void OnPointerMoved(PointerEventArgs e)
   {
      TextHitTestResult result = this.TextLayout.HitTestPoint(e.GetPosition(this));
      MouseMove?.Invoke(this, result.TextPosition);
   }

 
   public new string Text => this.DataContext is Paragraph p ? string.Join("", p.Inlines.ToList().ConvertAll(edinline => edinline.InlineText)) : "";

   public int TextLength
   {
      get
      {
         int len = 0;
         if (this.DataContext is Paragraph p)
         {
            foreach (var i in p.Inlines)
               len += i.InlineText?.Length ?? 0;
         }
         return len;
      }
   }

}

