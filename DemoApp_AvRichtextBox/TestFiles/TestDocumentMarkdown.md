# Markdown sample

This file shows the Markdown syntax that **AvRichTextBox** reads. Load it with *File > Load Markdown file*,
edit it, and save it again with *File > Save Markdown file*.

## Text formatting

Text can be **bold**, *italic*, ***bold and italic***, ~~struck through~~, or `inline code`.
Links look like [the AvRichTextBox repository](https://github.com/cuikp/AvRichTextBox).

A line ending with two spaces  
continues with a hard line break in the same paragraph.

## Lists

- First item
- Second item
  - Nested item
  - Another nested item
- Third item

1. Step one
2. Step two
   1. Sub-step
3. Step three

## Quote

> A block quote can hold a whole paragraph
> and continues over several source lines.

## Code

```csharp
var editor = new RichTextBox();
editor.LoadMarkdownDoc("TestDocumentMarkdown.md");
```

---

## Table

| Format | Load | Save |
|--------|------|------|
| Markdown | yes | yes |
| HTML | yes | yes |
| RTF | yes | yes |

###### Smallest heading level

The end.
