# A RichTextBox control for Avalonia
[![NuGet version](https://img.shields.io/nuget/v/Simplecto.Avalonia.RichTextBox.svg?cachebuster=1)](https://www.nuget.org/packages/Simplecto.Avalonia.RichTextBox/)

As of 2024, Avalonia doesn't yet come with a RichTextBox, and since I needed one I created a "poor-man's version" based on the existing control `SelectableTextBlock`.

Mirroring WPF, this `RichTextBox` control uses the concept of a `FlowDocument` (`FlowDoc`), which contains `Blocks` (at the current time, only `Paragraph` is available, although `Section` or `Table` could be added later). 
`Paragraph` contains `IEditable` objects (`EditableRun` (from `Avalonia.Run`) and `EditableInlineUIContainer` (from `Avalonia.InlineUIContainer`)) and it is bound to an `EditableParagraph` (inheriting from `SelectableTextBlock`).

The `FlowDoc` is at heart merely an `ObservableCollection` of Blocks bound as the `ItemsSource` of an `ItemsControl` inside a `ScrollViewer`. Upon adding the appropriate key input handling, voila, a `RichTextBox` magically appeared.

(The hard part after that was implementing the selection logic, because `Selection` for the `RichTextBox` has to be able to move between and span multiple Paragraphs (SelectableTextBlocks), both with the keyboard and the mouse, and to allow editing functions that involve splitting or merging Paragraphs. And of course the Inline logic for spanning, inserting, splitting or deleting Inlines.

```mermaid
classDiagram
    class RichTextBox{
        +FlowDocument FlowDoc
    }
    class FlowDocument{
        +ObservableCollection<Blocks>
    }
    class FlowDoc{
        -List<TextRange> TextRanges
    }
    class Blocks{
        +Paragraph Paragraph
    }
    class Paragraph{
        +IEditable Objects
        +EditableParagraph EditableParagraph
    }
    class IEditable{
        +EditableRun EditableRun
        +EditableInlineUIContainer EditableInlineUIContainer
    }
    class Selection{
    }
    class TextRange{
        +int Start
        +int End
        +int Length
        +Delete()
        +string Text
        +ApplyFormatting(AvaloniaProperty,  object)
    }

    RichTextBox --> FlowDocument : has
    FlowDocument --> Blocks : has
    FlowDoc --> TextRange : has
    Blocks --> Paragraph : contains
    Paragraph --> IEditable : has
    TextRange --> Selection : instance
    RichTextBox --> FlowDoc : has

```

Currently, when used in Debug Mode, the RichTextBox displays Inline debugging information in a right-hand panel - Inline starts, paragraph starts, inline texts, and indicates the inlines of the Selection start and end by background color coding.  The Debugger panel is not shown in Release mode.

The RichTextBox has the usual key functions:
* <kbd>Ctrl</kbd>+<kbd>B</kbd> for **bold**/unbold
* <kbd>Ctrl</kbd>-<kbd>I</kbd> for *italic*/unitalic
* <kbd>Ctrl</kbd>-<kbd>U</kbd> for <u>underline</u>/remove underline
* <kbd>Ctrl</kbd>-<kbd>Z</kbd> for undo
* <kbd>Ctrl</kbd>-<kbd>A</kbd> for select all

The `FlowDoc` has a `Selection` property, with `Start`, `End`, `Length`, `Select`, `Delete`, `Text`, etc.

The `RichTextBox` also includes the concept of `TextRange` (of which `Selection` is merely a special case), which can be defined to format text from code independent from the current `FlowDoc.Selection`. A new `TextRange` is created with a `Start` and `End` (and its owning `FlowDoc`), whereby it is automatically added to the `FlowDoc's TextRanges List` so its Start and/or End can be updated whenever text changes in the `FlowDoc` require it. `TextRange` also has an `ApplyFormatting` property which allows any `AvaloniaProperty` to be applied that pertains to Inlines.

I've tried to add Undos for all editing possibilities, but Undo hasn't really been stress-tested to the max, yet. There is no particular limit set for number of undos at the current time. (Redo doesn't exist yet.)

The RichTextBox content can be saved/loaded either as straight Xaml or a XamlPackage (to preserve images), similar to the WPF RichTextBox.
It can also save and load the FlowDoc content as a Word document (.docx), though only with a subset of Word document features.  This includes text, some common text/paragraph formatting, and most images, but not very much else at this time.


## Various future to-do improvements include:
* Finish paragraph formatting (such as line spacing) 
* RTF export (RTF import was recently added, but unfortunately the best RTF parser I could find (RtfDomParser) is only an RTF reader, without a corresponding RTF code generator)
* Save/Load Xaml (to/from a stream) for Selection and any given TextRange 
* Adding Table and Section Block types
* Allow the Undo limit to be set 
* Redo functionality (could be a headache) 
* More stress testing 
* A quirk or two at times when extending selection using <kbd>PageUp</kbd> or <kbd>PageDown</kbd> Key.


RtfDomParser can be found at https://github.com/SourceCodeBackup/RtfDomParser, but for this project I had to manually modify it to use Avalonia.Media instead of System.Drawing

**Added 2025/02/22:
Internal binding was of the RTB itself to its viewmodel, which prevented external binding to UserControl properties (such as IsVisible).  Internal binding is now to the immediate child (DockPanel "MainDP"), freeing up the properties of the UserControl itself.

Also upgraded copy/paste to allow copying and pasting of paragraph breaks (\r), which were ignored before.

**Added 2025/02/25
ver 1.0.16 now works with Avalonia 11.1.xx & 11.2.xx!  Binding update issues resolved.
In addition, added IME support for Chinese/Japanese input.  Kanji and Hanzi can now be directly inputted in the RichTextBox.

**Added 2025/02/26
ver 1.0.17 improves IME popup location and behavior (Hides on Esc key, or after backspacing to null entry).
In addition, the RichTextBox content can now be saved as .rtf  (SaveRtfDoc(string fileName)).  As of now, not all attributes are honored in the save (only bold, fontsize, italic and underline).
