using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvRichTextBox;

internal partial class EditableParagraph : TextBlock
{
   public static readonly StyledProperty<bool> TextLayoutInfoStartRequestedProperty = AvaloniaProperty.Register<EditableParagraph, bool>(nameof(TextLayoutInfoStartRequested));
   public bool TextLayoutInfoStartRequested { get => GetValue(TextLayoutInfoStartRequestedProperty); set { SetValue(TextLayoutInfoStartRequestedProperty, value); } }

   public static readonly StyledProperty<bool> TextLayoutInfoEndRequestedProperty = AvaloniaProperty.Register<EditableParagraph, bool>(nameof(TextLayoutInfoEndRequested));
   public bool TextLayoutInfoEndRequested { get => GetValue(TextLayoutInfoEndRequestedProperty); set { SetValue(TextLayoutInfoEndRequestedProperty, value); } }

   public bool IsEditable { get; set; } = true;

   Paragraph? ThisPar => this.DataContext as Paragraph;

   public int RectCharacterIndex = 0;
   
   public EditableParagraph()
   {
      this.Loaded += EditableParagraph_Loaded;
      this.PropertyChanged += EditableParagraph_PropertyChanged;
      this.MouseMove += EditableParagraph_MouseMove;

      FontFeatures = [ new FontFeature { Tag = "liga", Value = 0 } ]; // fix wrong hit testing with some font/letter combinations

      //this.KeyDown += EditableParagraph_KeyDown;

      LineSpacing = 0;

   }

   internal bool IsOverHyperlink = false;
   internal EditableHyperlink CurrentOverHyperlink = null!;
   private readonly Cursor HyperlinkCursor = new (StandardCursorType.Hand);

   private void EditableParagraph_MouseMove(EditableParagraph sender, int charIndex)
   {

      if (ThisPar?.Inlines.FirstOrDefault(il=> il.TextPositionOfInlineInParagraph <= charIndex && il.TextPositionOfInlineInParagraph + il.InlineLength >= charIndex) is EditableHyperlink currentHyperlink)
      {
         IsOverHyperlink = true;
         this.Cursor = HyperlinkCursor;
         CurrentOverHyperlink = currentHyperlink;
      }
      else
      {
         IsOverHyperlink = false;
         this.Cursor = Cursor.Default;
         CurrentOverHyperlink = null!;
      }


   }

   private ItemsControl myDocIC = null!;

   private ItemsControl GetDocIC => ThisPar == null ? null! : ThisPar.IsTableCellBlock switch
   {
      //Dig back to get itemscontrol from paragraph cell:
      true => (this.Parent is EditableCell ecell &&
               ecell.Parent is Grid gr &&
               gr.Parent is ContentPresenter grcp &&
               grcp.Parent is EditableTable etable &&
               etable.Parent is ContentControl cc &&
               cc.Parent is ContentPresenter cp &&
               cp.Parent is ItemsControl ic) ? ic : null!,

      //Dig back to get itemscontrol from normal paragraph:
      false => (this.Parent is Border b &&
               b.Parent is ContentControl cc &&
               cc.Parent is ContentPresenter cp &&
               cp.Parent is ItemsControl ic) ? ic : null!
   };

      

   private void EditableParagraph_Loaded(object? sender, RoutedEventArgs e)
   {            
      if (this.DataContext is not Paragraph thisPar) return;
      
      myDocIC = GetDocIC;

     
      //Ensure empty runs (paragraphs/linebreaks)
      if (thisPar.Inlines.Count == 0)
         thisPar.Inlines.Add(new EditableRun(""));

      List<int> lineBreakIndexes = thisPar.Inlines.OfType<EditableLineBreak>().ToList().ConvertAll(elb => thisPar.Inlines.IndexOf(elb));
      for (int idx = lineBreakIndexes.Count - 1; idx >= 0; idx--)
      {
         int elbIdx = lineBreakIndexes[idx];

         if (elbIdx == thisPar.Inlines.Count - 1 || thisPar.Inlines[elbIdx + 1] is not EditableRun erun)
            thisPar.Inlines.Insert(elbIdx + 1, new EditableRun(""));
      }

      UpdateInlines();


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
               //this.UpdateLayout();

               //if (TextLayout != null && TextLayout.TextLines.Count > 0)
               //{
               //   double maxLineHeight = Math.Max(TextLayout.TextLines[0].Height, TextLayout.TextLines[^1].Height);
               //   ThisPar.LineHeight = maxLineHeight;
               //}
               ////Debug.WriteLine("\nline spacing changed: LINESpacing = " + this.LineSpacing);

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

            case "DataContext":
               //Debug.WriteLine("datacontext changed");
               ThisPar.TextLayout = this.TextLayout;
               break;
         }

      }

   }

   //protected override void OnPointerPressed(PointerPressedEventArgs e) { /*Prevent default behavior*/  }
   //protected override void OnPointerReleased(PointerReleasedEventArgs e) { /* Prevent default behavior*/ }

   protected override void OnKeyDown(KeyEventArgs e)
   {
      //Keep to override default behavior
      
      //if (!this.IsFocused)
      //   e.Handled = true;
      //UpdateVMFromEPEnd();
   }

   protected override void OnLostFocus(FocusChangedEventArgs e)
   {
      base.OnLostFocus(e);
      this.Focusable = false;
   }

   protected override void OnGotFocus(FocusChangedEventArgs e)
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

