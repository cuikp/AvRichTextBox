using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using DocumentFormat.OpenXml.Drawing.Charts;
using HtmlAgilityPack;
using System.Net;
using System.Text;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

internal static partial class HtmlConversions
{
   internal static HtmlDocument GetHtmlFromFlowDocument(FlowDocument fdoc)
   {

      HtmlDocument hdoc = new();

      HtmlNode html = hdoc.CreateElement("html");
      HtmlNode head = hdoc.CreateElement("head");
      HtmlNode body = hdoc.CreateElement("body");

      html.AppendChild(head);
      html.AppendChild(body);
      hdoc.DocumentNode.AppendChild(html);


      if (fdoc.PagePadding != default)
      {
         var pad = fdoc.PagePadding;
         var padStyle = $"padding:{pad.Top}px {pad.Right}px {pad.Bottom}px {pad.Left}px;";
         body.SetAttributeValue("style", padStyle);
      }

      foreach (Block b in fdoc.Blocks)
      {
         switch (b)
         {
            case Paragraph p:

               HtmlNode parnode = GetParagraphNode(p, hdoc);
               body.AppendChild(parnode);
               break;

            case Table t:

               HtmlNode tableNode = GetTableNode(t, hdoc);
               body.AppendChild(tableNode);
               break;
         }
      }

      return hdoc;

   }

   private static HtmlNode GetTableNode(Table t, HtmlDocument hdoc)
   {

      int noCols = t.ColDefs.Count;
      double colWidthPerc = Math.Round(100D / noCols);

      string tableAlignString = "margin: 0;";
      switch (t.TableAlignment)
      {
         case HorizontalAlignment.Center:
            tableAlignString = "margin: 0 auto;";
            break;
         case HorizontalAlignment.Right:
            tableAlignString = "margin-left: auto; margin-right: 0;";
            break;
      }

      double tabBorderThickness = t.BorderThickness.Left;
      string tabBorderCol = ColorToCss(t.BorderBrush.Color);

      HtmlNode tableNode = hdoc.CreateElement("table");
      string tableStyleString =
         "border-spacing: 0;" +
         "border-collapse: collapse;" +
         $"border: {tabBorderThickness}px solid {tabBorderCol};" +
         $"width: {t.Width}px;" +
         "box-sizing:border-box;" +
         tableAlignString +
         "table-layout: fixed;";
      tableNode.SetAttributeValue("style", tableStyleString);

      HtmlNode colgroupNode = hdoc.CreateElement("colgroup");
      foreach (ColumnDefinition cdef in t.ColDefs)
      {
         HtmlNode colDefNode = hdoc.CreateElement("col");
         string colStyleString = $"width: {colWidthPerc}%;";
         colDefNode.SetAttributeValue("style", colStyleString);
         colgroupNode.ChildNodes.Add(colDefNode);
      }

      tableNode.ChildNodes.Add(colgroupNode);

      for (int rowno = 0; rowno < t.RowDefs.Count; rowno++)
      {
         HtmlNode rowNode = hdoc.CreateElement("tr");
         rowNode.SetAttributeValue("style", "height: 40px;"); // sets minimum height to establish row even with vertical merges

         for (int colno = 0; colno < t.ColDefs.Count; colno++)
         {
            if (t.Cells.FirstOrDefault(c => c.RowNo == rowno && c.ColNo == colno) is Cell thisCell)
            {
               HtmlNode cellNode = hdoc.CreateElement("td");

               string valignString = thisCell.CellVerticalAlignment switch { VerticalAlignment.Top => "top", VerticalAlignment.Center => "center", VerticalAlignment.Bottom => "bottom", _ => "center" };

               string cellStyleString =
                  $"border-width: {thisCell.BorderThickness.Top}px {thisCell.BorderThickness.Right}px {thisCell.BorderThickness.Bottom}px {thisCell.BorderThickness.Left}px;" +
                  $"border-style: solid;" +
                  $"border-color: {ToCssColor(thisCell.BorderBrush, Brushes.Black)};" +
                  $"vertical-align: {valignString};" +
                  $"background-color: {ToCssColor(thisCell.CellBackground, Brushes.Transparent)};" +
                  $"padding: {thisCell.Padding.Top}px {thisCell.Padding.Right}px {thisCell.Padding.Bottom}px {thisCell.Padding.Left}px;";
               cellNode.SetAttributeValue("style", cellStyleString);

               HtmlAttribute colSpanAtt = hdoc.CreateAttribute("colspan", thisCell.ColSpan.ToString());
               HtmlAttribute rowSpanAtt = hdoc.CreateAttribute("rowspan", thisCell.RowSpan.ToString());
               cellNode.Attributes.Add(colSpanAtt);
               cellNode.Attributes.Add(rowSpanAtt);

               if (thisCell.CellContent is Paragraph p)
                  cellNode.ChildNodes.Add(GetParagraphNode(p, hdoc));

               rowNode.ChildNodes.Add(cellNode);
            }
         }

         tableNode.ChildNodes.Add(rowNode);
      }

      return tableNode;


   }


   private static HtmlNode GetParagraphNode(Paragraph p, HtmlDocument hdoc)
   {
      HtmlNode parnode = hdoc.CreateElement("p");

      bool hasContent = false;

      foreach (IEditable ied in p.Inlines)
      {
         switch (ied)
         {
            case EditableRun erun:
               {
                  if (!string.IsNullOrEmpty(erun.Text))
                  {
                     var spanNode = hdoc.CreateElement("span");
                     spanNode.InnerHtml = WebUtility.HtmlEncode(erun.Text ?? "");
                     spanNode.SetAttributeValue("style", GetInlineStyle(erun));
                     parnode.AppendChild(spanNode);
                     hasContent = true;
                  }
                  break;
               }

            case EditableLineBreak:
               {
                  var brNode = hdoc.CreateElement("br");
                  parnode.AppendChild(brNode);
                  hasContent = true;
                  break;
               }

            case EditableInlineUIContainer eUIC:
               {
                  if (eUIC.Child is Image img && img.Source is Bitmap bmp)
                  {
                     using var memStream = new MemoryStream();
                     bmp.Save(memStream);
                     var base64 = Convert.ToBase64String(memStream.ToArray());

                     var imgNode = hdoc.CreateElement("img");
                     imgNode.SetAttributeValue("src", $"data:image/png;base64,{base64}");
                     imgNode.SetAttributeValue("width", img.Width.ToString());
                     imgNode.SetAttributeValue("height", img.Height.ToString());

                     parnode.AppendChild(imgNode);
                     hasContent = true;
                  }
                  break;
               }
         }
      }

      if (!hasContent)
      {
         parnode.AppendChild(hdoc.CreateElement("br"));
      }

      parnode.SetAttributeValue("style", GetParStyle(p));
      return parnode;
   }


   private static string GetParStyle(Paragraph p)
   {
      var parStyle = new StringBuilder();

      if (p.LineHeight > 0)
         parStyle.Append($"line-height:{p.LineHeight}px;");

      switch (p.TextAlignment)
      {
         case TextAlignment.Center:
            parStyle.Append("text-align:center;");
            break;
         case TextAlignment.Right:
            parStyle.Append("text-align:right;");
            break;
         case TextAlignment.Left:
            parStyle.Append("text-align:left;");
            break;
         case TextAlignment.Justify:
            parStyle.Append("text-align:justify;");
            break;
      }

      if (p.Margin != default)
         parStyle.Append($"margin:{p.Margin.Top}px {p.Margin.Right}px {p.Margin.Bottom}px {p.Margin.Left}px;");

      string? parBackgroundColor = ToCssColor(p.Background, Brushes.Transparent);
      if (parBackgroundColor != "")
         parStyle.Append($"background-color:{parBackgroundColor};");

      var borderColor = ToCssColor(p.BorderBrush, Brushes.Black);
      if (borderColor != "")
         parStyle.Append($"border-color:{borderColor};");

      if (p.BorderThickness != default)
         parStyle.Append($"border-style:solid;border-width:{p.BorderThickness.Top}px {p.BorderThickness.Right}px {p.BorderThickness.Bottom}px {p.BorderThickness.Left}px;");

      return parStyle.ToString();
   }


   private static string GetInlineStyle(EditableRun run)
   {
      var sb = new StringBuilder();

      if (!string.IsNullOrEmpty(run.FontFamily.ToString()))
         sb.Append($"font-family:{run.FontFamily};");

      if (run.FontWeight == FontWeight.Bold)
         sb.Append("font-weight:bold;");

      if (run.FontStyle == FontStyle.Italic)
         sb.Append("font-style:italic;");

      if (run.FontSize > 0)
         sb.Append($"font-size:{run.FontSize}px;");

      string? foregroundColor = ToCssColor(run.Foreground, Brushes.Black);
      if (foregroundColor != null)
         sb.Append($"color:{foregroundColor};");

      string? backgroundColor = ToCssColor(run.Background, Brushes.Transparent);
      if (backgroundColor != null)
         sb.Append($"background-color:{backgroundColor};");


      if (run.TextDecorations != null)
      {
         foreach (var td in run.TextDecorations)
         {
            switch (td.Location)
            {
               case TextDecorationLocation.Underline:
                  sb.Append("text-decoration:underline;");
                  break;
               case TextDecorationLocation.Strikethrough:
                  sb.Append("text-decoration:line-through;");
                  break;
            }
         }
      }

      if (run.BaselineAlignment == BaselineAlignment.Superscript)
         sb.Append("vertical-align: super;");
      if (run.BaselineAlignment == BaselineAlignment.Subscript)
         sb.Append("vertical-align: sub;");

      return sb.ToString();
   }


   private static string ToCssColor(IBrush? brush, IBrush? defaultBrush)
   {

      if (brush is SolidColorBrush scb)
         return ColorToCss(scb.Color);

      if (brush is ImmutableSolidColorBrush iscb)
         return ColorToCss(iscb.Color);

      if (defaultBrush is SolidColorBrush dfb)
         return ColorToCss(dfb.Color);

      if (defaultBrush is ImmutableSolidColorBrush idfb)
         return ColorToCss(idfb.Color);

      return "transparent";

   }

   private static string ColorToCss(Color c)
   {
      return $"rgba({c.R},{c.G},{c.B},{c.A / 255.0})";
   }



}