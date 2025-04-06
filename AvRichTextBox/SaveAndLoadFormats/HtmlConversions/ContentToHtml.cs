using Avalonia.Media;
using Avalonia.Media.Imaging;
using DocumentFormat.OpenXml.Office2010.Excel;
using HtmlAgilityPack;
using System.IO;
using System;
using System.Net;
using System.Text;
using Avalonia.Controls;
using System.Linq;

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

      foreach (Block b in fdoc.Blocks){

         if (b is Paragraph p)
         {
            HtmlNode parnode = hdoc.CreateElement("p");
            //parnode.SetAttributeValue("style", GetParStyle(p));

            foreach (IEditable ied in p.Inlines)
            {
               HtmlNode spanNode = hdoc.CreateElement("span");

               switch (ied)
               {
                  case EditableRun erun:
                     spanNode.InnerHtml = WebUtility.HtmlEncode(erun.Text ?? "");
                     spanNode.SetAttributeValue("style", GetInlineStyle(erun));
                     break;

                  case EditableLineBreak elbreak:
                     spanNode = hdoc.CreateElement("br");
                     //parnode.AppendChild(br);
                     break;

                  case EditableInlineUIContainer eUIC:

                     if (eUIC.Child is Image img && img.Source is Bitmap bmp)
                     {
                        using var memStream = new MemoryStream();
                        bmp.Save(memStream); 
                        var base64 = Convert.ToBase64String(memStream.ToArray());

                        var imgNode = hdoc.CreateElement("img");
                        imgNode.SetAttributeValue("src", $"data:image/png;base64,{base64}");
                        imgNode.SetAttributeValue("width",  img.Width.ToString());
                        imgNode.SetAttributeValue("height", img.Height.ToString());

                        parnode.AppendChild(imgNode);
                     }
                     break;

               }

               parnode.AppendChild(spanNode);

            }

            parnode.SetAttributeValue("style", GetParStyle(p));

            body.AppendChild(parnode);
         }
      }
      

      return hdoc;
        
   }

   private static string GetParStyle(Paragraph p)
   {
      var parStyle = new StringBuilder();

      if (p.LineSpacing > 0)
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

      string? parBackgroundColor = GetCssColor(p.Background);
      if (parBackgroundColor != null)
         parStyle.Append($"background-color:{parBackgroundColor};");

      var borderColor = GetCssColor(p.BorderBrush);
      if (borderColor != null)
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

      string? foregroundColor = GetCssColor(run.Foreground);
      if (foregroundColor != null)
         sb.Append($"color:{foregroundColor};"); 

      string? backgroundColor = GetCssColor(run.Background);
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


   private static string? GetCssColor(IBrush? brush)
   {
      if (brush is SolidColorBrush solid)
      {
         var color = solid.Color;
         if (color == Colors.Transparent) return null;
         return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
         //return $"rgba({c.R},{c.G},{c.B},{c.A / 255.0:F2})";
      }

      return null; 
   }

    

}