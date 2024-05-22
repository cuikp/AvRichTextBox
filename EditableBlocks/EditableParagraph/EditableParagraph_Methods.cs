using Avalonia.Controls.Documents;
using Avalonia.Media;
using System;

namespace AvRichTextBox;

public partial class EditableParagraph 
{
   
    private InlineCollection GetFormattedInlines()
    {

        InlineCollection returnInlines = [];
        foreach (IEditable ied in ((Paragraph)this.DataContext!).Inlines)
            returnInlines.Add(ied.BaseInline); 

        return returnInlines;

    }


    private int GetClosestIndex(int lineNo, double distanceFromLeft, int direction)
    {
        CharacterHit chit = this.TextLayout.TextLines[lineNo + direction].GetCharacterHitFromDistance(distanceFromLeft);

        double CharDistanceDiffThis = Math.Abs(distanceFromLeft - this.TextLayout.HitTestTextPosition(chit.FirstCharacterIndex).Left);
        double CharDistanceDiffNext = Math.Abs(distanceFromLeft - this.TextLayout.HitTestTextPosition(chit.FirstCharacterIndex + 1).Left);

        if (CharDistanceDiffThis > CharDistanceDiffNext)
            return chit.FirstCharacterIndex + 1;
        else
            return chit.FirstCharacterIndex;


    }


}

