using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DocumentFormat.OpenXml;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

internal static partial class WordConversions
{

   internal static Table GetTable(DocumentFormat.OpenXml.Wordprocessing.Table wordTable, FlowDocument fdoc)
   {

      Table newTable = new(fdoc)
      {
         BorderThickness = new Thickness(1),  // initial default
         BorderBrush = Brushes.Black,
         Margin = new Thickness(0, 0, 0, 0) // just in case
      };

      //newTable.Padding = new Thickness(0, 0, 0, 0);
      //newTable.CellSpacing = 0;  // default is "2"

      var ColumnWidths = new List<double>();
 

      List<Cell> lastVMergedHeadCells = [];

      newTable.BorderBrush = Brushes.Black;
      newTable.BorderThickness = new Thickness(1);

      double tableHeight = 0;
      double thisRowHeight = 10;

      int rowno = 0;

      foreach (OpenXmlElement telm in wordTable.ChildElements)
      {
         switch (telm.LocalName)
         {
            case "tblPr":

               foreach (OpenXmlElement tabPropelm in telm.ChildElements)
               {
                  switch (tabPropelm.LocalName)
                  {
                     case "jc":
                        switch (tabPropelm.GetAttributes()[0].Value)
                        {
                           case "left": { newTable.TableAlignment = HorizontalAlignment.Left; break; }
                           case "right": { newTable.TableAlignment = HorizontalAlignment.Right; break; }
                           case "center": { newTable.TableAlignment = HorizontalAlignment.Center; break; }
                        }
                     break;
                  }
               }

               break;

            case "tblGrid":

               foreach (OpenXmlElement gridelm in telm.ChildElements)
               {
                  switch (gridelm.LocalName)
                  {
                     case "gridCol":
                        double colWidth = TwipToPix(Convert.ToDouble(gridelm.GetAttributes()[0].Value));
                        var newtabcol = new ColumnDefinition(colWidth, GridUnitType.Pixel);
                        newTable.ColDefs.Add(newtabcol);
                        lastVMergedHeadCells.Add(null!);
                        break;
                  }
               }

               break;

            case "tr":

               int colno = 0;

               foreach (OpenXmlElement cn in telm.ChildElements)
               {
                  switch (cn.LocalName)
                  {

                     case "trPr":

                        foreach (OpenXmlElement properties in cn.ChildElements)
                        {
                           if (properties.LocalName == "trHeight")
                           {
                              foreach (OpenXmlAttribute xmlatt in properties.GetAttributes())
                              {
                                 //Debug.WriteLine("attLocalName = " + xmlatt.LocalName);
                                 if (xmlatt.LocalName == "val")
                                 {

                                    if (Double.Parse(xmlatt.Value ?? "") is double height)
                                    {
                                       thisRowHeight = TwipToPix(height);
                                       tableHeight += thisRowHeight;
                                    }
                                 }
                              }
                           }
                        }
                        break;

                     case "tc":

                        var newCell = GetCell(cn, ref colno, ref rowno, ref lastVMergedHeadCells, fdoc, newTable);

                        // Add cell to current row
                        if (!newCell.vmerged)
                        {
                           newTable.Cells.Add(newCell);
                           lastVMergedHeadCells[colno] = newCell;
                        }

                        colno += 1;
                        break;

                     default:
                        //Debug.WriteLine("unknown cn.LocalName = " + cn.LocalName);
                        break;
                  }
               }

               newTable.RowDefs.Add(new RowDefinition(thisRowHeight, GridUnitType.Pixel));

               rowno++;

               break;

            default:
               //Debug.WriteLine("unknown gridelm.LocalName = " + telm.LocalName);
               break;

         }
      }

      newTable.Height = tableHeight;

      return newTable;

   }

   private static Cell GetCell(OpenXmlElement cn, ref int colno, ref int rowno, ref List<Cell> lastVMergedHeadCells, FlowDocument fdoc, Table table)
   {
      var newCell = new Cell(table) { ColNo = colno, RowNo = rowno, ColSpan = 1, RowSpan = 1, BorderBrush = Brushes.Black, BorderThickness = new(1) };

      foreach (OpenXmlElement CellParNode in cn.ChildElements)
      {
         switch (CellParNode.LocalName)
         {
            case "tcPr":
               // Get cell properties
               foreach (OpenXmlElement CellPropNode in CellParNode.ChildElements)
               {
                  switch (CellPropNode.LocalName)
                  {
                     case "tcW":
                        break;

                     case "gridSpan":
                        newCell.ColSpan = Convert.ToInt32(CellPropNode.GetAttributes()[0].Value);
                        colno += newCell.ColSpan - 1;
                        break;

                     case "vAlign":

                        foreach (OpenXmlAttribute xmlatt in CellPropNode.GetAttributes())
                        {
                           if (xmlatt.LocalName == "val" && xmlatt.Value is string alignType)
                           {
                              switch (alignType)
                              {
                                 case "top":
                                    newCell.CellVerticalAlignment = VerticalAlignment.Top;
                                    break;

                                 case "center":
                                    newCell.CellVerticalAlignment = VerticalAlignment.Center;
                                    break;
                              }
                           }
                        }

                        break;

                     case "vmerge":
                     case "vMerge":
                        foreach (OpenXmlAttribute xmlatt in CellPropNode.GetAttributes())
                        {
                           switch (xmlatt.Value)
                           {
                              case "continue":
                                 newCell.vmerged = true;
                                 lastVMergedHeadCells[colno].RowSpan += 1;
                                 break;

                              case "restart":
                                 lastVMergedHeadCells[colno] = newCell;
                                 break;
                           }
                        }

                        break;

                     case "tcBorders":
                        double bordL = 1;
                        double bordT = 1;
                        double bordR = 1;
                        double bordB = 1;

                        foreach (OpenXmlElement elem in CellPropNode.Elements())
                        {
                           switch (elem.XName.LocalName)
                           {
                              case "left": // multiple colored borders not supported - left only
                                 foreach (OpenXmlAttribute xmatt in elem.GetAttributes())
                                 {
                                    switch (xmatt.LocalName)
                                    {
                                       case "color":  // only one color for all border sides supported 
                                          if (xmatt.Value is string s)
                                             newCell.BorderBrush = FromOpenXmlColor(s);
                                          break;

                                       case "sz":
                                          if (xmatt.Value is string szString)
                                             bordL = TwipToPix(Convert.ToDouble(szString));
                                          break;
                                    }
                                 }
                                 break;

                              case "top":
                                 foreach (OpenXmlAttribute xmatt in elem.GetAttributes())
                                 {
                                    switch (xmatt.LocalName)
                                    {
                                       case "sz":
                                          if (xmatt.Value is string szString)
                                             bordT = TwipToPix(Convert.ToDouble(szString));
                                          break;
                                    }
                                 }
                                 break;

                              case "right":
                                 foreach (OpenXmlAttribute xmatt in elem.GetAttributes())
                                 {
                                    switch (xmatt.LocalName)
                                    {
                                       case "sz":
                                          if (xmatt.Value is string szString)
                                             bordR = TwipToPix(Convert.ToDouble(szString));
                                          break;
                                    }
                                 }
                                 break;

                              case "bottom":
                                 foreach (OpenXmlAttribute xmatt in elem.GetAttributes())
                                 {
                                    switch (xmatt.LocalName)
                                    {
                                       case "sz":
                                          if (xmatt.Value is string szString)
                                             bordB = TwipToPix(Convert.ToDouble(szString));
                                          break;
                                    }
                                 }
                                 break;
                           }
                        }

                        newCell.BorderThickness = new(bordL, bordT, bordR, bordB);

                        break;

                     case "tcMar":
                        double padL = 1;
                        double padT = 1;
                        double padR = 1;
                        double padB = 1;
                        foreach (OpenXmlElement elem in CellPropNode.Elements())
                        {
                           switch (elem.XName.LocalName)
                           {
                              case "left": // multiple colored borders not supported - left only
                                 foreach (OpenXmlAttribute xmatt in elem.GetAttributes())
                                 {
                                    switch (xmatt.LocalName)
                                    {
                                       case "w":
                                          if (xmatt.Value is string padString)
                                             padL = TwipToPix(Convert.ToDouble(padString));
                                          break;
                                    }
                                 }
                                 break;

                              case "top":
                                 foreach (OpenXmlAttribute xmatt in elem.GetAttributes())
                                 {
                                    switch (xmatt.LocalName)
                                    {
                                       case "w":
                                          if (xmatt.Value is string padString)
                                             padT = TwipToPix(Convert.ToDouble(padString));
                                          break;
                                    }
                                 }
                                 break;

                              case "right":
                                 foreach (OpenXmlAttribute xmatt in elem.GetAttributes())
                                 {
                                    switch (xmatt.LocalName)
                                    {
                                       case "w":
                                          if (xmatt.Value is string padString)
                                             padR = TwipToPix(Convert.ToDouble(padString));
                                          break;
                                    }
                                 }
                                 break;

                              case "bottom":
                                 foreach (OpenXmlAttribute xmatt in elem.GetAttributes())
                                 {
                                    switch (xmatt.LocalName)
                                    {
                                       case "w":
                                          if (xmatt.Value is string padString)
                                             padB = TwipToPix(Convert.ToDouble(padString));
                                          break;
                                    }
                                 }
                                 break;
                           }

                           newCell.Padding = new(padL, padT, padR, padB);
                        }
                        break;

                     case "shd":

                        foreach (OpenXmlAttribute xmatt in CellPropNode.GetAttributes())
                        {
                           switch (xmatt.LocalName)
                           {
                              case "color":
                                 //Debug.WriteLine("color = " + xmatt.Value);
                                 break;

                              case "fill":
                                 if (xmatt.Value is string s)
                                    newCell.CellBackground = FromOpenXmlColor(s);
                                 break;
                           }
                        }
                        break;
                  }
               }

               break;

            case "p":
               Paragraph p = GetParagraph(CellParNode, fdoc);
               newCell.CellContent = p;
               break;
         }
      }

      return newCell;

   }

}
