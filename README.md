[README.md](https://github.com/user-attachments/files/21556780/README.md)
# A RichTextBox control for Avalonia
[![NuGet version](https://img.shields.io/nuget/v/Simplecto.Avalonia.RichTextBox.svg?cachebuster=1)](https://www.nuget.org/packages/Simplecto.Avalonia.RichTextBox/)

As of ~~2024~~2025, Avalonia doesn't yet come with a RichTextBox, and since I needed one I created a "poor-man's version" based on the existing control `SelectableTextBlock`.

Mirroring WPF, this `RichTextBox` control uses the concept of a `FlowDocument` (`FlowDoc`), which contains `Blocks` (at the current time, only `Paragraph` is available, although `Section` or `Table` could be added later). 
`Paragraph` contains `IEditable` objects (`EditableRun` (from `Avalonia.Run`) and `EditableInlineUIContainer` (from `Avalonia.InlineUIContainer`)) and it is bound to an `EditableParagraph` (inheriting from `SelectableTextBlock`).

The `FlowDoc` is at heart merely an `ObservableCollection` of Blocks bound as the `ItemsSource` of an `ItemsControl` inside a `ScrollViewer`. Upon adding the appropriate key input handling, the control functions like a `RichTextBox`.

(The hard part after that was implementing the selection logic, because `Selection` for the `RichTextBox` has to be able to move between and span multiple Paragraphs (SelectableTextBlocks), both with the keyboard and the mouse, and to allow editing functions that involve splitting or merging Paragraphs. And of course the Inline logic for spanning, inserting, splitting or deleting Inlines.)

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

**A Debugging panel can be displayed by setting "ShowDebuggerPanelInDebugMode" to True.  The panel displays Inline debugging information - Inline starts, paragraph starts, inline texts, and indicates the inlines of the Selection start and end by background color coding.  The Debugger panel is not shown in Release mode**

The RichTextBox has the usual key functions:
* <kbd>Ctrl</kbd>+<kbd>B</kbd> for **bold**/unbold
* <kbd>Ctrl</kbd>-<kbd>I</kbd> for *italic*/unitalic
* <kbd>Ctrl</kbd>-<kbd>U</kbd> for <u>underline</u>/remove underline
* <kbd>Ctrl</kbd>-<kbd>Z</kbd> for undo
* <kbd>Ctrl</kbd>-<kbd>A</kbd> for select all

The `FlowDoc` has a `Selection` property, with `Start`, `End`, `Length`, `Select`, `Delete`, `Text`, etc.

The `RichTextBox` also includes the concept of `TextRange` (of which `Selection` is merely a special case), which can be defined to format text from code independent from the current `FlowDoc.Selection`. A new `TextRange` is created with a `Start` and `End` (and its owning `FlowDoc`), whereby it is automatically added to the `FlowDoc's TextRanges List` so its Start and/or End can be updated whenever text changes in the `FlowDoc` require it. `TextRange` also has an `ApplyFormatting` property which allows any `AvaloniaProperty` to be applied that pertains to Inlines.

The RichTextBox content can be saved/loaded either as straight Xaml or a XamlPackage (to preserve images), similar to the WPF RichTextBox.
It can also save and load the FlowDoc content as a Word document (.docx), Rtf document (.rtf) or Html (.html), though only with a subset of attributes.  This includes text, common text/paragraph formatting, images, highlighting, forecolor, justification, borders, etc.  


## Various future to-do improvements include:
* Word/Html/RTF export and import can be fleshed out (to support more attributes)
* Save/Load Xaml, Rtf functionality (to/from a stream) for TextRanges 
* Adding Table support
* Allow the Undo limit to be set, and create a Redo stack
* Stress testing

RtfDomParser used for parsing of rtf files can be found at https://github.com/SourceCodeBackup/RtfDomParser, but for this project I had to manually modify it to use Avalonia.Media instead of System.Drawing.  Generation of .rtf is my own concoction with the bare minimum to produce a readable .rtf file/dataobject.

**Added 2025/02/22:
Internal binding was of the RTB itself to its viewmodel, which prevented external binding to UserControl properties (such as IsVisible).  Internal binding is now to the immediate child (DockPanel "MainDP"), freeing up the properties of the UserControl itself.
Also upgraded copy/paste to allow copying and pasting of paragraph breaks (\r), which were ignored before.

**Added 2025/02/25**
ver 1.0.16 now works with Avalonia 11.1.xx & 11.2.xx!  Binding update issues resolved.  Previous AvRichTextBox versions failed on Avalonia 11.1 and higher and have been deprecated.
In addition, added IME support for Chinese/Japanese input.  Kanji and Hanzi can now be directly inputted in the RichTextBox.

**Added 2025/02/26**
ver 1.0.17 improves IME popup location and behavior (Hides on Esc key, or after backspacing to null entry).
In addition, the RichTextBox content can now be saved as .rtf  (SaveRtfDoc(string fileName)).  As of now, not all attributes are honored in the save (only bold, fontsize, italic and underline).

**Added 2025/02/27**
ver 1.2.0 - Copying of richtext content (rtf format) is now possible.  Some navigation and pasting fixes/improvements.  Also pasting of large-volume text is now much faster.  Technically 1.0.16 should have been numbered 1.2.0 but hey.

**Added 2025/02/28**
ver. 1.2.1 - FontFamily now included in rtf copy/paste, fixed Word reading error due to fonts

**Added 2025/04/05**
ver. 1.2.6 - some minor fixes: run break errors, and better handling of Word colors.  Also setting ShowDebuggerPanelInDebugMode at runtime will now dynamically show/hide the Debugger panel.

**Added 2025/04/06**
ver. 1.3.0 - Can save/load as Html.  Rtf images/line spacing now saved.  Paragraph borders, colors and backgrounds supported.

**Added 2025/04/09**
ver. 1.3.2 - Changed the underlying strategy for Undo. Undo now creates clones instead of retaining objects, which was causing problems during complex Undo sequences.
Also made ShowDebuggerPanelInDebugMode default to False. 

**Added 2025/08/02**
ver. 1.3.8 - Includes changes such as fix to mouse selection (wasn't working in Release mode), double/triple clicking to select word/paragraph, and IsReadOnly property for the RichTextBox.
