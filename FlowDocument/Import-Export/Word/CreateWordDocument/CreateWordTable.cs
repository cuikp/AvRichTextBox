//using DocumentFormat.OpenXml;
//using DocumentFormat.OpenXml.Packaging;
//using DocumentFormat.OpenXml.Wordprocessing;
//using System.Diagnostics;
//using System.Windows.Media.Imaging;
//using System.Xml;
//using DOW = DocumentFormat.OpenXml.Wordprocessing;

//namespace QuikFind;

//public static partial class WordDocumentCreator
//{
//    static bool firstTable = true;

//    public static DOW.Table CreateWordDocTable(System.Windows.Documents.Table t)
//    {

//        var tabl = new DOW.Table();
    
//        List<Point> VMergeCount = new List<Point>();
//        for (int i = 0; i < 100; i++) VMergeCount.Add(new Point(1, 0));

//        // Add column definitions
//        var tblGrid = new TableGrid();
//        for (int colno = 0; colno < t.Columns.Count; colno++)
//            tblGrid.Append(new GridColumn() { Width = PixToTwip(t.Columns[colno].Width.Value).ToString() });
//        tabl.Append(tblGrid);

//        int coloffset = 0;

//        foreach (TableRowGroup rgroup in t.RowGroups)
//        {
//            foreach (System.Windows.Documents.TableRow r in rgroup.Rows)
//            {
//                var trow = new DOW.TableRow();
//                tabl.AppendChild(trow);
//                coloffset = 0;
                
//                for (int colno = 0; colno < t.Columns.Count; colno++)
//                {

//                    var cel = new DOW.TableCell();
//                    trow.Append(cel);
//                    var tcprop = new TableCellProperties();
//                    cel.Append(tcprop);


//                    var tcwid = new TableCellWidth() { Width = PixToTwip(t.Columns[colno].Width.Value).ToString() };
//                    tcprop.Append(tcwid);

//                    var tcbor = new TableCellBorders();
//                    tcprop.Append(tcbor);

//                    if (VMergeCount[colno].X > 1)
//                    {
//                        tcprop.Append(new VerticalMerge() { Val = new EnumValue<MergedCellValues>(MergedCellValues.Continue) });
//                        tcprop.Append(new GridSpan() { Val = (Int32Value)(VMergeCount[colno].Y + 1) });

//                        VMergeCount[colno] = new Point(VMergeCount[colno].X - 1, VMergeCount[colno].Y);

//                        colno += (int)VMergeCount[colno].Y;
//                        coloffset += (int)VMergeCount[colno].Y + 1;

//                        var pprop = new ParagraphProperties() { SpacingBetweenLines = new SpacingBetweenLines() { Line = "240", LineRule = LineSpacingRuleValues.Auto, Before = "0", After = "0" } };
//                        var newpar = new DOW.Paragraph();
//                        newpar.Append(pprop);

//                        cel.Append(newpar);   // necessary to add paragraph to vmerged cell
//                    }
//                    else
//                    {
//                        tcprop.Append(new VerticalMerge() { Val = new EnumValue<MergedCellValues>(MergedCellValues.Restart) });

//                        foreach (Block b in r.Cells[colno - coloffset].Blocks)
//                            cel.Append(CreateWordDocParagraph((System.Windows.Documents.Paragraph)b));

//                        if (r.Cells[colno - coloffset].RowSpan > 1)
//                            VMergeCount[colno] = new Point(r.Cells[colno - coloffset].RowSpan, r.Cells[colno - coloffset].ColumnSpan - 1);

//                        if (r.Cells[colno - coloffset].ColumnSpan > 1)
//                            tcprop.GridSpan = new GridSpan() { Val = r.Cells[colno - coloffset].ColumnSpan };
//                        else
//                            VMergeCount[colno] = new Point(VMergeCount[colno].X, 0);

//                        var thisCellVertAlign = new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Top };
//                        switch (r.Cells[colno - coloffset].BorderThickness.Left)
//                        {
//                            case 0.49:
//                                thisCellVertAlign.Val = TableVerticalAlignmentValues.Center;
//                                break;
//                            case 0.48:
//                                thisCellVertAlign.Val = TableVerticalAlignmentValues.Bottom;
//                                break;
//                        }
//                        tcprop.Append(thisCellVertAlign);


//                        var lmar = new LeftMargin() { Width = PixToTwip(r.Cells[colno - coloffset].Padding.Left).ToString() };
//                        var tmar = new TopMargin() { Width = PixToTwip(r.Cells[colno - coloffset].Padding.Top).ToString() };
//                        var rmar = new RightMargin() { Width = PixToTwip(r.Cells[colno - coloffset].Padding.Right).ToString() };
//                        var bmar = new BottomMargin() { Width = PixToTwip(r.Cells[colno - coloffset].Padding.Bottom).ToString() };
//                        var tcmar = new TableCellMargin() { LeftMargin = lmar, TopMargin = tmar, RightMargin = rmar, BottomMargin = bmar };
//                        tcprop.Append(tcmar);

//                        int thiscolspan = r.Cells[colno - coloffset].ColumnSpan;
//                        VMergeCount[colno] = new Point(VMergeCount[colno].X, thiscolspan - 1);
//                        colno += thiscolspan - 1;
//                        coloffset += thiscolspan - 1;
//                    }

//                    tcbor.Append(new LeftBorder() { Size = 7, Color = "#000000", Val = BorderValues.Single });
//                    tcbor.Append(new RightBorder() { Size = 7, Color = "#000000", Val = BorderValues.Single });
//                    tcbor.Append(new TopBorder() { Size = 7, Color = "#000000", Val = BorderValues.Single });
//                    tcbor.Append(new BottomBorder() { Size = 7, Color = "#000000", Val = BorderValues.Single});
//                }
//            }
//        }

//        return tabl;
//    }


//}
