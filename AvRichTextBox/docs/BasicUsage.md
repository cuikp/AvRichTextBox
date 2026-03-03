# Using AvRichTextBox in Avalonia

## Adding directly in Xaml

```xaml  
<avrtb:RichTextBox >

	<avrtb:RichTextBox.FlowDocument>
		<avrtb:FlowDocument >
			<avrtb:FlowDocument.Blocks>

            <avrtb:Paragraph>
					<avrtb:Paragraph.Inlines>
						<avrtb:EditableRun Text="This is a line of text. "/>
                  <avrtb:EditableRun Text="With a second run."/>
					</avrtb:Paragraph.Inlines>
				</avrtb:Paragraph>

				<avrtb:Paragraph>
					<avrtb:Paragraph.Inlines>
						<avrtb:EditableInlineUIContainer>
							<Image Width="100" Height="60" Source="avares://DemoApp_AvRichTextBox/Assets/avalonia-logo.ico"/>
						</avrtb:EditableInlineUIContainer>
					</avrtb:Paragraph.Inlines>
				</avrtb:Paragraph>

			</avrtb:FlowDocument.Blocks>
		</avrtb:FlowDocument>
	</avrtb:RichTextBox.FlowDocument>

</avrtb:RichTextBox>
```

## Adding a table
```xaml  
<avrtb:RichTextBox  >

	<avrtb:RichTextBox.FlowDocument>
		<avrtb:FlowDocument >
			<avrtb:FlowDocument.Blocks>

				<avrtb:Table ColDefs="100, 150, 100, 100" RowDefs="100, 100, 100" >
					
					<avrtb:Table.Cells>
						<avrtb:Cell RowNo="0" ColNo="0" BorderBrush="Red" BorderThickness="1" >
							<avrtb:Cell.CellContent>
								<avrtb:Paragraph>
									<avrtb:Paragraph.Inlines>
										<avrtb:EditableRun Text="Some cell text"/>
									</avrtb:Paragraph.Inlines>
								</avrtb:Paragraph>
							</avrtb:Cell.CellContent>
							</avrtb:Cell>
						<avrtb:Cell RowNo="0" ColNo="2" BorderBrush="Red" BorderThickness="3"/>
						<avrtb:Cell RowNo="2" ColNo="1" BorderBrush="Green" BorderThickness="2"/>
						<avrtb:Cell RowNo="2" ColNo="3" BorderBrush="Red" BorderThickness="2"/>
					</avrtb:Table.Cells>

				</avrtb:Table>

		  </avrtb:FlowDocument.Blocks>
		</avrtb:FlowDocument>
	</avrtb:RichTextBox.FlowDocument>

</avrtb:RichTextBox>
```
