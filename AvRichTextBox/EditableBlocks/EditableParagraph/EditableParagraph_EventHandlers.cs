using System.Collections.Generic;

namespace AvRichTextBox;

internal partial class EditableParagraph
{
   public delegate void MouseMoveHandler(EditableParagraph sender, int charIndex);
   public event MouseMoveHandler? MouseMove;

   public delegate void MouseLeaveHandler(EditableParagraph sender);
   public event MouseLeaveHandler? MouseLeave;

   //public delegate void KeyDownHandler(EditableParagraph sender, double hitPositionFromLeft);
   //public new event KeyDownHandler? KeyDown;


}

