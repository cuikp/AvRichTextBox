using Avalonia;
using Avalonia.Controls;
using System;
using System.Diagnostics;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;

namespace AvRichTextBox;

public partial class RichTextBox
{

   public class RichTextBoxTextInputClient(RichTextBox owner) : TextInputMethodClient
   {
      private readonly RichTextBox _owner = owner;

      private void RichTextBoxTextInputClient_TextViewVisualChanged(object? sender, EventArgs e)
      {
         //Debug.WriteLine("visual changed");
      }

      private void RichTextBoxTextInputClient_SelectionChanged(object? sender, EventArgs e)
      {
         //Debug.WriteLine("selection changed");
      }

      private double GetAdjustedCaretY (double yval) 
      {
         //Debug.WriteLine("ownerbounds bottom=" + _owner.Bounds.Bottom + " // yval= " + yval);  
         return (yval > _owner.Bounds.Bottom - 200) ? yval - 200 : yval + 22; 
      }

      public override Rect CursorRectangle => new(_owner.CaretPosition.X + 12, GetAdjustedCaretY(_owner.CaretPosition.Y),  1, 0);
      
      public void UpdateCaretPosition()
      {
         RaiseCursorRectangleChanged();
      }
       
      public override bool SupportsPreedit => true;

      public override bool SupportsSurroundingText => false;
      public override string SurroundingText => "";


      public override TextSelection Selection
      {
         get => new (_owner.FlowDoc.Selection.Start, _owner.FlowDoc.Selection.End);
         set { }
      }

      public override Visual TextViewVisual => null!;

      public override void SetPreeditText(string? preeditText)
      {
         _owner.InsertPreeditText(preeditText!);
      }
   }
}