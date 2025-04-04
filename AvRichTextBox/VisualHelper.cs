using Avalonia;
using Avalonia.Styling;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AvRichTextBox;

public static class VisualHelper
{
    internal static double GetUserY(this Visual? visual)
    {
        if (visual == null || !visual.IsVisible)
            return 0;

        Rect vRect = visual!.GetTransformedBounds()!.Value.Clip;
        
        return vRect.Y;

    }


    internal static Dictionary<EditableParagraph, Rect> GetVisibleEditableParagraphs(this Visual? itemsControl) // where T : Visual
    {
        var items = new Dictionary<EditableParagraph, Rect>();

        if (itemsControl == null || !itemsControl.IsVisible)
            return items;
        
        var edPars = itemsControl.GetVisualDescendants().OfType<EditableParagraph>();
        foreach (var edpar in edPars)
        {
            Rect vRect = edpar.GetTransformedBounds()!.Value.Clip;
            //Rect vRect = edpar.Bounds;
            
            if (vRect.Y > 0)   // Greater than zero means it's visible
                items.Add(edpar, edpar.GetTransformedBounds()!.Value.Clip);
                //items.Add(edpar, edpar.GetTransformedBounds()!.Value.Bounds);
        }
        

        return items;
    }
}
