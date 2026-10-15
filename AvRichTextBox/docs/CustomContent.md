# Custom blocks and inline content

Use `CustomInline` for mentions, emoji, badges, or other atomic content within a paragraph.
Use `CustomBlock` for an application widget in its own paragraph.
The editor handles document positions, selection, cloning, and undo. The application supplies the controls and reads their data.

## Define a view in the application

For example, define `MentionView.axaml` with compiled bindings to `CustomContent`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:rtb="using:AvRichTextBox"
             x:Class="Example.MentionView"
             x:DataType="rtb:CustomContent">
    <Border Padding="4,1" CornerRadius="4" Background="LightBlue">
        <TextBlock Text="{Binding FallbackText}" />
    </Border>
</UserControl>
```

The view uses a normal Avalonia code-behind class:

```csharp
namespace Example;

public partial class MentionView : Avalonia.Controls.UserControl
{
    public MentionView() => InitializeComponent();
}
```

## Register content and insert it

Register each type on the document. A factory must return a new control for each call.
Factories can run during cloning as well as insertion. Do not return a shared control or an existing child from the visual tree.

```csharp
FlowDocument document = editor.FlowDocument;
document.RegisterCustomContent("mention", content =>
    new MentionView { DataContext = content });

// Replace a matched @123 token. Positions use the document's text coordinates.
CustomInline mention = document.InsertCustomInline(
    new CustomContent("mention", "123", "@User"), tokenStart, tokenLength);

// Put the same application content in its own block.
CustomBlock block = document.InsertCustomBlock(
    document.Blocks.Count, new CustomContent("mention", "456", "@Other"));
```

Each inline occupies one document position, like an existing inline image.
An inline replacement must stay within one paragraph. Blocks can also be inserted in a table cell:

```csharp
document.InsertCustomBlock(0, new CustomContent("mention", "123", "@User"), cell);
```

The application handles token detection, suggestions, and the choice of content.
For example, after the user accepts an `@`, `:`, or `/` suggestion, replace the matched token with `InsertCustomInline` or insert a block.
The editor does not process those characters automatically.

## Change content

`CustomContent` contains an application-defined type, an opaque data string, and fallback text.
The editor does not instantiate types from the data or process the payload.
To change an inserted element, replace its definition:

```csharp
mention.Definition = new CustomContent("mention", "456", "@Other");
```

The change refreshes the control and participates in undo/redo.
Custom block content is available through `block.Content`; it is null after the user removes the contained element.

## Save, load, and copy

`SaveXamlString` and `LoadXamlString` retain the content definitions and custom block identity.
XAML packages use the same representation. Custom content uses the `urn:avrichtextbox:custom-content` namespace.
This is an AvRichTextBox extension to the XAML document format.

```csharp
string saved = editor.SaveXamlString();
editor.LoadXamlString(saved);
```

The definitions also survive `TextRange.Save`/`Load` with `ContentDataFormat.Xaml`.
When copying custom content, the clipboard includes its XAML representation, RTF, and fallback text.
Pasting that representation into AvRichTextBox retains the definitions and supports undo/redo.
Custom-content paste accepts paragraph boundaries in one block collection or one table cell.

Register factories on each destination document. Without a factory, the editor displays the fallback text and retains the data for later registration.
Factories belong to the document and are not saved or shared with other documents.

Plain-text, RTF, HTML, and Word export use fallback text. These formats do not retain the custom content data or controls.
Internal document text uses an object marker for each custom inline; use text export when the fallback text is required.
Control state must be stored in `CustomContent.Data` by the application before saving. The editor does not serialize the live control tree.
