# Using AvRichTextBox in Avalonia

## Adding directly in Xaml

```xaml  
<avrtb:RichTextBox >

	<avrtb:RichTextBox.FlowDocument>
		<avrtb:FlowDocument >
			<avrtb:FlowDocument.Blocks>

            <avrtb:Paragraph>
					<avrtb:Paragraph.GetInlines>
						<avrtb:EditableRun Text="This is a line of text. "/>
                  <avrtb:EditableRun Text="With a second run."/>
					</avrtb:Paragraph.GetInlines>
				</avrtb:Paragraph>

				<avrtb:Paragraph>
						<avrtb:Paragraph.GetInlines>
							<avrtb:EditableInlineUIContainer>
								<avrtb:EditableInlineUIContainer.Content>
									<Image Width="100" Height="60" Source="avares://DemoApp_AvRichTextBox/Assets/avalonia-logo.ico"/>	
								</avrtb:EditableInlineUIContainer.Content>
							</avrtb:EditableInlineUIContainer>
						</avrtb:Paragraph.GetInlines>
				</avrtb:Paragraph>


			</avrtb:FlowDocument.Blocks>
		</avrtb:FlowDocument>
	</avrtb:RichTextBox.FlowDocument>

</avrtb:RichTextBox>
```

## Markdown

Use `LoadMarkdown` to replace the document and `SaveMarkdown` to get Markdown text:

```csharp
editor.LoadMarkdown("# Notes\n\nText with **bold**, *italic*, and [a link](https://example.com).");
string markdown = editor.SaveMarkdown();

editor.LoadMarkdownDoc("notes.md");
editor.SaveMarkdownDoc("notes.md");
```

`FlowDocument` provides the same string methods without a visible control. It also supports UTF-8 streams.
Stream methods start at the current position and leave the stream open.

```csharp
var document = new FlowDocument();
document.LoadMarkdown(inputStream);
document.SaveMarkdown(outputStream);
```

Use `ContentDataFormat.Markdown` to save or replace a `TextRange`. `InsertMarkdown` replaces the current selection as one undoable edit.
Insertion supports paragraph boundaries in the document or within one table cell.
A selection that crosses table cell boundaries raises `NotSupportedException` before any change.

```csharp
editor.FlowDocument.InsertMarkdown("**Inserted text**");
editor.FlowDocument.Selection.Save(outputStream, ContentDataFormat.Markdown);
editor.FlowDocument.Selection.Load(inputStream, ContentDataFormat.Markdown);

await editor.CopyMarkdownAsync();
await editor.PasteMarkdownAsync();
```

The clipboard methods return `false` when the operation is unavailable. Copy respects `DisableUserCopy`, and paste respects `IsReadOnly`.
Ordinary paste keeps its existing behavior. The explicit Markdown paste method parses clipboard text as Markdown.

Supported syntax includes six heading levels, paragraphs, bold, italic, strikethrough, inline code, fenced and indented code blocks,
ordered and unordered lists, nested lists, block quotes, hard line breaks, links, image references, thematic breaks, and pipe tables.
Image references appear as links with their alternative text. The importer does not read or download image files.
Raw HTML appears as literal text.

Export produces normalized Markdown. It does not retain the original spacing, delimiter choice, or reference-link definitions.
Table export uses the first row as the header, joins multiple cell paragraphs with spaces, and does not preserve merged cells.
Embedded UI controls and formatting without Markdown equivalents, such as colors and page margins, are not exported.
Markdown structure follows document clones and undo/redo. Other export formats do not retain the Markdown-specific structure metadata.

An application can use these APIs to switch between a source editor and the rendered document.

## Adding a table
```xaml  
<avrtb:RichTextBox  >

	<avrtb:RichTextBox.FlowDocument>
		<avrtb:FlowDocument >
			<avrtb:FlowDocument.Blocks>

			<avrtb:Table ColDefs="100, 150, 100" RowDefs="100, 100, 100" >

					<avrtb:Table.GetCells>
						<avrtb:Cell RowNo="0" ColNo="0" BorderBrush="Red" BorderThickness="1" >
							<avrtb:Cell.GetCellBlocks>
								<avrtb:Paragraph>
									<avrtb:Paragraph.GetInlines>
										<avrtb:EditableRun Text="Some cell text"/>
									</avrtb:Paragraph.GetInlines>
								</avrtb:Paragraph>
							</avrtb:Cell.GetCellBlocks>
							</avrtb:Cell>
						<avrtb:Cell RowNo="0" ColNo="1" BorderBrush="Red" BorderThickness="3"/>
						<avrtb:Cell RowNo="0" ColNo="2" BorderBrush="Blue" BorderThickness="3"/>
						<avrtb:Cell RowNo="1" ColNo="0" BorderBrush="Green" BorderThickness="2"/>
						<avrtb:Cell RowNo="1" ColNo="1" BorderBrush="Black" BorderThickness="2"/>
						<avrtb:Cell RowNo="1" ColNo="2" BorderThickness="2"/>
						<avrtb:Cell RowNo="2" ColNo="0" BorderThickness="2"/>
						<avrtb:Cell RowNo="2" ColNo="1" BorderThickness="2"/>
						<avrtb:Cell RowNo="2" ColNo="2" BorderThickness="2"/>
					</avrtb:Table.GetCells>				
				

					</avrtb:Table>
				

		  </avrtb:FlowDocument.Blocks>
		</avrtb:FlowDocument>
	</avrtb:RichTextBox.FlowDocument>

</avrtb:RichTextBox>
```

