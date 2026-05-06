using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace AvRichTextBox;

public partial class XamlConversions
{
   
   readonly static List<Bitmap> consecutiveImageBitmaps = [];

   [GeneratedRegex(@"Xaml/Image[0-9]{1,}\.png")]
   public static partial Regex FindXamlImageEntriesRegex();

   [GeneratedRegex(@"<Relationship Type=.*?/xaml/entry.*?/>")]
   private static partial Regex XamlRelationshipEntryRegex();
   
   [GeneratedRegex(@"(?<=Target="").*?(?="")")]
   private static partial Regex XamlTargetEntryRegex();
   
   [GeneratedRegex("(?<=Image)[0-9]{1,}")]
   private static partial Regex ImageNoMatchRegex();

   internal static async Task<bool> LoadXamlPackage(string fileName, FlowDocument fdoc)
   {
      try
      {
         using FileStream fstream = new(fileName, FileMode.Open);
         using ZipArchive zipArchive = new(fstream, ZipArchiveMode.Read);
         string EntryXamlDocumentName = "";
         ZipArchiveEntry? relsEntry = zipArchive.GetEntry("_rels/.rels");
         if (relsEntry != null)
         {
            using Stream s = relsEntry.Open();
            byte[] relsBytes = new byte[(int)relsEntry.Length];
            s.ReadExactly(relsBytes);
            string relString = Encoding.UTF8.GetString(relsBytes);
            //string RelationshipEntryLine = @"<Relationship Type=.*?/xaml/entry.*?/>";
            //Match relLine = Regex.Match(relString, RelationshipEntryLine);

            Match relLine = XamlRelationshipEntryRegex().Match(relString);
            Match m = XamlTargetEntryRegex().Match(relLine.Value);
            EntryXamlDocumentName = m.Value.TrimStart('/');
         }

         //Get all sequentially numbered images for file
         if (EntryXamlDocumentName != "")
         {

            List<ZipArchiveEntry> imageEntries = [.. zipArchive.Entries.Where(ent => FindXamlImageEntriesRegex().IsMatch(ent.FullName))];
            for (int i = 1; i <= imageEntries.Count; i++)
            {
               ZipArchiveEntry? imageEntry = zipArchive.GetEntry($"Xaml/Image{i}.png");
               if (imageEntry != null)
               {
                  try
                  {
                     using Stream s = imageEntry.Open();
                     MemoryStream memStream = new();
                     s.CopyTo(memStream);
                     memStream.Position = 0;
                     consecutiveImageBitmaps.Add(new Bitmap(memStream));
                  }
                  catch { consecutiveImageBitmaps.Add(null!); Debug.WriteLine("png file in package could not be gotten.: " + imageEntry.FullName); }
               }
            }


            //Get the Docxaml data
            ZipArchiveEntry? xamlDocEntry = zipArchive.GetEntry(EntryXamlDocumentName);

            if (xamlDocEntry != null)
            {
               using Stream docStream = xamlDocEntry.Open();
               byte[] xamlDocBytes = new byte[xamlDocEntry.Length];
               int totalBytesRead = 0;
               while (totalBytesRead < xamlDocBytes.Length)
               {
                  int bytesRead = docStream.Read(xamlDocBytes, totalBytesRead, xamlDocBytes.Length - totalBytesRead);
                  if (bytesRead == 0)
                     throw new InvalidOperationException("End of stream reached before finished reading.");
                  totalBytesRead += bytesRead;
               }
               string xamlDocString = Encoding.UTF8.GetString(xamlDocBytes);
               ProcessXamlString(xamlDocString, fdoc);

            }
         }
      }
      catch (Exception ex) { return false; }

      return true;

   }

   internal static void ProcessXamlString(string docXamlString, FlowDocument fdoc)
   {  //Debug.WriteLine("xaml:\n" + docXamlString);

      fdoc.Blocks.Clear();

      XmlDocument xamlDocument = new();

      xamlDocument.LoadXml(docXamlString);
      if (xamlDocument.ChildNodes.Count == 1)
      {
         XmlNode? SectionNode = xamlDocument.ChildNodes[0];
         if (SectionNode!.Name == "Section")
         {
            foreach (XmlNode blockNode in SectionNode.ChildNodes.OfType<XmlNode>())
            {
               switch (blockNode.Name)
               {
                  case "Paragraph":

                     fdoc.Blocks.Add(GetParagraph(blockNode, fdoc));
                     break;

                  case "Table":

                     fdoc.Blocks.Add(GetTable(blockNode, fdoc));
                     break;
               }
            }
         }
      }

   }


   internal static Table GetTable(XmlNode tableNode, FlowDocument fdoc)
   {
      Table newTable = new(fdoc);

      foreach (XmlAttribute xmlatt in tableNode.Attributes!.OfType<XmlAttribute>())
      {
         switch (xmlatt.Name)
         {
            case "CellSpacing":
               break;
            case "Margin":
               newTable.Margin = new();
               break;
            case "Padding":

               break;

            case "TableAlignment":
               switch (xmlatt.Value)
               {
                  case "Center":
                     newTable.TableAlignment = HorizontalAlignment.Center;
                     break;
                  case "Left":
                     newTable.TableAlignment = HorizontalAlignment.Left;
                     break;
                  case "Right":
                     newTable.TableAlignment = HorizontalAlignment.Right;
                     break;
               }
               break;

            case "BorderThickness=":
               newTable.BorderThickness = new();
               break;

            case "BorderBrush":
               newTable.BorderBrush = new SolidColorBrush(Color.Parse(xmlatt.Value));
               break;
         }
      }

      foreach (XmlNode inlineNode in tableNode.ChildNodes.OfType<XmlNode>())
      {
         switch (inlineNode.Name)
         {
            case "Table.Columns":
               foreach (XmlNode coldefNode in inlineNode.ChildNodes.OfType<XmlNode>())
               {
                  if (coldefNode.Name == "TableColumn" && coldefNode.Attributes is XmlAttributeCollection atts)
                  {
                     foreach (XmlAttribute xmlatt in atts.OfType<XmlAttribute>().Where(widthAtt => widthAtt.Name == "Width"))
                     {                        
                        double colWidth = Double.Parse(xmlatt.Value);
                        newTable.ColDefs.Add(new ColumnDefinition(colWidth, GridUnitType.Pixel));
                     }
                  }
               }
               break;

            case "TableRowGroup":

               int[] firstAvailableRow = Enumerable.Repeat(0, newTable.ColDefs.Count).ToArray(); 

               int rowno = 0;
               foreach (XmlNode rowNode in inlineNode.ChildNodes.OfType<XmlNode>().Where(n=> n.Name == "TableRow"))
               {

                  newTable.RowDefs.Add(new RowDefinition());

                  int colno = 0;

                  foreach (XmlNode cellNode in rowNode.ChildNodes.OfType<XmlNode>().Where(n => n.Name == "TableCell"))
                  {
                     Cell newCell = new(newTable)
                     {
                        ColSpan = 1,
                        RowSpan = 1,
                     };

                     if (cellNode.Attributes is XmlAttributeCollection atts)
                     {
                        foreach (XmlAttribute xmlatt in atts.OfType<XmlAttribute>())
                        {
                           switch (xmlatt.Name)
                           {
                              case "ColumnSpan": newCell.ColSpan = Int32.Parse(xmlatt.Value); break; 
                              case "RowSpan": newCell.RowSpan = Int32.Parse(xmlatt.Value); break;
                              case "Padding": newCell.Padding = Thickness.Parse(xmlatt.Value); break;
                              case "BorderThickness": newCell.BorderThickness = Thickness.Parse(xmlatt.Value); break;

                              case "BorderBrush":
                                 if (xmlatt.Value is string bbS)
                                    newCell.BorderBrush = bbS == "" ? Brushes.Black :  new SolidColorBrush(Color.Parse(bbS));
                                 break;

                              case "CellBackground":
                                 if (xmlatt.Value is string cbS)
                                    newCell.CellBackground = cbS == "" ? Brushes.Transparent : new SolidColorBrush(Color.Parse(cbS));
                                 break;
                           }
                        }
                     }

                     //foreach (XmlNode parNode in cellNode.ChildNodes.OfType<XmlNode>().Where(n => n.Name == "Paragraph"))  // for future multiple pars in a cell?
                     if (cellNode.ChildNodes.OfType<XmlNode>().FirstOrDefault(n => n.Name == "Paragraph") is XmlNode cellParNode)
                     {
                        
                        Paragraph newPar = GetParagraph(cellParNode, fdoc);
                        //newPar.IsTableCellBlock = true;
                        //newPar.OwningTable = newTable;

                        VerticalAlignment valign = newPar.VerticalAlignment;
                        newCell.CellContent = newPar;
                        newCell.CellVerticalAlignment = valign;
                     }


                     while (rowno < firstAvailableRow[colno])
                     {
                        colno++;
                     }

                     for (int cspan = 0; cspan < newCell.ColSpan; cspan++)
                     {
                        int lastSpannedCol = colno + cspan;
                        if (lastSpannedCol < firstAvailableRow.Length)
                           firstAvailableRow[lastSpannedCol] = rowno + newCell.RowSpan;
                     }
                                          
                     newCell.RowNo = rowno;
                     newCell.ColNo = colno;
                     newTable.Cells.Add(newCell);
                     colno += newCell.ColSpan;
                  }

                  rowno++;
               }

               break;
         }
      }


      return newTable;

   }


   internal static Paragraph GetParagraph (XmlNode parNode, FlowDocument fdoc)
   {
      Paragraph newPar = new(fdoc);
      //newPar.LineHeight = ;
      foreach (XmlAttribute xmlatt in parNode.Attributes!.OfType<XmlAttribute>())
      {
         switch (xmlatt.Name)
         {
            case "TextAlignment":
               switch (xmlatt.Value)
               {
                  case "Left": newPar.TextAlignment = TextAlignment.Left; break;
                  case "Justify": newPar.TextAlignment = TextAlignment.Justify; break;
                  case "Right": newPar.TextAlignment = TextAlignment.Right; break;
                  case "Center": newPar.TextAlignment = TextAlignment.Center; break;
               }
               break;

            case "VerticalAlignment":
               switch (xmlatt.Value)
               {
                  case "Top": newPar.VerticalAlignment = VerticalAlignment.Top; break;
                  case "Center": newPar.VerticalAlignment = VerticalAlignment.Center; break;
                  case "Bottom": newPar.VerticalAlignment = VerticalAlignment.Bottom; break;
               }
               break;

            case "FontFamily":
               newPar.FontFamily = new FontFamily(xmlatt.Value);
               break;

            case "FontSize":
               newPar.FontSize = Convert.ToDouble(xmlatt.Value);
               break;

            case "FontWeight":
               switch (xmlatt.Value)
               {
                  case "Normal": newPar.FontWeight = FontWeight.Normal; break;
                  case "Bold": newPar.FontWeight = FontWeight.Bold; break;
               }
               break;

            case "FontStyle":
               switch (xmlatt.Value)
               {
                  case "Normal": newPar.FontStyle = FontStyle.Normal; break;
                  case "Italic": newPar.FontStyle = FontStyle.Italic; break;
               }
               break;

            case "Background":
               newPar.Background = new SolidColorBrush(Color.Parse(xmlatt.Value));
               break;

            case "Margin":
               newPar.Margin = Thickness.Parse(xmlatt.Value);
               break;

         }
      }

      //Debug.WriteLine("XmlAttributes:\n" + string.Join("____", tableNode.Attributes!.OfType<XmlAttribute>().Select(att => att.Name + "///" + att.Value)));

      foreach (XmlNode inlineNode in parNode.ChildNodes.OfType<XmlNode>())
      {
         IEditable? newIED = null;
         switch (inlineNode.Name)
         {
            case "Hyperlink":
               EditableHyperlink elink = new();
               foreach (XmlAttribute att in inlineNode.Attributes!)
               {
                  switch (att.Name)
                  {
                     case "NavigateUri":
                        elink.NavigateUri = att.Value;
                        break;
                  }
               }
               foreach (XmlNode runNode in inlineNode.ChildNodes.OfType<XmlNode>())
               {
                  switch (runNode.Name)
                  {
                     case "Run":
                        EditableRun linkRun = GetRun(runNode);
                        FlowDocument.CopyRunPropsToHyperlinkText(linkRun, ref elink);
                        break;
                  }
               }
               newIED = elink;
               break;

            case "Run":
               
               newIED = GetRun(inlineNode);
               break;

            case "LineBreak":

               newIED = new EditableLineBreak();
               break;

            case "InlineUIContainer":
               EditableInlineUIContainer eIUC = new(null!);

               foreach (XmlAttribute att in inlineNode.Attributes!)
               {
                  switch (att.Name)
                  {
                     case "FontFamily":
                        eIUC.FontFamily = new FontFamily(att.Value);
                        break;
                  }
               }

               if (inlineNode.ChildNodes.Count == 1)
               {
                  XmlNode? controlNode = inlineNode.ChildNodes[0];
                  if (controlNode!.Name == "Image")
                  {
                     Image img = new();

                     foreach (XmlAttribute attC in controlNode.Attributes!)
                     {
                        switch (attC.Name)
                        {
                           case "Width":
                              img.Width = double.Parse(attC.Value);
                              break;

                           case "Height":
                              img.Height = double.Parse(attC.Value);
                              break;

                           case "Stretch":
                              img.Stretch = Stretch.Fill; // leave fixed for now 
                              break;
                        }
                     }

                     if (controlNode.ChildNodes.Count == 1 && controlNode.ChildNodes[0] is XmlNode sourceNode && sourceNode.Name == "Image.Source")
                     {
                        if (sourceNode.ChildNodes.Count == 1 && sourceNode.ChildNodes[0] is XmlNode bitmapNode && bitmapNode.Name == "BitmapImage")
                        {
                           if (bitmapNode.Attributes?.OfType<XmlAttribute>().Where(batt => batt.Name == "UriSource").FirstOrDefault() is XmlAttribute uriSourceAtt)
                           {
                              Match imgNoMatch = ImageNoMatchRegex().Match(uriSourceAtt.Value);
                              if (imgNoMatch.Success)
                              {
                                 int ImageNo = int.Parse(imgNoMatch.Value);
                                 img.Source = consecutiveImageBitmaps[ImageNo - 1];
                              }
                           }
                        }
                     }

                     eIUC.Child = img;
                  }
               }

               newIED = eIUC;

               break;

            default:
               Debug.WriteLine("unknown par xmlnode: " + inlineNode.Name);
               break;
         }

         if (newIED != null)
            newPar.Inlines.Add(newIED);
      }

      if (newPar.Inlines.Count == 0) newPar.Inlines.Add(new EditableRun(""));

      return newPar;
   }

   internal static EditableRun GetRun(XmlNode inlineNode)
   {
      EditableRun erun = new (inlineNode.InnerText);

      foreach (XmlAttribute att in inlineNode.Attributes!)
      {
         switch (att.Name)
         {
            case "FontFamily":

               switch (att.Value)
               {
                  case "$Default":
                     //do nothing - inherit paragraph font
                     break;
                  default:
                     erun.FontFamily = new FontFamily(att.Value);
                     break;
               }
               break;

            case "FontWeight":
               switch (att.Value)
               {
                  case "Normal": erun.FontWeight = FontWeight.Normal; break;
                  case "Bold": erun.FontWeight = FontWeight.Bold; break;
                  case "DemiBold": erun.FontWeight = FontWeight.DemiBold; break;
                  case "ExtraBold": erun.FontWeight = FontWeight.ExtraBold; break;
                  case "Light": erun.FontWeight = FontWeight.Light; break;
                  case "Thin": erun.FontWeight = FontWeight.Thin; break;
                  case "Black": erun.FontWeight = FontWeight.Black; break;
                  case "ExtraBlack": erun.FontWeight = FontWeight.ExtraBlack; break;
                  case "UltraLight": erun.FontWeight = FontWeight.UltraLight; break;
                  case "ExtraLight": erun.FontWeight = FontWeight.ExtraLight; break;
                  case "SemiLight": erun.FontWeight = FontWeight.SemiLight; break;
                  case "Heavy": erun.FontWeight = FontWeight.Heavy; break;
               }
               break;

            case "FontSize":
               erun.FontSize = double.Parse(att.Value);
               break;

            case "FontStyle":
               switch (att.Value)
               {
                  case "Normal": erun.FontStyle = FontStyle.Normal; break;
                  case "Italic": erun.FontStyle = FontStyle.Italic; break;
                  case "Oblique": erun.FontStyle = FontStyle.Oblique; break;
               }
               break;

            case "TextDecorations":
               erun.TextDecorations = [];
               foreach (string tDecRaw in att.Value.Split(','))
               {
                  string tDec = tDecRaw.Trim();
                  switch (tDec)
                  {
                     case "Underline": erun.TextDecorations.Add(new() { Location = TextDecorationLocation.Underline }); break;
                     case "Overline": erun.TextDecorations.Add(new () { Location = TextDecorationLocation.Overline }); break;
                     case "Baseline": erun.TextDecorations.Add(new () { Location = TextDecorationLocation.Baseline }); break;
                     case "Strikethrough": erun.TextDecorations.Add(new () { Location = TextDecorationLocation.Strikethrough }); break;
                  }
               }
               break;

            case "Foreground":
               erun.Foreground = new SolidColorBrush(Color.Parse(att.Value));
               break;

            case "Background":
               erun.Background = new SolidColorBrush(Color.Parse(att.Value));
               break;

            case "FontStretch":
               switch (att.Value)
               {
                  case "Normal": erun.FontStretch = FontStretch.Normal; break;
                  case "Condensed": erun.FontStretch = FontStretch.Condensed; break;
                  case "SemiCondensed": erun.FontStretch = FontStretch.SemiCondensed; break;
                  case "ExtraCondensed": erun.FontStretch = FontStretch.ExtraCondensed; break;
                  case "UltraCondensed": erun.FontStretch = FontStretch.UltraCondensed; break;
                  case "Expanded": erun.FontStretch = FontStretch.Expanded; break;
                  case "SemiExpanded": erun.FontStretch = FontStretch.SemiExpanded; break;
                  case "ExtraExpanded": erun.FontStretch = FontStretch.ExtraExpanded; break;
                  case "UltraExpanded": erun.FontStretch = FontStretch.UltraExpanded; break;
               }
               break;

            case "BaselineAlignment":
               switch (att.Value)
               {
                  case "Baseline": erun.BaselineAlignment = BaselineAlignment.Baseline; break;
                  case "Bottom": erun.BaselineAlignment = BaselineAlignment.Bottom; break;
                  case "Top": erun.BaselineAlignment = BaselineAlignment.Top; break;
                  case "Center": erun.BaselineAlignment = BaselineAlignment.Center; break;
                  case "TextTop": erun.BaselineAlignment = BaselineAlignment.TextTop; break;
                  case "TextBottom": erun.BaselineAlignment = BaselineAlignment.TextBottom; break;
                  case "Superscript": erun.BaselineAlignment = BaselineAlignment.Superscript; break;
                  case "Subscript": erun.BaselineAlignment = BaselineAlignment.Subscript; break;
               }

               break;
         }
      }

      return erun;
   }   

}


