﻿using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

internal static partial class WordConversions
{
   internal static IEditable GetDeletedRun(OpenXmlElement psection, ref Paragraph para)
   {
      foreach (OpenXmlElement rsection in psection.Elements())
         switch (rsection.LocalName)
         {
            case "r":
               return GetIEditable(rsection, ref para);
         }

      return null!;

   }

   internal static IEditable GetInsertedRun(OpenXmlElement psection, ref Paragraph para)
   {
      var thisrun = new EditableRun("");

      foreach (OpenXmlElement rsection in psection.Elements())
         switch (rsection.LocalName)
         {
            case "r":
               IEditable thisil = GetIEditable(rsection, ref para);
               thisrun.Text += ((EditableRun)thisil).Text;
               break;
         }

      thisrun.Background = Brushes.Gainsboro;
      thisrun.TextDecorations = TextDecorations.Underline;

      return thisrun;

   }

   internal static IEditable GetIEditable(OpenXmlElement psection, ref Paragraph para)
   {
      var thisrun = new EditableRun("");
      IEditable iline = thisrun;
      string contentType = "";

      foreach (OpenXmlElement rsection in psection.Elements())
      {
         try
         {
            switch (rsection.LocalName)
            {
               case "object":

                  Debug.WriteLine("object");
                  //try
                  //{
                  //   var inelmShape = rsection.Elements().Where(ce => (ce.LocalName ?? "") == "shape").ElementAtOrDefault(0);
                  //   string styleString = inelmShape.GetAttributes()[1].Value;
                  //   var imgelm = inelmShape.Elements().Where(ce => (ce.LocalName ?? "") == "imagedata").ElementAtOrDefault(0);
                  //   string rIdnoO = imgelm.GetAttributes().Where(gat => gat.LocalName == "id").FirstOrDefault().Value; ridnostring = rIdnoO;
                  //   ImagePart imgpO = (ImagePart)mainDocPart.GetPartById(rIdnoO);

                  //   string imgContentType = imgpO.ContentType.ToString();
                  //   var imgO = new Image();
                  //   var bmgO = new BitmapImage();

                  //   string[] wdhtParts = styleString.Split(";".ToCharArray());

                  //   switch (wdhtParts[0].Substring(wdhtParts[0].Length - 2))
                  //   {
                  //      case "in":
                  //         imgO.Width = InchesToPixels(Convert.ToDouble(wdhtParts[0].Split(":".ToCharArray())[1].TrimEnd("in".ToCharArray())));
                  //         break;
                  //      case "pt":
                  //         imgO.Width = PointsToPixels(Convert.ToDouble(wdhtParts[0].Split(":".ToCharArray())[1].TrimEnd("pt".ToCharArray())));
                  //         break;
                  //   }

                  //   switch (wdhtParts[1].Substring(wdhtParts[1].Length - 2))
                  //   {
                  //      case "in":
                  //         imgO.Height = InchesToPixels(Convert.ToDouble(wdhtParts[1].Split(":".ToCharArray())[1].TrimEnd("in".ToCharArray())));
                  //         break;
                  //      case "pt":
                  //         imgO.Height = PointsToPixels(Convert.ToDouble(wdhtParts[1].Split(":".ToCharArray())[1].TrimEnd("pt".ToCharArray())));
                  //         break;
                  //   }

                  //   MemoryStream streamExO = new MemoryStream();
                  //   imgpO.GetStream().CopyTo(streamExO);  //have to copy to new stream because it's in a zip, can't set position
                  //   streamExO.Position = 0;
                  //   System.Drawing.Imaging.Metafile mfimgO = new System.Drawing.Imaging.Metafile(streamExO);
                  //   streamExO.Position = 0;

                  //   if (imgContentType == "image/x-emf")
                  //   {
                  //      //Must redraw emf's to large size (*5) for better quality when converted to png
                  //      var target = new System.Drawing.Bitmap((int)imgO.Width * 5, (int)imgO.Height * 5);
                  //      var g = System.Drawing.Graphics.FromImage(target);
                  //      g.DrawImage(mfimgO, 0, 0, (int)imgO.Width * 5, (int)imgO.Height * 5);
                  //      target.Save(streamExO, System.Drawing.Imaging.ImageFormat.Png);
                  //   }
                  //   else
                  //   {
                  //      mfimgO.Save(streamExO, System.Drawing.Imaging.ImageFormat.Png);
                  //   }


                  //   streamExO.Position = 0;

                  //   bmgO.BeginInit();
                  //   bmgO.CacheOption = BitmapCacheOption.OnLoad;
                  //   bmgO.StreamSource = streamExO;
                  //   bmgO.EndInit();
                  //   imgO.Source = bmgO;
                  //   imgO.IsVisible = true;

                  //   iline = new EditableInlineUIContainer(imgO);
                  //   para.FontFamily = new FontFamily("IMAGE");

                  //}

                  //catch (Exception ex){ Debug.WriteLine("Error getting object"); }

                  break;

               case "drawing":

                  try
                  {
                     var inelm = rsection.Elements().Where(ce => (ce.LocalName ?? "") == "inline").ElementAtOrDefault(0);
                     var img = new Image();
                     string rIdno = "";
                     Stream imgStream = null!;
                     contentType = "";

                     if (inelm != null)
                     {
                        var extelm = inelm.Elements().Where(ce => (ce.LocalName ?? "") == "extent").ElementAtOrDefault(0);
                        if (extelm != null)
                        {
                           img.Width = EMUToPix(Convert.ToDouble(extelm.GetAttributes().Where(gat => gat.LocalName == "cx").FirstOrDefault().Value));
                           img.Height = EMUToPix(Convert.ToDouble(extelm.GetAttributes().Where(gat => gat.LocalName == "cy").FirstOrDefault().Value));
                        }
                        //MessageBox.Show("extelmWidth=" + img.Width.ToString() + ":::extELMHeight=" + img.Height.ToString());

                        var grelm = inelm.Elements().Where(ce => (ce.LocalName ?? "") == "graphic").ElementAtOrDefault(0);
                        var grdataelm = grelm?.Elements().Where(ce => (ce.LocalName ?? "") == "graphicData").ElementAtOrDefault(0);
                        var picelm = grdataelm?.Elements().Where(ce => (ce.LocalName ?? "") == "pic").ElementAtOrDefault(0);
                        if (picelm != null)
                        {
                           var blipFillelm = picelm.Elements().Where(ce => (ce.LocalName ?? "") == "blipFill").ElementAtOrDefault(0);
                           var blip = blipFillelm?.Elements().Where(ce => (ce.LocalName ?? "") == "blip").ElementAtOrDefault(0);
                           rIdno = blip?.GetAttributes()[0].Value ?? "0";
                           var spPr = picelm.Elements().Where(ce => (ce.LocalName ?? "") == "spPr").ElementAtOrDefault(0);
                           var xfrm = spPr?.Elements().Where(ce => (ce.LocalName ?? "") == "xfrm").ElementAtOrDefault(0);
                           var ext = xfrm?.Elements().Where(ce => (ce.LocalName ?? "") == "ext").ElementAtOrDefault(0);
                           img.Width = EMUToPix(Convert.ToDouble(ext?.GetAttributes()[0].Value));
                           img.Height = EMUToPix(Convert.ToDouble(ext?.GetAttributes()[1].Value));

                           ImagePart imgp = (ImagePart)mainDocPart!.GetPartById(rIdno!);
                           contentType = imgp.ContentType.ToString();
                           imgStream = imgp.GetStream();

                        }

                        var chartelm = grdataelm?.Elements().Where(ce => (ce.LocalName ?? "") == "chart").ElementAtOrDefault(0);
                        if (chartelm != null)
                        {
                           rIdno = chartelm.GetAttributes().Where(gat => gat.LocalName == "id").FirstOrDefault().Value ?? "0";
                           ChartPart chp = (ChartPart)mainDocPart!.GetPartById(rIdno);
                           contentType = chp.ContentType.ToString();
                           imgStream = chp.GetStream();
                        }

                        Bitmap bmg = null!; 
                        //Debug.WriteLine("contenttype= " + contentType);

                        if (contentType is "image/x-emf" or "image/x-wmf")
                        {
                           MemoryStream streamEx = new ();
                           imgStream.CopyTo(streamEx);  //have to copy to new stream because it's in a zip, can't set position
                           streamEx.Position = 0;
                           bmg = new Bitmap(streamEx);

                           //System.Drawing.Imaging.Metafile mfimg = new System.Drawing.Imaging.Metafile(streamEx);
                           //streamEx.Position = 0;
                           ////Must redraw emf's to large size (*5) for better quality when converted to png
                           //var target = new System.Drawing.Bitmap((int)img.Width * 5, (int)img.Height * 5);
                           //var g = System.Drawing.Graphics.FromImage(target);
                           //g.DrawImage(mfimg, 0, 0, (int)img.Width * 5, (int)img.Height * 5);
                           //target.Save(streamEx, System.Drawing.Imaging.ImageFormat.Png);
                           //streamEx.Position = 0;
                           //bmg.BeginInit();
                           //bmg.CacheOption = BitmapCacheOption.OnLoad;
                           //bmg.StreamSource = streamEx;
                           //bmg.EndInit();
                        }

                        else
                        {
                           MemoryStream streamEx = new ();
                           imgStream.CopyTo(streamEx);  //have to copy to new stream because it's in a zip, can't set position
                           streamEx.Position = 0;
                           bmg = new Bitmap(streamEx);
                        }

                        img.IsVisible = true;
                        img.Source = bmg;

                        iline = new EditableInlineUIContainer(img);
                     }

                  }
                  catch { Debug.WriteLine("drawing error:" + rsection.LocalName); }

                  break;


               case "rPr":

                  EditableRun thisRun = (EditableRun)iline!;
                  foreach (OpenXmlElement rprsection in rsection.Elements())
                  {
                     try
                     {
                        switch (rprsection.LocalName)
                        {
                           case "u":

                              TextDecoration ProtectedTextDecoration = new() { Location = TextDecorationLocation.Underline };
                              thisRun.TextDecorations = [ ProtectedTextDecoration ];
                              break;

                           //case "i": ((EditableRun)iline).FontStyle = FontStyle.Italic; break;
                           case "i": thisrun.FontStyle = FontStyle.Italic; break;
                           case "b": thisrun.FontWeight = FontWeight.Bold; break;
                                 
                           case "rFonts":

                              string runAsciiFont = "";
                              string runEastAsiaFont = "";
                              // Dim cultureInfo As CultureInfo = cultureInfo.CurrentCulture

                              foreach (OpenXmlAttribute ga in rprsection.GetAttributes())
                              {
                                 switch (ga.LocalName)
                                 {                                       
                                    case "ascii": { runAsciiFont = ga.Value!; break; }
                                    case "eastAsia": { runEastAsiaFont = ga.Value!; break; }
                                    case "hAnsi": { break; }
                                    case "cs": { break; }
                                    case "hint":
                                       {
                                          //System.Windows.Markup.XmlLanguage JapLang;
                                          //JapLang = (System.Windows.Markup.XmlLanguage)xlc.ConvertFromString("ja-JP");
                                          //if ((ga.Value ?? "") == "eastAsia") iline.FontFamily = new System.Windows.Media.FontFamily(DefaultEastAsiaFont);
                                          break;
                                       }
                                    default: { break; }
                                 }
                              }

                              runEastAsiaFont = (runEastAsiaFont == "") ? DefaultEastAsiaFont : runEastAsiaFont;
                              runAsciiFont = (runAsciiFont == "compositefont:Inter,#Inter, ") ? DefaultAsciiFont : runAsciiFont;

                              if (runAsciiFont != DefaultAsciiFont | runEastAsiaFont != DefaultEastAsiaFont)
                                 thisRun.FontFamily = new FontFamily(runEastAsiaFont + ", Inter");

                              break;

                           case "sz":


                              thisRun.FontSize = PointsToPixels(Convert.ToDouble(rprsection.GetAttributes()[0].Value));
                              if (thisRun.BaselineAlignment == BaselineAlignment.Subscript | ((EditableRun)iline).BaselineAlignment == BaselineAlignment.TextTop)
                                 thisRun.FontSize /= 3;
                              else
                                 thisRun.FontSize /= 2;
                              break;


                           case "szCs":


                              thisRun.FontSize = PointsToPixels(Convert.ToDouble(rprsection.GetAttributes()[0].Value));
                              if (thisRun.BaselineAlignment == BaselineAlignment.Subscript | thisRun.BaselineAlignment == BaselineAlignment.TextTop)
                                 thisRun.FontSize /= 3;
                              else
                                 thisRun.FontSize /= 2;
                              break;


                           case "highlight":

                              //thisRun.Background = HighlightColorValueToBrush(rprsection.GetAttributes()[0].Value ?? "#00000000");
                              BrushConverter? Bconverter = new();
                              thisRun.Background = (ImmutableSolidColorBrush)Bconverter.ConvertFromString(WordHighlightColorValueToHexString(rprsection.GetAttributes()[0].Value ?? "#00000000"))!;
                              break;

                           //case "tag":

                           //   thisRun.Tag = rprsection.GetAttributes()[0].Value;
                           //   break;


                           case "color":

                              try
                              {
                                 BrushConverter? BConverter = new();
                                 string? brushString = "#FF000000"!;
                                 string colorValString = rprsection.GetAttributes()[0].Value!;
                                 brushString = colorValString switch
                                 {
                                    "auto" => "#FF000000"!,
                                    _ => WordHighlightColorValueToHexString(rprsection.GetAttributes()[0].Value!),
                                 };
                                 thisRun.Foreground = (ImmutableSolidColorBrush)BConverter.ConvertFromString(brushString)!;
                                 //thisRun.Foreground = (SolidColorBrush)BConverter.ConvertFromString(brushString)!;
                              }
                              catch (Exception cEx) { Debug.WriteLine("Color error at:\n" + thisRun.Text + "\n\nvalue=" + rprsection.GetAttributes()[0].Value + "\n" + cEx.Message); }
                              break;


                           case "vertAlign":

                              switch (rprsection.GetAttributes()[0].Value)
                              {
                                 case "subscript":
                                    thisRun.BaselineAlignment = BaselineAlignment.Subscript;
                                    thisRun.FontSize /= 1.5;
                                    break;

                                 case "superscript":
                                    thisRun.BaselineAlignment = BaselineAlignment.Top;
                                    thisRun.FontSize /= 1.5;
                                    break;

                                 default:
                                    thisRun.BaselineAlignment = BaselineAlignment.Baseline;
                                    break;

                              }

                              break;
                        }

                     }
                     catch (Exception rprEx) { Debug.WriteLine("Error getting run properties:\nLocalName=" + rprsection.LocalName + "\n" + rprEx.Message); }
                  }

                  break;


               case "br":
                  EditableLineBreak newLineBreak = new();
                  para.Inlines.Add(newLineBreak);

                  //if (rsection.HasAttributes)
                  //{
                  //   if ((rsection.GetAttributes()[0].Value ?? "") == "page")
                  //   {
                  //      newLineBreak.FontFamily = new FontFamily("PAGEBREAK");
                  //      newLineBreak.Tag = "PageBreak";
                  //   }
                  //}

                  break;

               case "t": { thisrun.Text += rsection.InnerText; break; }
               //case "t": { thisrun.Text += rsection.InnerText; lastRunText = thisrun.Text; break; }  //For debugging purposes

               case "delText": { thisrun.Text += rsection.InnerText; thisrun.TextDecorations = TextDecorations.Strikethrough; thisrun.Foreground = Brushes.Red; break; }

               case "lastRenderedPageBreak": break;

               default: { break; }
                  //default: { MessageBox.Show("unknown:\n" + rsection.LocalName);  break; }

            }

         }
         catch (Exception ex) { Debug.WriteLine("Error getting run:\nLocalName=" + rsection.LocalName + "\n" + ex.Message); }

      }

      return iline;
   }


   internal static void AddDeepRuns(OpenXmlElement psection, ref Paragraph para)
   {
      foreach (OpenXmlElement deepRun in psection.Elements())
      {
         switch (deepRun.LocalName)
         {
            case "r": { para.Inlines.Add(GetIEditable(deepRun, ref para)); break; }
            case "smartTag": { AddDeepRuns(deepRun, ref para); break; }
         }
      }
   }


}

