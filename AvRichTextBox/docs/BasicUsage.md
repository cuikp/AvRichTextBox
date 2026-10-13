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

## Display text without user selection

Set `IsSelectionEnabled` to `False` to prevent mouse and keyboard selection.

```xaml
<avrtb:RichTextBox IsSelectionEnabled="False"
                  IsReadOnly="True"
                  CtrlKeyOpensHyperlink="False" />
```

Hyperlinks remain clickable. `CtrlKeyOpensHyperlink` still controls whether a link requires the Ctrl key.

`IsSelectionEnabled` is a bindable property with a default value of `True`.
When this property becomes `False`, the control clears the current selection and stops an active mouse selection.
Selections made through the document API remain available, but the control hides their text and table highlights.
The Copy and Cut commands do not copy text while selection is disabled.

`IsSelectionEnabled` does not prevent text edits or caret movement. Use `IsReadOnly="True"` to prevent text edits, as in this example.

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

