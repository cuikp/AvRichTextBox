using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Diagnostics;
using System.Linq;

namespace AvRichTextBox;

public partial class EditableParagraph : SelectableTextBlock
{
   public bool IsEditable { get; set; } = true;

   private readonly SolidColorBrush cursorBrush = new (Colors.Cyan, 0.55);

   public int RectCharacterIndex = 0;

   public EditableParagraph()
   {
      this.SelectionBrush = cursorBrush;

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
      //if (!e.Property.Name.Contains("IsPointerOver"))
      //   Debug.WriteLine("e.propertyName = " + e.Property.Name);


      if (thisPar != null)  //because this may be called right after paragraph has been deleted
      {

         switch (e.Property.Name)
         {
            case "Bounds":
               //Necessary for initial setting for each created paragraph
               thisPar.FirstIndexLastLine = this.TextLayout.TextLines[^1].FirstTextSourceIndex;
               break;

            //case "Inlines":
            //   //Debug.WriteLine("inlines changed: " + e.;
            //   ////raise event as TextChanged:
            //   if (EditableParagraph_TextChanged != null)
            //      EditableParagraph_TextChanged(this);

            //   break;

            //case "SelectionStart":
            //   UpdateVMFromEPStart();
            //   break;

            //case "SelectionEnd":
            //   UpdateVMFromEPEnd();
            //   break;

            case "TextLayoutInfoStartRequested":
               UpdateVMFromEPStart();
               break;

            case "TextLayoutInfoEndRequested":
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
      //e.Handled = true;

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

   }

   public int SelectionLength => SelectionEnd - SelectionStart;

   Paragraph? thisPar => this.DataContext as Paragraph;

  
   public new string Text => string.Join("", ((Paragraph)this.DataContext!).Inlines.ToList().ConvertAll(edinline => edinline.InlineText));


}

