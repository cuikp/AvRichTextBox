using Avalonia.Media;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using static AvRichTextBox.HelperMethods;
using DOW = DocumentFormat.OpenXml.Wordprocessing;

namespace AvRichTextBox;

internal static partial class WordConversions
{

    internal static DOW.Table CreateWordDocTable(Table t, ref MainDocumentPart mainPart)
    {

        var tabl = new DOW.Table();

        // Add column definitions
        var tblGrid = new TableGrid();
        for (int colno = 0; colno < t.ColDefs.Count; colno++)
            tblGrid.Append(new GridColumn() { Width = PixToTwip(t.ColDefs[colno].Width.Value).ToString() });

        TableRowAlignmentValues trav = DOW.TableRowAlignmentValues.Left;
        switch (t.TableAlignment)
        {
            case Avalonia.Layout.HorizontalAlignment.Left: trav = DOW.TableRowAlignmentValues.Left; break;
            case Avalonia.Layout.HorizontalAlignment.Center: trav = DOW.TableRowAlignmentValues.Center; break;
            case Avalonia.Layout.HorizontalAlignment.Right: trav = DOW.TableRowAlignmentValues.Right; break;
        }
        DOW.TableProperties tabProps = new();
        var tjust = new DOW.TableJustification { Val = trav };
        tabProps.Append(tjust);

        // this is overriden by edge cell borders in Word...
        var tableBorderProps = new TableBorders();
        UInt32 tbLeft = (uint)PixToTwip(t.BorderThickness.Left);
        UInt32 tbRight = (uint)PixToTwip(t.BorderThickness.Right);
        UInt32 tbTop = (uint)PixToTwip(t.BorderThickness.Top);
        UInt32 tbBottom = (uint)PixToTwip(t.BorderThickness.Bottom);
        string tableBorderColor = ToOpenXmlColor(t.BorderBrush.Color);
        tableBorderProps.Append(new LeftBorder() { Size = tbLeft, Color = tableBorderColor, Val = BorderValues.Single });
        tableBorderProps.Append(new RightBorder() { Size = tbRight, Color = tableBorderColor, Val = BorderValues.Single });
        tableBorderProps.Append(new TopBorder() { Size = tbTop, Color = tableBorderColor, Val = BorderValues.Single });
        tableBorderProps.Append(new BottomBorder() { Size = tbBottom, Color = tableBorderColor, Val = BorderValues.Single });

        tabProps.Append(tableBorderProps);


        tabl.Append(tabProps);
        tabl.Append(tblGrid);

        int[] NextAvailableRows = new int[t.ColDefs.Count];

        (int span, string borderColor, Thickness borderThickness, Thickness cellPadding)[,] SpanProps = new (int span, string borderColor, Thickness borderThickness, Thickness cellPadding)[t.ColDefs.Count, t.RowDefs.Count];

        double lastRowHeight = 0;

        for (int rowno = 0; rowno < t.RowDefs.Count; rowno++)
        {
            double minCellHeight = t.Cells.Where(c => c.RowNo == rowno).Min(c1 => c1.Height);
            minCellHeight = lastRowHeight == 0 ? minCellHeight : Math.Min(minCellHeight, lastRowHeight);
            lastRowHeight = minCellHeight;

            var trPr = new TableRowProperties(new TableRowHeight
            {
                HeightType = HeightRuleValues.AtLeast,
                Val = new UInt32Value((uint)PixToTwip(minCellHeight))
            });


            var trow = new DOW.TableRow();
            trow.Append(trPr);

            tabl.AppendChild(trow);

            int currColSpan = 0;

            string currentBorderColor = "000000";
            Thickness currentBorderThickness = new(1);
            Thickness currentCellPadding = new(5);

            for (int colno = 0; colno < t.ColDefs.Count; colno++)
            {
                var tcprop = new TableCellProperties();
                var tcwid = new TableCellWidth() { Width = PixToTwip(t.ColDefs[colno].Width.Value).ToString() };

                tcprop.Append(tcwid);

                var cel = new DOW.TableCell();
                trow.Append(cel);
                cel.Append(tcprop);


                if (rowno < NextAvailableRows[colno])
                {
                    tcprop.Append(new VerticalMerge() { Val = new EnumValue<MergedCellValues>(MergedCellValues.Continue) });
                    cel.Append(CreateWordDocParagraph(new Paragraph(), ref mainPart)); // needs empty paragraph 

                    if (SpanProps[colno, rowno].span > 1)
                    {
                        tcprop.Append(new GridSpan { Val = SpanProps[colno, rowno].span });
                        currentBorderColor = SpanProps[colno, rowno].borderColor;
                        currentBorderThickness = SpanProps[colno, rowno].borderThickness;
                        currentCellPadding = SpanProps[colno, rowno].cellPadding;
                        colno += (SpanProps[colno, rowno].span - 1);
                    }
                }


                if (t.Cells.FirstOrDefault(c => c.RowNo == rowno && c.ColNo == colno) is Cell thisCell)
                {
                    tcprop.Append(new Shading
                    {
                        Val = ShadingPatternValues.Clear,
                        Color = "auto",
                        Fill = new StringValue(ToOpenXmlColor(thisCell.CellBackground == null ? Colors.Transparent : thisCell.CellBackground.Color))
                    });


                    tcprop.Append(new VerticalMerge() { Val = new EnumValue<MergedCellValues>(MergedCellValues.Restart) });
                    tcprop.Append(new GridSpan { Val = thisCell.ColSpan });

                    for (int i = colno; i < colno + thisCell.ColSpan; i++)
                        NextAvailableRows[i] = rowno + thisCell.RowSpan;

                    currentBorderColor = ToOpenXmlColor(thisCell.BorderBrush.Color);
                    currentBorderThickness = thisCell.BorderThickness;
                    currentCellPadding = thisCell.Padding;

                    for (int j = rowno + 1; j < rowno + thisCell.RowSpan; j++)
                    {
                        SpanProps[colno, j].span = thisCell.ColSpan;
                        SpanProps[colno, j].borderColor = currentBorderColor;
                        SpanProps[colno, j].borderThickness = currentBorderThickness;
                        SpanProps[colno, j].cellPadding = currentCellPadding;
                    }

                    currColSpan = thisCell.ColSpan;

                    foreach (Block b in thisCell.CellBlocks)
                    {
                        cel.Append(CreateWordDocParagraph(b, ref mainPart));
                    }

                    var thisCellVertAlign = new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Top };
                    switch (thisCell.CellVerticalAlignment)
                    {
                        case Avalonia.Layout.VerticalAlignment.Center:
                            thisCellVertAlign.Val = TableVerticalAlignmentValues.Center;
                            break;
                        case Avalonia.Layout.VerticalAlignment.Bottom:
                            thisCellVertAlign.Val = TableVerticalAlignmentValues.Bottom;
                            break;
                    }
                    tcprop.Append(thisCellVertAlign);

                    colno += (currColSpan - 1);

                }
                else
                {
                    cel.Append(CreateWordDocParagraph(new Paragraph(), ref mainPart));
                }

                //cell borders
                UInt32 cbLeft = (uint)PixToTwip(currentBorderThickness.Left);
                UInt32 cbRight = (uint)PixToTwip(currentBorderThickness.Right);
                UInt32 cbTop = (uint)PixToTwip(currentBorderThickness.Top);
                UInt32 cbBottom = (uint)PixToTwip(currentBorderThickness.Bottom);

                var tcbor = new TableCellBorders();
                tcbor.Append(new LeftBorder() { Size = cbLeft, Color = currentBorderColor, Val = BorderValues.Single });
                tcbor.Append(new RightBorder() { Size = cbRight, Color = currentBorderColor, Val = BorderValues.Single });
                tcbor.Append(new TopBorder() { Size = cbTop, Color = currentBorderColor, Val = BorderValues.Single });
                tcbor.Append(new BottomBorder() { Size = cbBottom, Color = currentBorderColor, Val = BorderValues.Single });
                tcprop.Append(tcbor);

                //Cell padding
                var lmar = new LeftMargin() { Width = PixToTwip(currentCellPadding.Left).ToString() };
                var tmar = new TopMargin() { Width = PixToTwip(currentCellPadding.Top).ToString() };
                var rmar = new RightMargin() { Width = PixToTwip(currentCellPadding.Right).ToString() };
                var bmar = new BottomMargin() { Width = PixToTwip(currentCellPadding.Bottom).ToString() };
                var tcmar = new TableCellMargin() { LeftMargin = lmar, TopMargin = tmar, RightMargin = rmar, BottomMargin = bmar };
                tcprop.Append(tcmar);

            }
        }

        return tabl;
    }


}
