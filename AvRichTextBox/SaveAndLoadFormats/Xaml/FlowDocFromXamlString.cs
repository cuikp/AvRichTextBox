using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Diagnostics;
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

   internal static void LoadXamlPackage(string fileName, FlowDocument fdoc)
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
      catch (Exception ex) { throw new FileLoadException("Could not load xaml package", fileName, ex); }


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
            foreach (XmlNode parNode in SectionNode.ChildNodes.OfType<XmlNode>().Where(n => n.Name == "Paragraph"))
            {
               Paragraph newPar = new();

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

                     case "FontFamily":
                        newPar.FontFamily = new FontFamily(xmlatt.Value);
                        break; 

                     case "FontSize":
                        newPar.FontSize = Convert.ToDouble(xmlatt.Value);
                        break; 

                     case "FontWeight":
                        switch(xmlatt.Value)
                        {
                           case "Normal": newPar.FontWeight = FontWeight.Normal; break;
                           case "Bold": newPar.FontWeight = FontWeight.Bold; break;
                        }
                        break; 
                       
                     case "FontStyle":
                        switch(xmlatt.Value)
                        {
                           case "Normal": newPar.FontStyle = FontStyle.Normal; break;
                           case "Italic": newPar.FontStyle = FontStyle.Italic; break;
                        }
                        break;

                     case "Background":
                        /// get background from string
                        break;

                     case "Margin":
                        //convert string to thickness
                        //newPar.Margin = new Avalonia.Thickness();
                        break;

                  }
               }

               //Debug.WriteLine("XmlAttributes:\n" + string.Join("____", parNode.Attributes!.OfType<XmlAttribute>().Select(att => att.Name + "///" + att.Value)));

               foreach (XmlNode inlineNode in parNode.ChildNodes.OfType<XmlNode>())
               {
                  IEditable? newIED = null;
                  switch (inlineNode.Name)
                  {
                     case "Run":
                        EditableRun erun = new(inlineNode.InnerText);
                        //Debug.WriteLine("inlineNode= " + inlineNode.Attributes.Count + " /// " + inlineNode.InnerText);
                        foreach (XmlAttribute att in inlineNode.Attributes!)
                        {
                           switch (att.Name)
                           {
                              case "FontFamily":
                                 //Debug.WriteLine("fontfamily_run = " + erun.FontFamily.ToString());
                                 switch (erun.FontFamily.Name)
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
                                 switch (att.Value)
                                 {
                                    case "Underline": erun.TextDecorations = TextDecorations.Underline; break;
                                    case "Overline": erun.TextDecorations = TextDecorations.Overline; break;
                                    case "Baseline": erun.TextDecorations = TextDecorations.Baseline; break;
                                    case "Strikethrough": erun.TextDecorations = TextDecorations.Strikethrough; break;
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
                                    case "SemiExpanded":erun.FontStretch = FontStretch.SemiExpanded; break;
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
                        newIED = erun;
                        break;

                     case "LineBreak":
                        //EditableRun eLineBreak = new(@"\n");
                        EditableLineBreak eLineBreak = new();
                        newIED = eLineBreak;
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
                              Image img = new ();

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
              fdoc.Blocks.Add(newPar);

            }
         }
      }

   }

   
}


