using System.Collections.ObjectModel;

namespace AvRichTextBox;

public static class StaticProperties
{
    //internal static bool DisableUndoStack {get; set { field = value; Debug.WriteLine("setting disablundo stack -> " + value); } }= false;
    internal static bool DisableUndoStack { get; set; } = false;

    internal static void AddDefaultParagraph(ObservableCollection<Block> blockCollection)
    {
        DisableUndoStack = true;
        Paragraph newpar = new();
        newpar.Inlines.Add(new EditableRun(""));
        blockCollection.Add(newpar);
        DisableUndoStack = false;

    }
}
