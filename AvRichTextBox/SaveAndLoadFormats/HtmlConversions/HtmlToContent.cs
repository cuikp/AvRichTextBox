using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using HtmlAgilityPack;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AvRichTextBox;

internal static partial class HtmlConversions
{
   internal static void GetFlowDocumentFromHtml(HtmlDocument hdoc, FlowDocument fdoc)
   {

      try
      {
         HtmlNode? bodyNode = hdoc.DocumentNode.SelectSingleNode("//body");
         if (bodyNode == null)
         {
            Debug.WriteLine("No <body> found.");
            return;
         }

         // Get PagePadding
         foreach (KeyValuePair<string, string> kvp in ParseStyleAttribute(bodyNode.GetAttributeValue("style", "")))
         {
            switch (kvp.Key)
            {
               case "padding":
                  var paddings = kvp.Value.Split(' ').Select(val => int.TryParse(val.Replace("px", ""), out var px) ? px : 0).ToList();
                  if (paddings.Count == 4)
                     fdoc.PagePadding = new Avalonia.Thickness(paddings[3], paddings[0], paddings[1], paddings[2]);
                  break;
            }
         }

         foreach (HtmlNode parNode in bodyNode.ChildNodes.Where(cn => cn.Name == "p"))
         {
            Paragraph p = new();

            foreach (HtmlNode inlineNode in parNode.ChildNodes.Where(cn => cn.NodeType is HtmlNodeType.Element or HtmlNodeType.Text))
            {
               switch (inlineNode.Name)
               {
                  case "span":
                     EditableRun erun = new() { Text = inlineNode.InnerText };

                     foreach (KeyValuePair<string, string> kvp in ParseStyleAttribute(inlineNode.GetAttributeValue("style", "")))
                     {
                        switch (kvp.Key)
                        {
                           case "font-weight":

                              erun.FontWeight = kvp.Value switch
                              {
                                 "bold" => FontWeight.Bold,
                                 "normal" => FontWeight.Normal,
                                 _ => FontWeight.Normal
                              };

                              break;

                           case "font-style":
                              erun.FontStyle = kvp.Value == "italic" ? FontStyle.Italic : FontStyle.Normal;
                              break;

                           case "font-family":
                              //Debug.WriteLine("fontfam = " + kvp.Value);
                              var fontName = kvp.Value;
                              if (fontName.StartsWith("compositefont:", StringComparison.OrdinalIgnoreCase))
                                 fontName = fontName["compositefont:".Length..];

                              var hashIndex = fontName.IndexOf('#');
                              if (hashIndex >= 0)
                                 fontName = fontName[(hashIndex + 1)..];

                              erun.FontFamily = new FontFamily(fontName.Trim());

                              break;

                           case "font-size":
                              if (double.TryParse(kvp.Value.Replace("px", ""), out var size))
                                 erun.FontSize = size;
                              break;

                           case "color":
                              IBrush? cBrush = ParseCssColor(kvp.Value);
                              if (cBrush != null)
                                 erun.Foreground = (SolidColorBrush)cBrush;
                              break;

                           case "background-color":
                              IBrush? bkBrush = ParseCssColor(kvp.Value);
                              if (bkBrush != null)
                                 erun.Background = (SolidColorBrush)bkBrush;
                              break;
                        }
                     }

                     p.Inlines.Add(erun);
                     break;

                  case "br":
                     p.Inlines.Add(new EditableLineBreak());
                     break;
                  
                  case "img":

                     var src = inlineNode.GetAttributeValue("src", null!);
                     if (!string.IsNullOrEmpty(src) && src.StartsWith("data:image"))
                     {
                        var base64Index = src.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                        if (base64Index >= 0)
                        {
                           var base64 = src[(base64Index + 7)..];

                           byte[] imageBytes = Convert.FromBase64String(base64);
                           using var ms = new MemoryStream(imageBytes);
                           var bitmap = new Bitmap(ms);

                           var img = new Image
                           {
                              Source = bitmap,
                              IsVisible = true,
                           };

                           if (inlineNode.GetAttributeValue("width", null!) is string w && double.TryParse(w, out var width))
                              img.Width = width;

                           if (inlineNode.GetAttributeValue("height", null!) is string h && double.TryParse(h, out var height))
                              img.Height = height;

                           p.Inlines.Add(new EditableInlineUIContainer(img));
                        }
                     }

                     break;
               }

            }

            foreach (KeyValuePair<string, string> kvp in ParseStyleAttribute(parNode.GetAttributeValue("style", "")))
            {
               switch (kvp.Key)
               {
                  case "line-height":
                                                               

                     if (p.Inlines.Count > 0)
                     {
                        double lineHeight = Double.TryParse(kvp.Value.Replace("px", ""), out var px) ? px : 0;
                        double maxInlineHeight = p.Inlines.Max(ilh => ilh.InlineHeight);
                        p.LineSpacing = LineHeightToLineSpacing(lineHeight, maxInlineHeight);
                     }
                     //p.LineHeight = Double.TryParse(kvp.Value.Replace("px", ""), out var px) ? px : 0;
                     break;

                  case "text-align":

                     p.TextAlignment = kvp.Value switch
                     {
                        "center" => TextAlignment.Center,
                        "right" => TextAlignment.Right,
                        "left" => TextAlignment.Left,
                        "justify" => TextAlignment.Justify,
                        _ => TextAlignment.Left
                     };
                     break;

                  case "margin":
                     var margins = kvp.Value.Split(' ').Select(val => int.TryParse(val.Replace("px", ""), out var px) ? px : 0).ToList();
                     if (margins.Count == 4)
                        p.Margin = new Avalonia.Thickness(margins[3], margins[0], margins[1], margins[2]);
                     break;

                  case "background-color":
                     IBrush? bBrush = ParseCssColor(kvp.Value);
                     if (bBrush != null)
                        p.Background = (SolidColorBrush)bBrush;
                     break;

                  case "border-color":
                     IBrush? bcBrush = ParseCssColor(kvp.Value);
                     if (bcBrush != null)
                        p.BorderBrush = (SolidColorBrush)bcBrush;
                     break;

                  case "border-width":
                     var bwidths = kvp.Value.Split(' ').Select(val => int.TryParse(val.Replace("px", ""), out var px) ? px : 0).ToList();
                     if (bwidths.Count == 4)
                        p.BorderThickness = new Avalonia.Thickness(bwidths[3], bwidths[0], bwidths[1], bwidths[2]);
                     break;

               }
            }



            fdoc.Blocks.Add(p);
         }


      }
      catch (Exception ex) { Debug.WriteLine($"Error getting flow doc: \n{ex.Message}"); }

   }


   private static Dictionary<string, string> ParseStyleAttribute(string? style)
   {
      var dict = new Dictionary<string, string>();
      if (string.IsNullOrWhiteSpace(style))
         return dict;

      var parts = style.Split(';', StringSplitOptions.RemoveEmptyEntries);
      foreach (var part in parts)
      {
         var kv = part.Split(':', 2);
         if (kv.Length == 2)
            dict[kv[0].Trim().ToLower()] = kv[1].Trim();
      }
      return dict;
   }

   private static IBrush? ParseCssColor(string cssColor)
   {
      try
      {
         var converter = new BrushConverter();
         var brush = converter.ConvertFromString(cssColor);

         if (brush is ISolidColorBrush solid)
            return new SolidColorBrush(solid.Color);

         return null;
      }
      catch { return null; }
   }

   private static double LineHeightToLineSpacing (double lineHeight, double maxFontSize)
   {
      return lineHeight - maxFontSize * 1.25;
      
   }

}
