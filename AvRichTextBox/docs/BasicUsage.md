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

