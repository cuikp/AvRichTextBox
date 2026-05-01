using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using HtmlAgilityPack;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

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

         foreach (HtmlNode childNode in bodyNode.ChildNodes)
         {
            ProcessRecursive(childNode, fdoc);
            switch (childNode.Name)
            {
               case "p":
                  Paragraph p = GetParagraphFromNode(childNode, fdoc);
                  fdoc.Blocks.Add(p);
                  break;

               case "table":
                  Table t = GetTableFromNode(childNode, fdoc);
                  fdoc.Blocks.Add(t);
                  break;
            
            }
         }

      }
      catch (Exception ex) { Debug.WriteLine($"Error getting flow doc: \n{ex.Message}"); }

   }

   static void ProcessRecursive(HtmlNode node, FlowDocument fdoc)
   {
      foreach (var child in node.ChildNodes)
      {
         switch (child.Name.ToLowerInvariant())
         {
            case "p":
               Paragraph p = GetParagraphFromNode(child, fdoc);
               fdoc.Blocks.Add(p);
               break;
            case "table":
               Table t = GetTableFromNode(child, fdoc);
               fdoc.Blocks.Add(t);
               break;
            case "div":
            case "article":
            case "section":
            case "span":
               ProcessRecursive(child, fdoc);
               break;

            case "br":  //ignore outside of paragraph
               break;

            case "script":
            case "style":
            case "noscript":
               break;

            default:
               ProcessRecursive(child, fdoc);
               break;
         }
      }
   }

   private static Table GetTableFromNode(HtmlNode tableNode, FlowDocument fdoc)
   {
      Table newTable = new(fdoc);

      int noCols = 0;
      var colNodes = tableNode.SelectNodes("./colgroup/col");
      if (colNodes != null && colNodes.Count > 0)
         noCols = colNodes.Count;


      double tableWidthPix = 100;
      double margL = 0;
      double margR = 0;
      double marg = 0;


      Dictionary<string, string> parsedTableStyles = ParseStyleAttribute(tableNode.GetAttributeValue("style", ""));

      ISolidColorBrush tableBorderBrush = Brushes.Black;
      Thickness tableBorderThickness = new(1);
      GetBordersFromCssStyle(parsedTableStyles, ref tableBorderBrush, ref tableBorderThickness);
      newTable.BorderBrush = tableBorderBrush;
      newTable.BorderThickness = tableBorderThickness;

      if (parsedTableStyles.TryGetValue("width", out var widthString))
         tableWidthPix = Double.Parse(widthString.Replace("px", ""));

      HorizontalAlignment tableHorizAlignment = HorizontalAlignment.Left;
      GetAlignmentFromCssStyle(parsedTableStyles, ref tableHorizAlignment);
      newTable.TableAlignment = tableHorizAlignment;

      newTable.Width = tableWidthPix;

      double colWidth = tableWidthPix / noCols;
      for (int i = 0; i < noCols; i++)
         newTable.ColDefs.Add(new ColumnDefinition(colWidth, GridUnitType.Pixel));

      var trNodes = tableNode.SelectNodes("./tr|./tbody/tr|./thead/tr|./tfoot/tr")?.ToList() ?? [];

      for (int i = 0; i < trNodes.Count; i++)
         newTable.RowDefs.Add(new RowDefinition());

      int[] nextAvailableRows = new int[noCols];

      for (int rowNo = 0; rowNo < trNodes.Count; rowNo++)
      {

         HtmlNode tr = trNodes[rowNo];
         var cellNodes = tr.SelectNodes("./td|./th")?.ToList() ?? [];

         int colNo = 0;
         int colSpan = 1;
         int rowSpan = 1;

         foreach (var td in cellNodes)
         {
            while (colNo < noCols && rowNo < nextAvailableRows[colNo])
               colNo++;

            foreach (HtmlAttribute att in td.Attributes)
            {
               if (att.Name == "colspan")
                  colSpan = Math.Max(1, Int32.Parse(att.Value));
               if (att.Name == "rowspan")
                  rowSpan = Math.Max(1, Int32.Parse(att.Value));
            }


            var newCell = new Cell(newTable)
            {
               RowNo = rowNo,
               ColNo = colNo,
               ColSpan = colSpan,
               RowSpan = rowSpan,
               BorderThickness = new(1),
               BorderBrush = Brushes.Black
            };


            Dictionary<string, string> parsedCellStyles = ParseStyleAttribute(td.GetAttributeValue("style", ""));

            ISolidColorBrush cellBorderBrush = Brushes.Black;
            Thickness cellBorderThickness = new(1);

            GetBordersFromCssStyle(parsedCellStyles, ref cellBorderBrush, ref cellBorderThickness);
            newCell.BorderBrush = cellBorderBrush;
            newCell.BorderThickness = cellBorderThickness;


            if (parsedCellStyles.TryGetValue("background-color", out var backgroundColorString))
               if (ParseCssColor(backgroundColorString) is ISolidColorBrush bgc)
                  newCell.CellBackground = bgc;


            if (parsedCellStyles.TryGetValue("padding", out var paddingString))
            {
               if (paddingString.Contains(' '))
               {
                  var paddings = paddingString.Split(' ').Select(val => int.TryParse(val.Replace("px", ""), out var px) ? px : 0).ToList();
                  if (paddings.Count == 4)
                     newCell.Padding = new Thickness(paddings[3], paddings[0], paddings[1], paddings[2]);
               }
               else
               {
                  if (int.Parse(paddingString.Replace("px", "")) is int padding)
                     newCell.Padding = new Thickness(padding);
               }
            }


            if (parsedCellStyles.TryGetValue("vertical-align", out var cellVerticalAlign))
               switch (cellVerticalAlign)
               {
                  case "top": newCell.CellVerticalAlignment = VerticalAlignment.Top; break;
                  case "center": newCell.CellVerticalAlignment = VerticalAlignment.Center; break;
                  case "bottom": newCell.CellVerticalAlignment = VerticalAlignment.Bottom; break;
               }


            HtmlNode? contentNode = td.ChildNodes.FirstOrDefault(n => n.NodeType == HtmlNodeType.Element);

            if (contentNode != null)
            {
               Paragraph p = GetParagraphFromNode(contentNode, fdoc);
               newCell.CellContent = p;
            }
            else
               newCell.CellContent = new Paragraph(fdoc);

            newTable.Cells.Add(newCell);

            for (int cs = colNo; cs < colNo + colSpan; cs++)
               nextAvailableRows[cs] += rowSpan;

            colNo += colSpan;
         }

      }


      return newTable;

   }

   private static void GetBordersFromCssStyle(Dictionary<string, string> parsedStyles, ref ISolidColorBrush cellBorderBrush, ref Thickness cellBorderThickness)
   {
      foreach (KeyValuePair<string, string> kvp in parsedStyles)
      {
         switch (kvp.Key.Trim().ToLowerInvariant())
         {
            case "border":
               var parsed = ParseBorderShorthand(kvp.Value);
               if (parsed.HasValue)
               {
                  var (thickness, brush) = parsed.Value;

                  if (thickness.HasValue)
                     cellBorderThickness = thickness.Value;

                  if (brush != null)
                     cellBorderBrush = brush;
               }
               break;

            case "border-width":
               cellBorderThickness = GetBorderThickness(kvp.Value);
               break;

            case "border-color":
               if (ParseCssColor(kvp.Value) is SolidColorBrush scb)
                  cellBorderBrush = scb;
               break;
         }

      }
   }

   private static void GetAlignmentFromCssStyle(Dictionary<string, string> styles, ref HorizontalAlignment tableHorizAlignment)
   {
      styles.TryGetValue("margin-left", out var ml);
      styles.TryGetValue("margin-right", out var mr);

      if (styles.TryGetValue("margin", out var marginVal))
      {
         var parts = marginVal.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim().ToLowerInvariant()).ToArray();

         if (parts.Length == 1)
         {
            ml ??= parts[0];
            mr ??= parts[0];
         }
         else if (parts.Length == 2)
         {
            ml ??= parts[1];
            mr ??= parts[1];
         }
         else if (parts.Length == 3)
         {
            ml ??= parts[1];
            mr ??= parts[1];
         }
         else if (parts.Length >= 4)
         {
            mr ??= parts[1];
            ml ??= parts[3];
         }
      }

      ml = ml?.Trim().ToLowerInvariant();
      mr = mr?.Trim().ToLowerInvariant();

      bool leftAuto = string.Equals(ml, "auto", StringComparison.OrdinalIgnoreCase);
      bool rightAuto = string.Equals(mr, "auto", StringComparison.OrdinalIgnoreCase);


      if (leftAuto && rightAuto)
         tableHorizAlignment = HorizontalAlignment.Center;
      else if (leftAuto && !rightAuto)
         tableHorizAlignment = HorizontalAlignment.Right;
      else
         tableHorizAlignment = HorizontalAlignment.Left;
   }


   /// <summary>
   /// Holds accumulated inline formatting state while recursing through nested HTML elements.
   /// </summary>
   private class InlineFormatting
   {
      public FontWeight FontWeight { get; set; } = FontWeight.Normal;
      public FontStyle FontStyle { get; set; } = FontStyle.Normal;
      public TextDecorationCollection? TextDecorations { get; set; }
      public BaselineAlignment BaselineAlignment { get; set; } = BaselineAlignment.Baseline;
      public FontFamily? FontFamily { get; set; }
      public double? FontSize { get; set; }
      public SolidColorBrush? Foreground { get; set; }
      public SolidColorBrush? Background { get; set; }

      public InlineFormatting Clone()
      {
         return new InlineFormatting
         {
            FontWeight = FontWeight,
            FontStyle = FontStyle,
            TextDecorations = TextDecorations,
            BaselineAlignment = BaselineAlignment,
            FontFamily = FontFamily,
            FontSize = FontSize,
            Foreground = Foreground,
            Background = Background,
         };
      }

      public void ApplyTo(EditableRun run)
      {
         run.FontWeight = FontWeight;
         run.FontStyle = FontStyle;
         if (TextDecorations != null)
            run.TextDecorations = TextDecorations;
         run.BaselineAlignment = BaselineAlignment;
         if (FontFamily != null)
            run.FontFamily = FontFamily;
         if (FontSize.HasValue)
            run.FontSize = FontSize.Value;
         if (Foreground != null)
            run.Foreground = Foreground;
         if (Background != null)
            run.Background = Background;
      }
   }

   /// <summary>
   /// Recursively processes inline HTML nodes, accumulating formatting from nested tags
   /// like <b>, <i>, <u>, <sup>, <sub>, <span>, etc.
   /// </summary>
   private static void AddInlinesFromNode(HtmlNode parentNode, Paragraph par, InlineFormatting formatting)
   {
      foreach (HtmlNode node in parentNode.ChildNodes.Where(cn => cn.NodeType is HtmlNodeType.Element or HtmlNodeType.Text))
      {
         switch (node.Name)
         {
            case "#text":
               string textContent = WebUtility.HtmlDecode(node.InnerText);
               if (!string.IsNullOrEmpty(textContent))
               {
                  //EditableRun textRun = new() { Text = textContent.Trim(['\r', '\n', '\v']) };
                  EditableRun textRun = new() { Text = Regex.Replace(textContent, @"\s+", " ") };
                  formatting.ApplyTo(textRun);
                  par.Inlines.Add(textRun);
               }
               break;

            case "br":
               par.Inlines.Add(new EditableLineBreak());
               break;

            case "img":
               var src = node.GetAttributeValue("src", null!);
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

                     if (node.GetAttributeValue("width", null!) is string w && double.TryParse(w, out var width))
                        img.Width = width;

                     if (node.GetAttributeValue("height", null!) is string h && double.TryParse(h, out var height))
                        img.Height = height;

                     par.Inlines.Add(new EditableInlineUIContainer(img));
                  }
               }
               break;

            case "span":
               var spanFormatting = formatting.Clone();
               ApplyStyleToFormatting(node.GetAttributeValue("style", ""), spanFormatting);
               AddInlinesFromNode(node, par, spanFormatting);
               break;

            case "b":
            case "strong":
               var boldFormatting = formatting.Clone();
               boldFormatting.FontWeight = FontWeight.Bold;
               ApplyStyleToFormatting(node.GetAttributeValue("style", ""), boldFormatting);
               AddInlinesFromNode(node, par, boldFormatting);
               break;

            case "i":
            case "em":
               var italicFormatting = formatting.Clone();
               italicFormatting.FontStyle = FontStyle.Italic;
               ApplyStyleToFormatting(node.GetAttributeValue("style", ""), italicFormatting);
               AddInlinesFromNode(node, par, italicFormatting);
               break;

            case "u":
               var underlineFormatting = formatting.Clone();
               underlineFormatting.TextDecorations = Avalonia.Media.TextDecorations.Underline;
               ApplyStyleToFormatting(node.GetAttributeValue("style", ""), underlineFormatting);
               AddInlinesFromNode(node, par, underlineFormatting);
               break;

            case "s":
            case "strike":
            case "del":
               var strikeFormatting = formatting.Clone();
               strikeFormatting.TextDecorations = Avalonia.Media.TextDecorations.Strikethrough;
               ApplyStyleToFormatting(node.GetAttributeValue("style", ""), strikeFormatting);
               AddInlinesFromNode(node, par, strikeFormatting);
               break;

            case "sup":
               var supFormatting = formatting.Clone();
               supFormatting.BaselineAlignment = BaselineAlignment.Superscript;
               ApplyStyleToFormatting(node.GetAttributeValue("style", ""), supFormatting);
               AddInlinesFromNode(node, par, supFormatting);
               break;

            case "sub":
               var subFormatting = formatting.Clone();
               subFormatting.BaselineAlignment = BaselineAlignment.Subscript;
               ApplyStyleToFormatting(node.GetAttributeValue("style", ""), subFormatting);
               AddInlinesFromNode(node, par, subFormatting);
               break;

            default:
               // For any unrecognized inline element, recurse into its children
               // preserving current formatting
               AddInlinesFromNode(node, par, formatting);
               break;
         }
      }
   }

   /// <summary>
   /// Applies CSS style properties to an InlineFormatting instance.
   /// </summary>
   private static void ApplyStyleToFormatting(string style, InlineFormatting formatting)
   {
      if (string.IsNullOrEmpty(style))
         return;

      foreach (KeyValuePair<string, string> kvp in ParseStyleAttribute(style))
      {
         switch (kvp.Key)
         {
            case "font-weight":
               formatting.FontWeight = kvp.Value switch
               {
                  "bold" => FontWeight.Bold,
                  "normal" => FontWeight.Normal,
                  _ => FontWeight.Normal
               };
               break;

            case "font-style":
               formatting.FontStyle = kvp.Value == "italic" ? FontStyle.Italic : FontStyle.Normal;
               break;

            case "font-family":
               var fontName = kvp.Value;
               if (fontName.StartsWith("compositefont:", StringComparison.OrdinalIgnoreCase))
                  fontName = fontName["compositefont:".Length..];

               var hashIndex = fontName.IndexOf('#');
               if (hashIndex >= 0)
                  fontName = fontName[(hashIndex + 1)..];

               formatting.FontFamily = new FontFamily(fontName.Trim());
               break;

            case "font-size":
               if (double.TryParse(kvp.Value.Replace("px", ""), out var size))
                  formatting.FontSize = size;
               break;

            case "color":
               if (ParseCssColor(kvp.Value) is SolidColorBrush foreSCB)
                  formatting.Foreground = foreSCB;
               break;

            case "background-color":
               if (ParseCssColor(kvp.Value) is SolidColorBrush backSCB)
                  formatting.Background = backSCB;
               break;

            case "vertical-align":
               formatting.BaselineAlignment = kvp.Value switch
               {
                  "super" => BaselineAlignment.Superscript,
                  "sub" => BaselineAlignment.Subscript,
                  _ => formatting.BaselineAlignment
               };
               break;

            case "text-decoration":
               if (kvp.Value.Contains("underline"))
                  formatting.TextDecorations = TextDecorations.Underline;
               else if (kvp.Value.Contains("line-through"))
                  formatting.TextDecorations = TextDecorations.Strikethrough;
               break;
         }
      }
   }

   /// <summary>
   /// special case: <p><br></p> => empty paragraph
   /// </summary>
   /// <param name="paragraphNode"></param>
   /// <returns></returns>
   private static bool IsHtmlEmptyParagraph(HtmlNode paragraphNode)
   {
      var elementChildren = paragraphNode.ChildNodes
          .Where(n => n.NodeType == HtmlNodeType.Element)
          .ToList();

      var textContent = WebUtility.HtmlDecode(paragraphNode.InnerText);

      return elementChildren.Count == 1
          && elementChildren[0].Name.Equals("br", StringComparison.OrdinalIgnoreCase)
          && string.IsNullOrWhiteSpace(textContent);
   }


   private static Paragraph GetParagraphFromNode(HtmlNode childNode, FlowDocument fdoc)
   {
      Paragraph par = new(fdoc);

      if (!IsHtmlEmptyParagraph(childNode))
      {
         InlineFormatting defaultFormatting = new();
         AddInlinesFromNode(childNode, par, defaultFormatting);
      }

      foreach (KeyValuePair<string, string> kvp in ParseStyleAttribute(childNode.GetAttributeValue("style", "")))
      {
         switch (kvp.Key)
         {
            case "line-height":

               if (par.Inlines.Count > 0)
               {
                  double lineHeight = Double.TryParse(kvp.Value.Replace("px", ""), out var px) ? px : 0;
                  //double maxInlineHeight = par.Inlines.Max(ilh => ilh.InlineHeight);
                  //par.LineHeight = LineHeightToLineSpacing(lineHeight, maxInlineHeight);
                  par.LineHeight = lineHeight;
               }
               //par.LineHeight = Double.TryParse(kvp.Value.Replace("px", ""), out var px) ? px : 0;
               break;

            case "text-align":

               par.TextAlignment = kvp.Value switch
               {
                  "center" => TextAlignment.Center,
                  "right" => TextAlignment.Right,
                  "left" => TextAlignment.Left,
                  "justify" => TextAlignment.Justify,
                  _ => TextAlignment.Left
               };
               break;

            case "margin":
               string marginString = kvp.Value;
               if (marginString.Contains(' '))
               {
                  var margins = marginString.Split(' ').Select(val => int.TryParse(val.Replace("px", ""), out var px) ? px : 0).ToList();
                  if (margins.Count == 4)
                     par.Margin = new Thickness(margins[3], margins[0], margins[1], margins[2]);
               }
               else
               {
                  if (int.Parse(marginString.Replace("px", "")) is int padding)
                     par.Margin = new Thickness(padding);
               }

               break;

            case "background-color":
               if (ParseCssColor(kvp.Value) is SolidColorBrush bBrush)
                  par.Background = bBrush;
               break;

            case "border-color":
               if (ParseCssColor(kvp.Value) is SolidColorBrush bcBrush)
                  par.BorderBrush = bcBrush;
               break;

            case "border-width":
               par.BorderThickness = GetBorderThickness(kvp.Value);
               break;

         }
      }

      return par;

   }

   private static Thickness GetBorderThickness(string val)
   {
      Thickness returnThickness = new(1);

      var bwidths = val.Split(' ').Select(val => int.TryParse(val.Replace("px", ""), out var px) ? px : 0).ToList();
      if (bwidths.Count == 4)
         returnThickness = new Thickness(bwidths[3], bwidths[0], bwidths[1], bwidths[2]);

      return returnThickness;


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

   private static readonly Regex Rgba = new(@"^\s*rgba?\(\s*(?<r>\d{1,3})\s*,\s*(?<g>\d{1,3})\s*,\s*(?<b>\d{1,3})\s*(?:,\s*(?<a>[-+]?\d*\.?\d+)\s*)?\)\s*;?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

   private static ColorConverter colConverter = new();

   private static SolidColorBrush? ParseCssColor(string cssColor)
   {
      if (string.IsNullOrWhiteSpace(cssColor)) return null;

      cssColor = cssColor.Trim();

      // rgba()/rgb()
      var m = Rgba.Match(cssColor);
      if (m.Success)
      {
         if (!byte.TryParse(m.Groups["r"].Value, out var r)) return null;
         if (!byte.TryParse(m.Groups["g"].Value, out var g)) return null;
         if (!byte.TryParse(m.Groups["b"].Value, out var b)) return null;

         double aD = 1.0;
         if (m.Groups["a"].Success &&
             !double.TryParse(m.Groups["a"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out aD))
            return null;

         aD = Math.Clamp(aD, 0.0, 1.0);
         var a = (byte)Math.Round(aD * 255.0);

         return new SolidColorBrush(Avalonia.Media.Color.FromArgb(a, r, g, b));
      }

      // hex (#RGB, #RRGGBB, #AARRGGBB) and named colors (Red, etc.)
      try
      {
         var obj = colConverter.ConvertFromString(cssColor.TrimEnd(';'));
         if (obj is Avalonia.Media.Color c) return new SolidColorBrush(c);
      }
      catch { }

      return null;
   }


   //private static double LineHeightToLineSpacing (double lineHeight, double maxFontSize)
   //{
   //   return lineHeight - maxFontSize * 1.25;
   //}


   private static (Thickness? thickness, ISolidColorBrush? brush)? ParseBorderShorthand(string value)
   {
      if (string.IsNullOrWhiteSpace(value))
         return null;

      var v = value.Trim();
      if (string.Equals(v, "none", StringComparison.OrdinalIgnoreCase) ||
          string.Equals(v, "hidden", StringComparison.OrdinalIgnoreCase))
      {
         return (new Thickness(0), null);
      }

      var tokens = SplitCssTokens(v).ToList();
      if (tokens.Count == 0) return null;

      if (tokens.Any(t => t.Equals("none", StringComparison.OrdinalIgnoreCase) ||
                          t.Equals("hidden", StringComparison.OrdinalIgnoreCase)))
      {
         return (new Thickness(0), null);
      }

      Thickness? thickness = null;
      ISolidColorBrush? brush = null;

      foreach (var t in tokens)
      {
         var px = TryParseCssLengthPx(t);
         if (px.HasValue)
         {
            thickness = new Thickness(px.Value);
            break;
         }
      }

      foreach (var t in tokens)
      {
         var b = ParseCssColor(t);
         if (b != null)
         {
            brush = b;
            break;
         }
      }

      if (thickness == null && brush == null)
         return null;

      return (thickness, brush);
   }

   private static IEnumerable<string> SplitCssTokens(string s)
   {
      int i = 0;
      while (i < s.Length)
      {
         while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
         if (i >= s.Length) yield break;

         int start = i;
         int depth = 0;

         while (i < s.Length)
         {
            char ch = s[i];
            if (ch == '(') depth++;
            else if (ch == ')') depth = Math.Max(0, depth - 1);
            else if (depth == 0 && char.IsWhiteSpace(ch)) break;
            i++;
         }

         var tok = s.Substring(start, i - start).Trim();
         if (tok.Length > 0) yield return tok;
      }
   }

   private static double? TryParseCssLengthPx(string token)
   {
      if (string.IsNullOrWhiteSpace(token)) return null;
      token = token.Trim();

      var m = Regex.Match(token, @"^(?<n>\d+(\.\d+)?)\s*(px)?$", RegexOptions.IgnoreCase);
      if (!m.Success) return null;

      if (double.TryParse(m.Groups["n"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
         return v;

      return null;
   }


}
