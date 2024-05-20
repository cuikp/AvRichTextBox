//using DocumentFormat.OpenXml;
//using DocumentFormat.OpenXml.Packaging;
//using System.Diagnostics;
//using System.Drawing.Imaging;
//using System.Net.Mime;
//using System.Windows.Media.Imaging;

//namespace QuikFind;

//public partial class XmlToFlowDocument
//{
//    public static int numDrawingErrors = 0;

//    public static int picno = 0;

//    private static string lastRunText = "";

//    public static Inline GetDeletedRun(OpenXmlElement psection, ref Paragraph para)
//    {
//        foreach (OpenXmlElement rsection in psection.Elements())
//            switch (rsection.LocalName)
//            {
//                case "r":
//                    return GetRun(rsection, ref para);
//            }

//        return null;

//    }

//    public static Inline GetInsertedRun(OpenXmlElement psection, ref Paragraph para)
//    {
//        var thisrun = new Run();

//        foreach (OpenXmlElement rsection in psection.Elements())
//            switch (rsection.LocalName)
//            {
//                case "r":
//                    Inline thisil = GetRun(rsection, ref para);
//                    thisrun.Text += ((Run)thisil).Text;
//                    break;
//            }

//        thisrun.Background = Brushes.Gainsboro;
//        thisrun.TextDecorations = TextDecorations.Underline;

//        return thisrun;

//    }

//    public static Inline GetRun(OpenXmlElement psection, ref Paragraph para)
//    {
//        var thisrun = new Run();
//        Inline iline = thisrun;
//        string ridnostring = "";
//        string contentType = "";

//        foreach (OpenXmlElement rsection in psection.Elements())
//        {
//            try
//            {
//                switch (rsection.LocalName)
//                {
//                    case "object":

//                        try
//                        {
//                            var inelmShape = rsection.Elements().Where(ce => (ce.LocalName ?? "") == "shape").ElementAtOrDefault(0);
//                            string styleString = inelmShape.GetAttributes()[1].Value;
//                            var imgelm = inelmShape.Elements().Where(ce => (ce.LocalName ?? "") == "imagedata").ElementAtOrDefault(0);
//                            string rIdnoO = imgelm.GetAttributes().Where(gat => gat.LocalName == "id").FirstOrDefault().Value; ridnostring = rIdnoO;
//                            ImagePart imgpO = (ImagePart)mainDocPart.GetPartById(rIdnoO);
                            
//                            string imgContentType = imgpO.ContentType.ToString();
//                            var imgO = new System.Windows.Controls.Image();
//                            var bmgO = new BitmapImage();

//                            string[] wdhtParts = styleString.Split(";".ToCharArray());

//                            switch (wdhtParts[0].Substring(wdhtParts[0].Length - 2))
//                            {
//                                case "in":
//                                    imgO.Width = InchesToPixels(Convert.ToDouble(wdhtParts[0].Split(":".ToCharArray())[1].TrimEnd("in".ToCharArray())));
//                                    break;
//                                case "pt":
//                                    imgO.Width = PointsToPixels(Convert.ToDouble(wdhtParts[0].Split(":".ToCharArray())[1].TrimEnd("pt".ToCharArray())));
//                                    break;
//                            }

//                            switch (wdhtParts[1].Substring(wdhtParts[1].Length - 2))
//                            {
//                                case "in":
//                                    imgO.Height = InchesToPixels(Convert.ToDouble(wdhtParts[1].Split(":".ToCharArray())[1].TrimEnd("in".ToCharArray())));
//                                    break;
//                                case "pt":
//                                    imgO.Height = PointsToPixels(Convert.ToDouble(wdhtParts[1].Split(":".ToCharArray())[1].TrimEnd("pt".ToCharArray())));
//                                    break;
//                            }

//                            MemoryStream streamExO = new MemoryStream();
//                            imgpO.GetStream().CopyTo(streamExO);  //have to copy to new stream because it's in a zip, can't set position
//                            streamExO.Position = 0;
//                            System.Drawing.Imaging.Metafile mfimgO = new System.Drawing.Imaging.Metafile(streamExO);
//                            streamExO.Position = 0;

//                            if (imgContentType == "image/x-emf")
//                            {
//                                //Must redraw emf's to large size (*5) for better quality when converted to png
//                                var target = new System.Drawing.Bitmap((int)imgO.Width * 5, (int)imgO.Height * 5);
//                                var g = System.Drawing.Graphics.FromImage(target);
//                                g.DrawImage(mfimgO, 0, 0, (int)imgO.Width * 5, (int)imgO.Height * 5);
//                                target.Save(streamExO, System.Drawing.Imaging.ImageFormat.Png);
//                            }
//                            else
//                            {
//                                mfimgO.Save(streamExO, System.Drawing.Imaging.ImageFormat.Png);
//                            }

                            
//                            streamExO.Position = 0;

//                            bmgO.BeginInit();
//                            bmgO.CacheOption = BitmapCacheOption.OnLoad;
//                            bmgO.StreamSource = streamExO;
//                            bmgO.EndInit();
//                            imgO.Source = bmgO;

//                            imgO.Visibility = Visibility.Visible;

//                            iline = new InlineUIContainer(imgO);
//                            para.FontFamily = new FontFamily("IMAGE");

//                        }

//                        catch (Exception ex)
//                        {
//                            if (MessageBox.Show("Error getting object section:\n" + ex.Message + "\n\nContinue?\n\n(ridno=" + ridnostring + ")", "QuikFind error", MessageBoxButton.OKCancel)
//                                == MessageBoxResult.Cancel) Environment.Exit(0);
//                        }

//                        break;

//                    case "drawing":

//                        try
//                        {

//                            var inelm = rsection.Elements().Where(ce => (ce.LocalName ?? "") == "inline").ElementAtOrDefault(0);
//                            var img = new System.Windows.Controls.Image();
//                            string rIdno = "";
//                            Stream imgStream = null;
//                            contentType = "";

//                            if (inelm != null)
//                            {

//                                //MessageBox.Show("inelm loading");

//                                var extelm = inelm.Elements().Where(ce => (ce.LocalName ?? "") == "extent").ElementAtOrDefault(0);
//                                if (extelm != null)
//                                {
//                                    img.Width = EMUToPix(Convert.ToDouble(extelm.GetAttributes().Where(gat => gat.LocalName == "cx").FirstOrDefault().Value));
//                                    img.Height = EMUToPix(Convert.ToDouble(extelm.GetAttributes().Where(gat => gat.LocalName == "cy").FirstOrDefault().Value));
//                                }
//                                //MessageBox.Show("extelmWidth=" + img.Width.ToString() + ":::extELMHeight=" + img.Height.ToString());

//                                var grelm = inelm.Elements().Where(ce => (ce.LocalName ?? "") == "graphic").ElementAtOrDefault(0);
//                                var grdataelm = grelm.Elements().Where(ce => (ce.LocalName ?? "") == "graphicData").ElementAtOrDefault(0);

//                                var picelm = grdataelm.Elements().Where(ce => (ce.LocalName ?? "") == "pic").ElementAtOrDefault(0);
//                                if (picelm != null)
//                                {
//                                    var blipFillelm = picelm.Elements().Where(ce => (ce.LocalName ?? "") == "blipFill").ElementAtOrDefault(0);
//                                    var blip = blipFillelm.Elements().Where(ce => (ce.LocalName ?? "") == "blip").ElementAtOrDefault(0);
//                                    rIdno = blip.GetAttributes()[0].Value;
//                                    var spPr = picelm.Elements().Where(ce => (ce.LocalName ?? "") == "spPr").ElementAtOrDefault(0);
//                                    var xfrm = spPr.Elements().Where(ce => (ce.LocalName ?? "") == "xfrm").ElementAtOrDefault(0);
//                                    var ext = xfrm.Elements().Where(ce => (ce.LocalName ?? "") == "ext").ElementAtOrDefault(0);
//                                    img.Width = EMUToPix(Convert.ToDouble(ext.GetAttributes()[0].Value));
//                                    img.Height = EMUToPix(Convert.ToDouble(ext.GetAttributes()[1].Value));

//                                    ImagePart imgp = (ImagePart)mainDocPart.GetPartById(rIdno);
//                                    contentType = imgp.ContentType.ToString();
//                                    imgStream = imgp.GetStream();

//                                }

//                                var chartelm = grdataelm.Elements().Where(ce => (ce.LocalName ?? "") == "chart").ElementAtOrDefault(0);
//                                if (chartelm != null)
//                                {
//                                    rIdno = chartelm.GetAttributes().Where(gat => gat.LocalName == "id").FirstOrDefault().Value;
//                                    ChartPart chp = (ChartPart)mainDocPart.GetPartById(rIdno);
//                                    contentType = chp.ContentType.ToString();
//                                    imgStream = chp.GetStream();
//                                }


//                                var bmg = new BitmapImage();

//                                //MessageBox.Show("contenttype= " + contentType);

//                                if (contentType == "image/x-emf" || contentType == "image/x-wmf")
//                                {

//                                    MemoryStream streamEx = new MemoryStream();
//                                    imgStream.CopyTo(streamEx);  //have to copy to new stream because it's in a zip, can't set position

//                                    streamEx.Position = 0;
//                                    System.Drawing.Imaging.Metafile mfimg = new System.Drawing.Imaging.Metafile(streamEx);
//                                    streamEx.Position = 0;

//                                    //Must redraw emf's to large size (*5) for better quality when converted to png
//                                    var target = new System.Drawing.Bitmap((int)img.Width * 5, (int)img.Height * 5);
//                                    var g = System.Drawing.Graphics.FromImage(target);
//                                    g.DrawImage(mfimg, 0, 0, (int)img.Width * 5, (int)img.Height * 5);
//                                    target.Save(streamEx, System.Drawing.Imaging.ImageFormat.Png);

//                                    streamEx.Position = 0;
//                                    bmg.BeginInit();
//                                    bmg.CacheOption = BitmapCacheOption.OnLoad;
//                                    bmg.StreamSource = streamEx;
//                                    bmg.EndInit();

//                                }

//                                else

//                                {
//                                    //MessageBox.Show("else");

//                                    MemoryStream streamEx = new MemoryStream();
//                                    imgStream.CopyTo(streamEx);  //have to copy to new stream because it's in a zip, can't set position
//                                    streamEx.Position = 0;
//                                    bmg.BeginInit();
//                                    bmg.CacheOption = BitmapCacheOption.OnDemand;
//                                    bmg.StreamSource = streamEx;
//                                    bmg.EndInit();
//                                }

//                                img.Visibility = Visibility.Visible;
//                                img.Source = bmg;

//                                iline = new InlineUIContainer(img);

//                                para.FontFamily = new FontFamily("IMAGE");
//                            }

//                        }
//                        //catch (Exception exd) { MessageBox.Show("Error trying to get drawing:\n" + exd.Message + "\ncontentype= " + contentType); }
//                        catch (Exception exd) { numDrawingErrors += 1; }

//                        break;


//                    case "rPr":

//                        foreach (OpenXmlElement rprsection in rsection.Elements())
//                        {
//                            try
//                            {
//                                switch (rprsection.LocalName)
//                                {
//                                    case "u":

//                                        var ProtectedTextDecoration = new TextDecoration();
//                                        ProtectedTextDecoration.Location = TextDecorationLocation.Underline;
//                                        iline.TextDecorations.Add(ProtectedTextDecoration);
//                                        break;


//                                    case "i": iline = new Italic(iline); break;
//                                    case "b": iline = new Bold(iline); break;

//                                    case "rFonts":
//                                        var xlc = new System.Windows.Markup.XmlLanguageConverter();
//                                        string runAsciiFont = "";
//                                        string runEastAsiaFont = "";
//                                        // Dim cultureInfo As CultureInfo = cultureInfo.CurrentCulture

//                                        foreach (OpenXmlAttribute ga in rprsection.GetAttributes())
//                                        {
//                                            switch (ga.LocalName)
//                                            {
//                                                case "ascii": { runAsciiFont = ga.Value; break; }
//                                                case "eastAsia": { runEastAsiaFont = ga.Value; break; }
//                                                case "hAnsi": { break; }
//                                                case "cs": { break; }
//                                                case "hint":
//                                                    {
//                                                        //System.Windows.Markup.XmlLanguage JapLang;
//                                                        //JapLang = (System.Windows.Markup.XmlLanguage)xlc.ConvertFromString("ja-JP");
//                                                        //if ((ga.Value ?? "") == "eastAsia") iline.FontFamily = new System.Windows.Media.FontFamily(DefaultEastAsiaFont);
//                                                        break;
//                                                    }
//                                                default: { break; }
//                                            }

//                                        }

//                                        runEastAsiaFont = (runEastAsiaFont == "") ? DefaultEastAsiaFont : runEastAsiaFont;
//                                        runAsciiFont = (runAsciiFont == "") ? DefaultAsciiFont : runAsciiFont;

//                                        if (runAsciiFont != DefaultAsciiFont | runEastAsiaFont != DefaultEastAsiaFont)
//                                            iline.FontFamily = new FontFamily(runAsciiFont + ", " + runEastAsiaFont);

//                                        break;

//                                    case "sz":


//                                        iline.FontSize = PointsToPixels(Convert.ToDouble(rprsection.GetAttributes()[0].Value));
//                                        if (iline.BaselineAlignment == BaselineAlignment.Subscript | iline.BaselineAlignment == BaselineAlignment.TextTop)
//                                            iline.FontSize /= 3;
//                                        else
//                                            iline.FontSize /= 2;
//                                        break;


//                                    case "szCs":


//                                        iline.FontSize = PointsToPixels(Convert.ToDouble(rprsection.GetAttributes()[0].Value));
//                                        if (iline.BaselineAlignment == BaselineAlignment.Subscript | iline.BaselineAlignment == BaselineAlignment.TextTop)
//                                            iline.FontSize /= 3;
//                                        else
//                                            iline.FontSize /= 2;
//                                        break;


//                                    case "highlight":

//                                        iline.Background = HighlightColorValueToBrush(rprsection.GetAttributes()[0].Value);
//                                        break;

//                                    case "tag":

//                                        iline.Tag = rprsection.GetAttributes()[0].Value;
//                                        break;


//                                    case "color":

//                                        try
//                                        {
//                                            BrushConverter Bconverter = new BrushConverter();
//                                            Brush brush = (Brush)Bconverter.ConvertFromString("#FF000000");
//                                            string colorValString = rprsection.GetAttributes()[0].Value;
//                                            switch (colorValString)
//                                            {
//                                                case "auto":
//                                                    brush = (Brush)Bconverter.ConvertFromString("#FFFF0000"); //red
//                                                    break;
//                                                default:
//                                                    brush = (Brush)Bconverter.ConvertFromString("#FF" + rprsection.GetAttributes()[0].Value);
//                                                    break;
//                                            }
//                                            iline.Foreground = brush;
//                                        }
//                                        catch (Exception cEx) { MessageBox.Show("Color error at:\n" + lastRunText + "\n\nvalue=" + rprsection.GetAttributes()[0].Value + "\n" + cEx.Message); }
//                                        break;


//                                    case "vertAlign":

//                                        switch (rprsection.GetAttributes()[0].Value)
//                                        {
//                                            case "subscript":
//                                                iline.BaselineAlignment = BaselineAlignment.Subscript;
//                                                iline.FontSize /= 1.5;
//                                                break;


//                                            case "superscript":
//                                                // iline.BaselineAlignment = BaselineAlignment.Superscript  //displays too high
//                                                iline.BaselineAlignment = BaselineAlignment.TextTop;
//                                                iline.FontSize /= 1.5;
//                                                break;

//                                            default:
//                                                iline.BaselineAlignment = BaselineAlignment.Baseline;
//                                                break;

//                                        }

//                                        break;
//                                }

//                            }
//                            catch (Exception rprEx) { MessageBox.Show("Error getting run properties:\nLocalName=" + rprsection.LocalName + "\n" + rprEx.Message); }
//                        }

//                        break;


//                    case "br":
//                        LineBreak newLineBreak = new LineBreak();
//                        para.Inlines.Add(newLineBreak);

//                        if (rsection.HasAttributes)
//                        {
//                            if ((rsection.GetAttributes()[0].Value ?? "") == "page")
//                            {
//                                newLineBreak.FontFamily = new FontFamily("PAGEBREAK");
//                                newLineBreak.Tag = "PageBreak";
//                            }
//                        }

//                        break;

//                    case "t": { thisrun.Text += rsection.InnerText; break; }
//                    //case "t": { thisrun.Text += rsection.InnerText; lastRunText = thisrun.Text; break; }  //For debugging purposes

//                    case "delText": { thisrun.Text += rsection.InnerText; thisrun.TextDecorations = TextDecorations.Strikethrough; thisrun.Foreground = Brushes.Red; break; }

//                    case "lastRenderedPageBreak": break;

//                    default: { break; }
//                        //default: { MessageBox.Show("unknown:\n" + rsection.LocalName);  break; }

//                }

//            }
//            catch (Exception ex) { MessageBox.Show("Error getting run:\nLocalName=" + rsection.LocalName + "\n" + ex.Message); }

//        }

//        return iline;
//    }
      

//    public static void AddDeepRuns(OpenXmlElement psection, ref Paragraph para)
//    {
//        foreach (OpenXmlElement deepRun in psection.Elements())
//        {
//            switch (deepRun.LocalName)
//            {
//                case "r": { para.Inlines.Add(GetRun(deepRun, ref para)); break; }
//                case "smartTag": { AddDeepRuns(deepRun, ref para); break; }
//            }
//        }
//    }


//}

