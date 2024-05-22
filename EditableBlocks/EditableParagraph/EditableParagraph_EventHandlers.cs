using Avalonia;
using System.Collections.Generic;

namespace AvRichTextBox;

public partial class EditableParagraph
{
   public delegate void MouseMoveHandler(EditableParagraph sender, int charIndex);
    public event MouseMoveHandler? MouseMove;

    public delegate void SelectionStartRectChangedHandler(EditableParagraph sender);
    public event SelectionStartRectChangedHandler? SelectionStartRect_Changed;

    public delegate void SelectionEndRectChangedHandler(EditableParagraph sender);
    public event SelectionEndRectChangedHandler? SelectionEndRect_Changed;

   //public delegate void TextChangedHandler(EditableParagraph sender);
   //public event TextChangedHandler? EditableParagraph_TextChanged;

   //public delegate void CharIndexRectNotifiedHandler(EditableParagraph sender, Rect selEndRect);
   //public event CharIndexRectNotifiedHandler? CharIndexRect_Notified;

   //public delegate void KeyDownHandler(EditableParagraph sender, double hitPositionFromLeft);
   //public new event KeyDownHandler? KeyDown;


}

