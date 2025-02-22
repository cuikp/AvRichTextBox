using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace AvRichTextBox;

public partial class FlowDocument
{

   string SectionTextDefault => "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">";
   //string ParagraphTextDefault => "<Paragraph LineHeight=\"18.666666666666668\" FontFamily=\"Times New Roman, ‚l‚r –¾’©\" Margin=\"0,0,0,0\" Padding=\"0,0,0,0\">";

   private string GetDocXaml(bool isXamlPackage)
   {
      XmlDocument xamlDocument = new();
                  
      StringBuilder selXaml = new (SectionTextDefault);

      foreach (Paragraph paragraph in Blocks)
      {

         StringBuilder ParagraphHeader = new ("<Paragraph ");
         ParagraphHeader.Append("FontFamily=\"" + paragraph.FontFamily.Name + "\" FontWeight=\"" + paragraph.FontWeight.ToString() + "\" FontStyle=\"" + paragraph.FontStyle.ToString() + "\" FontSize=\"" + paragraph.FontSize.ToString() + 
          "\" Margin=\"" + paragraph.Margin.ToString() + "\" Background=\"" + paragraph.Background.ToString() + "\">");
         selXaml.Append(ParagraphHeader);

         foreach (IEditable ied in paragraph.Inlines)
         {
            switch (ied.GetType())
            {
               case Type t when t == typeof(EditableRun):

                  EditableRun erun = (EditableRun)ied;
                  if (erun.Text == "\v")
                     selXaml.Append("<LineBreak/>");
                  else
                  {
                     StringBuilder RunHeader = new ("<Run ");
                     RunHeader.Append(GetRunAttributesString(erun));
                     selXaml.Append(RunHeader);
                     selXaml.Append(erun.Text!.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"));
                     selXaml.Append("</Run>");
                  }
                  break;

               case Type t when t == typeof(EditableLineBreak):
                  selXaml.Append("<LineBreak/>");
                  break;

               case Type t when t == typeof(EditableInlineUIContainer):

                  if (isXamlPackage)
                  {
                     EditableInlineUIContainer eIUC = (EditableInlineUIContainer)ied;
                     string InlineUIHeader = "<InlineUIContainer FontFamily=\"" + eIUC.FontFamily.Name + "\" BaselineAlignment=\"" + eIUC.BaselineAlignment.ToString() + "\">";
                     selXaml.Append(InlineUIHeader);

                     if (eIUC.Child.GetType() == typeof(Image))
                     {
                        Image childImage = (Image)eIUC.Child;
                        string ImageHeader =
                            "<Image Stretch=\"Fill\" " +
                            "Width=\"" + childImage.Width +
                            "\" Height=\"" + childImage.Height +
                            "\">";
                        selXaml.Append(ImageHeader);

                        selXaml.Append(
                            "<Image.Source>" +
                            "<BitmapImage UriSource=\"./Image" + eIUC.ImageNo + ".png" + "\" CacheOption=\"OnLoad\" />" +
                            "</Image.Source>"
                            );

                        selXaml.Append("</Image>");
                     }

                     selXaml.Append("</InlineUIContainer>");
                  }

                  break;
            }

         }
         selXaml.Append("</Paragraph>");
      }

      selXaml.Append("</Section>");

      return selXaml.ToString();

   }

   private void ProcessXamlString(string docXamlString)
   {  //Debug.WriteLine("xaml:\n" + docXamlString);

      //FlowDoc.SelectionParagraphs.Clear();
      Blocks.Clear();

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

               XmlAttribute? textAlignAttribute = parNode.Attributes!.OfType<XmlAttribute>().Where(attP => attP.Name == "TextAlignment").FirstOrDefault();
               if (textAlignAttribute != null)
               {
                  switch (textAlignAttribute.Value)
                  {
                     case "Left":
                        newPar.TextAlignment = TextAlignment.Left;
                        break;

                     case "Center":
                        newPar.TextAlignment = TextAlignment.Center;
                        break;
                  }
               }

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
                                 erun.FontFamily = new FontFamily(att.Value);
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
                        EditableRun eLineBreak = new("\v");
                        newIED = eLineBreak;
                        break;

                     case "InlineUIContainer":
                        EditableInlineUIContainer eIUC = new(null!);
                        
                        foreach (XmlAttribute att in inlineNode.Attributes!)
                        {
                           switch (att.Name)
                           {
                              case "FontFamily":
                                 eIUC.FontFamily = new Avalonia.Media.FontFamily(att.Value);
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
                                       img.Stretch = Avalonia.Media.Stretch.Fill; // leave fixed for now 
                                       break;
                                 }
                              }

                              if (controlNode.ChildNodes.Count == 1)
                              {
                                 XmlNode sourceNode = controlNode.ChildNodes[0]!;
                                 if (sourceNode.Name == "Image.Source")
                                 {
                                    if (sourceNode.ChildNodes.Count == 1)
                                    {
                                       XmlNode bitmapNode = sourceNode.ChildNodes[0]!;
                                       if (bitmapNode.Name == "BitmapImage")
                                       {
                                          XmlAttribute? uriSourceAtt = bitmapNode.Attributes?.OfType<XmlAttribute>().Where(batt => batt.Name == "UriSource").FirstOrDefault();
                                          if (uriSourceAtt != null)
                                          {
                                             Match imgNoMatch = Regex.Match(uriSourceAtt.Value, "(?<=Image)[0-9]{1,}");
                                             if (imgNoMatch.Success)
                                             {
                                                int ImageNo = int.Parse(imgNoMatch.Value);
                                                img.Source = consecutiveImageBitmaps[ImageNo - 1];
                                             }
                                          }
                                       }
                                    }
                                 }
                              }

                              eIUC.Child = img;
                           }
                        }

                        newIED = eIUC;

                        break;
                  }

                  if (newIED != null)
                     newPar.Inlines.Add(newIED);
               }

               if (newPar.Inlines.Count == 0) newPar.Inlines.Add(new EditableRun(""));
              Blocks.Add(newPar);

            }
         }
      }

   }


   private string GetRunAttributesString(EditableRun erun)
   {
      StringBuilder attSB = new ();
      attSB.Append("FontFamily=\"" + erun.FontFamily.ToString() + "\"");
      attSB.Append(" FontWeight=\"" + erun.FontWeight.ToString() + "\"");
      attSB.Append(" FontSize=\"" + erun.FontSize.ToString() + "\"");
      attSB.Append(" FontStyle=\"" + erun.FontStyle.ToString() + "\"");
      attSB.Append(" FontStretch=\"" + erun.FontStretch.ToString() + "\"");
      attSB.Append(" BaselineAlignment=\"" + erun.BaselineAlignment.ToString() + "\"");
      if (erun.Foreground != null)
         attSB.Append(" Foreground=\"" + erun.Foreground.ToString() + "\"");
      if (erun.Background != null)
         attSB.Append(" Background=\"" + erun.Background.ToString() + "\"");

      if (erun.TextDecorations != null)
      {
         string textDecString = "";
         if (erun.TextDecorations.Count > 0)
         {
            textDecString = " TextDecorations=\"";
            switch (erun.TextDecorations[0].Location)
            {
               case TextDecorationLocation.Underline:
                  textDecString += "Underline";
                  break;
               case TextDecorationLocation.Baseline:
                  textDecString += "Baseline";
                  break;
               case TextDecorationLocation.Overline:
                  textDecString += "Overline";
                  break;
            }
            textDecString += "\"";
         }
         attSB.Append(textDecString);
      }
      
      //Closing bracket
      attSB.Append(">");

      return attSB.ToString();
   }



}


