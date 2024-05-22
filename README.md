## A RichTextBox control for Avalonia
[![NuGet version](https://img.shields.io/nuget/v/Simplecto.Avalonia.RichTextBox.svg?cachebuster=1)](https://www.nuget.org/packages/Simplecto.Avalonia.RichTextBox/)

As of 2024, Avalonia doesn't yet come with a RichTextBox, and since I needed one I created a "poor-man's version" based on the existing control "SelectableTextBox".

Mirroring WPF, this RichTextBox control uses the concept of a FlowDocument (FlowDoc), which contains Blocks (at the current time, only "Paragraph" is available, although Section or Table could be added later). A "Paragraph" contains "IEditable" objects (EditableRun (from Avalonia.Run) and EditableInlineUIContainer (from Avalonia.InlineUIContainer)) and it is bound to an "EditableParagraph" (inheriting from SelectableTextBlock).

The FlowDoc is at heart merely an ObservableCollection of Blocks bound as the ItemsSource of an ItemsControl inside a ScrollViewer. Upon adding the appropriate key input handling, voila, a RichTextBox magically appeared.

(The hard part after that was implementing the selection logic, because Selection for the RichTextBox has to be able to move between and span multiple Paragraphs (SelectableTextBlocks), both with the keyboard and the mouse, and to allow editing functions that involve splitting or merging Paragraphs. And of course the Inline logic for spanning, inserting, splitting or deleting Inlines.

The RichTextBox has the usual key functions:<br>
Ctrl-B for bold/unbold<br>
Ctrl-I for italic/unitalic<br> 
Ctrl-U for underline/remove underline<br>
Ctrl-Z for undo<br>
Ctrl-A for select all<br>

The FlowDoc has a "Selection" property, with Start, End, Length, Select, Delete, Text, etc.

The RichTextBox also includes the concept of TextRange (of which Selection is merely a special case), which can be defined to format text from code independent from the current FlowDoc.Selection. A new TextRange is created with a Start and End (and its owning FlowDoc), whereby it is automatically added to the FlowDoc's TextRanges List so its Start and/or End can be updated whenever text changes in the FlowDoc require it. TextRange also has an ApplyFormatting property which allows any AvaloniaProperty to be applied that pertains to Inlines.

I've tried to add Undos for all editing possibilities, but Undo hasn't really been stress-tested to the max, yet. There is no particular limit set for number of undos at the current time. (Redo doesn't exist yet.)

The RichTextBox content can be saved/loaded either as straight Xaml or a XamlPackage (to preserve images), similar to the WPF RichTextBox.<br>
It can also save and load the FlowDoc content as a Word document (.docx), though only with a subset of Word document features.  This includes text, some common text/paragraph formatting, and most images, but not very much else at this time.<br>


Various future to-do improvements include:<br>

**Finish paragraph formatting (such as line spacing) <br>
**RTF import/export (need to find a reasonably solid RTF parser)<br>
**Save/Load Xaml (to/from a stream) for Selection and any given TextRange <br>

Adding Table and Section Block types<br>
Allow the Undo limit to be set <br>
Redo functionality (could be a headache) <br>
More stress testing <br>
A quirk or two at times when extending selection using PageUp or PageDown Key.<br>
