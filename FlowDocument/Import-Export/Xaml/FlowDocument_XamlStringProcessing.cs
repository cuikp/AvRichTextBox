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

         StringBuilder ParagraphHeader = new StringBuilder("<Paragraph ");
         ParagraphHeader.Append("FontFamily=\"" + paragraph.FontFamily.Name + "\" FontWeight=\"" + paragraph.FontWeight.ToString() + "\" FontSize=\"" + paragraph.FontSize.ToString() + 
          "\" Margin=\"" + paragraph.Margin.ToString() + "\" Background=\"" + paragraph.Background.ToString() + "\">");
         selXaml.Append(ParagraphHeader);

         foreach (IEditable ied in paragraph.Inlines)
         {
            //xstring RunHeader = "<Run FontFamily=\"" + ied.FontFamily + "\" FontWeight=\"" + ied.FontWeight.ToString() + "\" FontSize=\"" + ied.FontSize.ToString() + "\""
            //    + " Foreground=\"#FF000000\" TextDecorations=\"None\" Background=\"#00000000\">";
            switch (ied.GetType())
            {
               case Type t when t == typeof(EditableRun):

                  EditableRun erun = (EditableRun)ied;
                  if (erun.Text == "\v")
                     selXaml.Append("<LineBreak/>");
                  else
                  {
                     StringBuilder RunHeader = new StringBuilder("<Run ");
                     RunHeader.Append("FontFamily=\"" + erun.FontFamily + "\" FontWeight=\"" + erun.FontWeight.ToString() + "\" FontSize=\"" + erun.FontSize.ToString() + "\">");
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
   {
      //Debug.WriteLine("xaml:\n" + docXamlString);

      //FlowDoc.SelectionParagraphs.Clear();
      Blocks.Clear();


      XmlDocument xamlDocument = new();

      //docXamlString = docXamlString.Replace("\0", "t");

      xamlDocument.LoadXml(docXamlString);
      if (xamlDocument.ChildNodes.Count == 1)
      {
         XmlNode? SectionNode = xamlDocument.ChildNodes[0];
         if (SectionNode!.Name == "Section")
         {
            foreach (XmlNode parNode in SectionNode.ChildNodes.OfType<XmlNode>().Where(n => n.Name == "Paragraph"))
            {
               Paragraph newPar = new();

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
                                 erun.FontFamily = new Avalonia.Media.FontFamily(att.Value);
                                 break;

                              case "FontWeight":
                                 switch (att.Value)
                                 {
                                    case "Normal":
                                       erun.FontWeight = Avalonia.Media.FontWeight.Normal;
                                       break;
                                    case "Bold":
                                       erun.FontWeight = Avalonia.Media.FontWeight.Bold;
                                       break;
                                 }
                                 break;

                              case "FontSize":
                                 erun.FontSize = double.Parse(att.Value);
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
                              Image img = new Image();

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





}


