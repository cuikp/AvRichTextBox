using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvRichTextBox;

public partial class EditableParagraph : SelectableTextBlock
{
   public bool IsEditable { get; set; } = true;

   private readonly SolidColorBrush caretBrush = new (Colors.Cyan, 0.55);

   public int RectCharacterIndex = 0;
   
   public EditableParagraph()
   {
      this.SelectionBrush = caretBrush;

      this.Loaded += EditableParagraph_Loaded;

      this.PropertyChanged += EditableParagraph_PropertyChanged;

      //this.KeyDown += EditableParagraph_KeyDown;

   }

   private void EditableParagraph_Loaded(object? sender, RoutedEventArgs e)
   {
      UpdateInlines();
            
   }

   public static readonly StyledProperty<bool> TextLayoutInfoStartRequestedProperty = AvaloniaProperty.Register<EditableParagraph, bool>(nameof(TextLayoutInfoStartRequested));
   public bool TextLayoutInfoStartRequested { get => GetValue(TextLayoutInfoStartRequestedProperty); set { SetValue(TextLayoutInfoStartRequestedProperty, value); } }

   public static readonly StyledProperty<bool> TextLayoutInfoEndRequestedProperty = AvaloniaProperty.Register<EditableParagraph, bool>(nameof(TextLayoutInfoEndRequested));
   public bool TextLayoutInfoEndRequested { get => GetValue(TextLayoutInfoEndRequestedProperty); set { SetValue(TextLayoutInfoEndRequestedProperty, value); } }
   
   public void UpdateVMFromEPStart()
   {
      SelectionStartRect_Changed?.Invoke(this);
      this.SetValue(TextLayoutInfoStartRequestedProperty, false);

   }

   public void UpdateVMFromEPEnd()
   {
      SelectionEndRect_Changed?.Invoke(this);
      this.SetValue(TextLayoutInfoEndRequestedProperty, false);
   }

   //public void NotifyCharIndexRect(EditableParagraph ep, Rect selEndRect)
   //{

   //   if (CharIndexRect_Notified != null)
   //      CharIndexRect_Notified(ep, selEndRect);
   //}


   private void EditableParagraph_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
   {
      
      //Debug.WriteLine("e.propertyName = " + e.Property.Name);

      if (ThisPar != null)  //because this may be called right after paragraph has been deleted
      {

         switch (e.Property.Name)
         {
            case "Bounds":
               //Necessary for initial setting for each created paragraph
               ThisPar?.FirstIndexLastLine = this.TextLayout.TextLines[^1].FirstTextSourceIndex;
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

   protected override void OnPointerPressed(PointerPressedEventArgs e)
   {
      //Prevent default behavior


   }

   protected override void OnKeyDown(KeyEventArgs e)
   {
      //Keep to override
      //Debug.WriteLine("thisEP focused = " + this.IsFocused);
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
      //e.Handled = true;

      TextHitTestResult result = this.TextLayout.HitTestPoint(e.GetPosition(this));

      MouseMove?.Invoke(this, result.TextPosition);

   }

   protected override void OnPointerReleased(PointerReleasedEventArgs e)
   {
      // Prevent default behavior
      //e.Handled = true;
   }

   internal void UpdateInlines()
   {

      if (((Paragraph)this.DataContext!).Inlines != null)
         this.Inlines = GetFormattedInlines();

      //foreach (Inline thisIL in this.Inlines!)
      //   Debug.WriteLine("1:\n" + ((Run)thisIL).GetText + " ::: " + thisIL.FontWeight);

      //this.Height = this.Inlines[0].get


      this.InvalidateMeasure();
      this.InvalidateVisual();

   }

   public int SelectionLength => SelectionEnd - SelectionStart;

   Paragraph? ThisPar => this.DataContext as Paragraph;

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

