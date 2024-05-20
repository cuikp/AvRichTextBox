//using DocumentFormat.OpenXml;
//using DocumentFormat.OpenXml.Wordprocessing;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Windows;
//using System.Windows.Documents;
//using System.Windows.Media;
//using static QuikFind.FunctionsModule;

//namespace QuikFind;

//public partial class XmlToFlowDocument
//{

//    public static System.Windows.Documents.Table GetTable(DocumentFormat.OpenXml.Wordprocessing.Table wordTable)
//    {

//        var thisTable = new System.Windows.Documents.Table();
//        thisTable.RowGroups.Add(new TableRowGroup());
//        thisTable.BorderThickness = new Thickness(0.5);  // initial default
//        thisTable.BorderBrush = Brushes.Black;
//        thisTable.CellSpacing = 0;  // default is "2"
//        thisTable.Margin = new Thickness(0, 0, 0, 0); // just in case
//        thisTable.Padding = new Thickness(0, 0, 0, 0);


//        var ColumnWidths = new List<double>();
//        TableGrid tgrid = wordTable.Descendants<TableGrid>().FirstOrDefault();
//        int nocols = tgrid.Descendants<GridColumn>().Count();

//        var LastVmergedHeadCells = new System.Windows.Documents.TableCell[nocols + 1];

//        thisTable.BorderBrush = Brushes.Black;
//        thisTable.BorderThickness = new Thickness(1);

//        //MessageBox.Show(wordTable.Descendants<DocumentFormat.OpenXml.Wordprocessing.TableRow>().Count().ToString());

//        foreach (OpenXmlElement telm in wordTable.ChildElements)
//        {
//            switch (telm.LocalName)
//            {
//                case "tblGrid":

//                    foreach (OpenXmlElement gridelm in telm.ChildElements)
//                    {
//                        switch (gridelm.LocalName)
//                        {
//                            case "gridCol":
//                                var newtabcol = new TableColumn();
//                                var glc = new GridLengthConverter();
//                                newtabcol.Width = new GridLength(TwipToPix(Convert.ToDouble(gridelm.GetAttributes()[0].Value)));
//                                thisTable.Columns.Add(newtabcol);
//                                break;
//                        }
//                    }

//                    break;

//                case "tr":

//                    var newrow = new System.Windows.Documents.TableRow();
//                    thisTable.RowGroups[0].Rows.Add(newrow);
//                    int colno = 0;

//                    foreach (OpenXmlElement cn in telm.ChildElements)
//                    {
//                        switch (cn.LocalName)
//                        {
//                            case "tc":

//                                var thiscell = new System.Windows.Documents.TableCell();

//                                thiscell.Tag = new List<string>(new string[] { "", "" });

//                                foreach (OpenXmlElement CellParNode in cn.ChildElements)
//                                {
//                                    switch (CellParNode.LocalName)
//                                    {
//                                        case "tcPr":
//                                            // Get cell properties
//                                            foreach (OpenXmlElement CellPropNode in CellParNode.ChildElements)
//                                            {
//                                                switch (CellPropNode.LocalName)
//                                                {
//                                                    case "tcW":
//                                                        break;

//                                                    case "gridSpan":
//                                                        thiscell.ColumnSpan = Convert.ToInt32(CellPropNode.GetAttributes()[0].Value);
//                                                        colno += thiscell.ColumnSpan - 1;
//                                                        break;

//                                                    case "vAlign":
//                                                        if ((CellPropNode.GetAttributes()[0].Value ?? "") == "center")
//                                                            thiscell.Focusable = true;
//                                                        else
//                                                            thiscell.Focusable = false;
//                                                        break;

//                                                    case "vmerge":
//                                                    case "vMerge":
//                                                        if (CellPropNode.GetAttributes().Count == 0 || (CellPropNode.GetAttributes()[0].Value ?? "") == "continue")
//                                                        {
//                                                            LastVmergedHeadCells[colno].RowSpan += 1;
//                                                            ((List<string>)thiscell.Tag)[0] = "merged";
//                                                        }
//                                                        else
//                                                        {
//                                                            // restart
//                                                            ((List<string>)thiscell.Tag)[0] = "";
//                                                            LastVmergedHeadCells[colno] = thiscell;
//                                                            LastVmergedHeadCells[colno].RowSpan = 1;
//                                                        }

//                                                        break;

//                                                    case "tcBorders":
//                                                        break;

//                                                    case "tcMar":
//                                                        break;
//                                                }
//                                            }

//                                            break;

//                                        case "p":
//                                            thiscell.Blocks.Add(GetParagraph(CellParNode));
//                                            break;
//                                    }
//                                }

//                                // Add cell to current row
//                                if (((List<string>)thiscell.Tag)[0] != "merged")
//                                {
//                                    newrow.Cells.Add(thiscell);
//                                    if (colno < LastVmergedHeadCells.Count())
//                                    {
//                                        LastVmergedHeadCells[colno] = thiscell;
//                                        LastVmergedHeadCells[colno].RowSpan = 1;
//                                    }
//                                }

//                                colno += 1;
//                                break;
//                        }
//                    }

//                    break;

//            }
//        }

//        return thisTable;

//    }

//}
