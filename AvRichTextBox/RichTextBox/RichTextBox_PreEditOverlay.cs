using Avalonia.Controls.Shapes;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvRichTextBox;

public partial class RichTextBox
{

   internal void CreateClient()
   {
      InputMethod.SetIsInputMethodEnabled(this, true);
      this.TextInputMethodClientRequested += RichTextBox_TextInputMethodClientRequested;

      client = new RichTextBoxTextInputClient(this);
      //Debug.WriteLine("created new client)");

      this.Focus();

   }

   RichTextBoxTextInputClient client = null!;

   private void RichTextBox_TextInputMethodClientRequested(object? sender, TextInputMethodClientRequestedEventArgs e)
   {
     
      if (e.GetType() == typeof(TextInputMethodClientRequestedEventArgs))
      {
         client ??= new RichTextBoxTextInputClient(this);
        
         e.Client = client;

         //Debug.WriteLine("e.Client requested = " + e.Client.Selection.ToString());

      }

   }

   string _preeditText = "";

   internal void InsertPreeditText(string preeditText)
   {      
      _preeditText = preeditText;
      //Debug.WriteLine("preditexttext = *" + _preeditText + "*");
      UpdatePreeditOverlay();
      
   }

   internal Point CaretPosition { get; set; }
   public Point GetCurrentPosition() => CaretPosition;


   private void UpdatePreeditOverlay()
   {

      if (!string.IsNullOrEmpty(_preeditText) && _CaretRect != null)
      {
         double cX = _CaretRect.Margin.Left - 2;
         double cY = _CaretRect.Margin.Top - 2;

         PreeditOverlay.Text = _preeditText;
         PreeditOverlay.Margin = new Thickness(cX, cY, 0, 0);
         PreeditOverlay.IsVisible = true;
         CaretPosition = new Point(cX, cY - rtbVM.RTBScrollOffset.Y);
         client.UpdateCaretPosition();

      }
      else
      {
         PreeditOverlay.IsVisible = false;
      }
   }


   private readonly Rectangle? _CaretRect = new()
   {
      StrokeThickness = 2,
      Stroke = Brushes.Black,
      Height = 20,
      Width = 1.5,
      IsVisible = false,
      HorizontalAlignment = HorizontalAlignment.Left,
      VerticalAlignment = VerticalAlignment.Top,
      IsHitTestVisible = false
   };


}

