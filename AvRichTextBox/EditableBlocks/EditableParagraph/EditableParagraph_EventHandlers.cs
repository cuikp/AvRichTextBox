using System.Collections.Generic;

namespace AvRichTextBox;

internal partial class EditableParagraph
{
    internal delegate void MouseMoveHandler(EditableParagraph sender, int charIndex);
    internal event MouseMoveHandler? MouseMove;

    internal delegate void MouseLeaveHandler(EditableParagraph sender);
    internal event MouseLeaveHandler? MouseLeave;

    //internal delegate void KeyDownHandler(EditableParagraph sender, double hitPositionFromLeft);
    //internal new event KeyDownHandler? KeyDown;


}

