using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DynamicData;
using RtfDomParserAv;
using System.Text;
using System.Text.RegularExpressions;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

internal static partial class RtfConversions
{
    internal static string DefaultEastAsiaFont = "";
    internal static string DefaultAsciiFont = "";


    internal static void GetFlowDocumentFromRtf(RTFDomDocument rtfdoc, FlowDocument fdoc)
    {
        double leftMargin = Math.Round(TwipToPix(rtfdoc.LeftMargin));
        double topMargin = Math.Round(TwipToPix(rtfdoc.TopMargin));
        double rightMargin = Math.Round(TwipToPix(rtfdoc.RightMargin));
        double bottomMargin = Math.Round(TwipToPix(rtfdoc.BottomMargin));

        fdoc.PagePadding = new Thickness(leftMargin, topMargin, rightMargin, bottomMargin);

        foreach (RTFDomElement rtfelm in rtfdoc.Elements)
        {
            switch (rtfelm)
            {
                case RTFDomParagraph rtfpar:

                    Paragraph newpar = GetParagraphFromRtfDom(rtfpar, fdoc);
                    fdoc.Blocks.Add(newpar);
                    break;

                case RTFDomTable rtftable:
                    Table newtable = GetTableFromRtfDom(rtftable, fdoc, rtfdoc.ColorTable);
                    fdoc.Blocks.Add(newtable);
                    break;

                //default:
                //    Debug.WriteLine("unknown rtfdoc element: " + rtfelm.GetType());
                //    break;
            }

        }

        fdoc.PagePadding = new Thickness(TwipToPix(rtfdoc.LeftMargin), TwipToPix(rtfdoc.TopMargin), TwipToPix(rtfdoc.RightMargin), TwipToPix(rtfdoc.BottomMargin));

    }

    internal static Table GetTableFromRtfDom(RTFDomTable rtftable, FlowDocument fdoc, RTFColorTable cTable)
    {
        Table newtable = new();
        for (int colno = 0; colno < rtftable.Columns.Count; colno++)
        {
            RTFDomElement thisCol = rtftable.Columns[colno];
            newtable.ColDefs.Add(new());
        }

        int tablewidth = 20;
        int rowno = 0;
        foreach (RTFDomTableRow row in rtftable.Elements.OfType<RTFDomTableRow>())
        {
            RowDefinition newRowDef = new();
            newtable.RowDefs.Add(newRowDef);


            foreach (RTFAttribute att in row.Attributes)
            {
                switch (att.Name)
                {
                    case "trql":
                        newtable.TableAlignment = HorizontalAlignment.Left;
                        break;
                    case "trqc":
                        newtable.TableAlignment = HorizontalAlignment.Center;
                        break;
                    case "trqr":
                        newtable.TableAlignment = HorizontalAlignment.Right;
                        break;

                    case "trrh":
                        newRowDef.MinHeight = Math.Round(TwipToPix(att.Value));
                        break;
                }
            }

            int colno = 0;

            BorderType borderType = BorderType.Left;

            foreach (RTFDomTableCell celm in row.Elements.OfType<RTFDomTableCell>())
            {

                Cell newCell = new() { RowNo = rowno, ColNo = colno, ColSpan = celm.ColSpan, RowSpan = celm.RowSpan };

                double cellBorderLeft = 1;
                double cellBorderTop = 1;
                double cellBorderRight = 1;
                double cellBorderBottom = 1;

                double cellPaddingLeft = 1;
                double cellPaddingTop = 1;
                double cellPaddingRight = 1;
                double cellPaddingBottom = 1;

                bool isVMerged = false;
                bool isHMerged = false;
                
                foreach (RTFAttribute att in celm.Attributes)
                {
                    switch (att.Name)
                    {
                        case "clvertalc":
                            newCell.CellVerticalAlignment = VerticalAlignment.Center;
                            break;
                        case "clvertalt":
                            newCell.CellVerticalAlignment = VerticalAlignment.Top;
                            break;
                        case "clvertalb":
                            newCell.CellVerticalAlignment = VerticalAlignment.Bottom;
                            break;

                        case "clmrg":
                            isHMerged = true;
                            break;

                        case "clmgf":
                            //Debug.WriteLine("RTB: cell colspan = " + newCell.ColSpan);
                            break;

                        case "clvmrg":
                            isVMerged = true;
                            break;

                        case "clvmgf":
                            break;

                        case "brdrs":
                            Debug.WriteLine("border single");
                            break;

                        case "brdrw":
                            switch (borderType)
                            {
                                case BorderType.Left:
                                    cellBorderLeft = Math.Round(TwipToPix(att.Value));
                                    break;
                                case BorderType.Right:
                                    cellBorderRight = Math.Round(TwipToPix(att.Value));
                                    break;
                                case BorderType.Top:
                                    cellBorderTop = Math.Round(TwipToPix(att.Value));
                                    break;
                                case BorderType.Bottom:
                                    cellBorderBottom = Math.Round(TwipToPix(att.Value));
                                    break;
                            }
                            break;

                        case "clbrdrb":
                            borderType = BorderType.Bottom;
                            break;
                        case "clbrdrt":
                            borderType = BorderType.Top;
                            break;
                        case "clbrdrr":
                            borderType = BorderType.Right;
                            break;
                        case "clbrdrl":
                            borderType = BorderType.Left;
                            break;

                        case "brdrcf":
                            newCell.BorderBrush = new SolidColorBrush(cTable.GetColor(att.Value, Colors.Black));
                            break;

                        case "clcbpat":
                            newCell.CellBackground = new SolidColorBrush(cTable.GetColor(att.Value, Colors.Black));
                            break;

                        case "cellx":
                            tablewidth = Math.Max(tablewidth, att.Value);
                            break;

                        case "clpadl":
                            cellPaddingLeft = Math.Round(TwipToPix(att.Value));
                            break;

                        case "clpadt":
                            cellPaddingTop = Math.Round(TwipToPix(att.Value));
                            break;

                        case "clpadr":
                            cellPaddingRight = Math.Round(TwipToPix(att.Value));
                            break;

                        case "clpadb":
                            cellPaddingBottom = Math.Round(TwipToPix(att.Value));
                            break;

                        default:
                            Debug.WriteLine("unknown attr = " + att.Name);
                            break;
                    }
                }

                if (!isVMerged && !isHMerged)
                {
                    foreach (RTFDomParagraph rtfpardom in celm.Elements.OfType<RTFDomParagraph>())
                        newCell.CellBlocks.Add(GetParagraphFromRtfDom(rtfpardom, fdoc));

                    newCell.BorderThickness = new(cellBorderLeft, cellBorderTop, cellBorderRight, cellBorderBottom);
                    newCell.Padding = new(cellPaddingLeft, cellPaddingTop, cellPaddingRight, cellPaddingBottom);

                    if (newCell.CellBlocks != null)
                        newtable.Cells.Add(newCell);
                }

                colno++;
            }

            rowno++;
        }

        newtable.Width = TwipToPix(tablewidth);
        int cols = newtable.ColDefs.Count;
        newtable.ColDefs.ToList().ForEach(cd => cd.Width = new(newtable.Width / cols, Avalonia.Controls.GridUnitType.Pixel));

        return newtable;
    }

    internal static Paragraph GetParagraphFromRtfDom(RTFDomParagraph rtfpar, FlowDocument fdoc)
    {
        Paragraph newpar = new();

        switch (rtfpar.Format.Align)
        {
            case RTFAlignment.Left: newpar.TextAlignment = TextAlignment.Left; break;
            case RTFAlignment.Center: newpar.TextAlignment = TextAlignment.Center; break;
            case RTFAlignment.Right: newpar.TextAlignment = TextAlignment.Right; break;
            case RTFAlignment.Justify: newpar.TextAlignment = TextAlignment.Justify; break;
        }

        newpar.Background = new SolidColorBrush(rtfpar.Format.BackColor);
        newpar.BorderBrush = new SolidColorBrush(rtfpar.Format.BorderColor);
        newpar.BorderThickness = new Thickness(TwipToPix(rtfpar.Format.BorderWidth));
        newpar.FontFamily = new FontFamily(rtfpar.Format.FontName);
        newpar.LeftIndent = TwipToPix(rtfpar.Format.LeftIndent);
        newpar.FirstLineIndent = TwipToPix(rtfpar.Format.ParagraphFirstLineIndent);

        List<IEditable> addInlines = GetRtfTextElementsAsInlines(rtfpar.Elements);
        newpar.ListMarker = TakeListText(addInlines);

        newpar.Inlines.AddRange(addInlines);

        newpar.EnsureEmptyRuns();

        double ilineH = newpar.Inlines.First().InlineHeight;

        if (rtfpar.Format.MultipleLineSpacing)
            newpar.LineHeight = ilineH * (rtfpar.Format.LineSpacing / 240.0);
        else
            newpar.LineHeight = Math.Abs(rtfpar.Format.LineSpacing) / 15.0;


        return newpar;
    }

    internal static List<IEditable> GetRtfTextElementsAsInlines(RTFDomElementList elements)
    {
        List<IEditable> returnList = [];

        foreach (RTFDomElement domelm in elements)
        {

            if (domelm is RTFDomField rtfField)
            {

                EditableHyperlink newEHL = GetRtfField(rtfField);
                if (newEHL != null)
                    returnList.Add(newEHL);
            }

            else if (domelm is RTFDomLineBreak rtflineBreak)
            {
                EditableLineBreak elinebreak = new();
                returnList.Add(elinebreak);
            }

            else if (domelm is RTFDomImage rtfImage)
            {
                EditableInlineUIContainer eIUC = new(null!)
                {
                    FontFamily = "Image" //???
                };

                Avalonia.Controls.Image img = new()
                {
                    Stretch = Stretch.Fill
                };

                // Loaded RTF is external input (stored documents, clipboard of other programs): a picture group
                // without data or in a format Avalonia cannot decode must not abort loading the whole document.
                // The image then stays without a source.
                Bitmap? imageSource = null;
                if (rtfImage.Data is byte[] imageData && imageData.Length > 0)
                {
                    try
                    {
                        MemoryStream memStream = new(imageData) { Position = 0 };
                        imageSource = new Bitmap(memStream);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("image data could not be decoded: " + ex.Message);
                    }
                }
                img.Source = imageSource;

                // \picwgoal/\pichgoal is the display size. If it is missing or 0 the bitmap size applies;
                // otherwise the loaded image would get width and height 0 and stay invisible.
                double goalWidth = TwipToPix(rtfImage.Width);
                double goalHeight = TwipToPix(rtfImage.Height);
                img.Width = goalWidth > 0 ? goalWidth : imageSource?.Size.Width ?? 0;
                img.Height = goalHeight > 0 ? goalHeight : imageSource?.Size.Height ?? 0;

                eIUC.SetChild(img);
                returnList.Add(eIUC);
            }

            else if (domelm is RTFDomText rtftext2)
            {
                returnList.Add(GetRun(rtftext2));
            }
            else
            {
                Debug.WriteLine("unknown element: " + domelm.GetType());
            }
        }

        return returnList;
    }

    internal static EditableHyperlink GetRtfField(RTFDomField rtfField)
    {
        EditableHyperlink newEHL = null!;

        foreach (RTFDomElement elem in rtfField.Elements)
        {
            switch (elem)
            {
                case RTFDomElementContainer rtfCont:

                    if (rtfCont.Name == "fldinst")
                    {
                        string uriString = elem.InnerText;
                        int tooltipIndex = uriString.IndexOf(@"\o");
                        if (tooltipIndex >= 0)
                            uriString = uriString[..tooltipIndex];
                        uriString = uriString.Replace("HYPERLINK ", "").Replace("\"", "").Trim();
                        //Debug.WriteLine("uriString = " + uriString);
                        newEHL = new("", uriString);
                    }
                    else if (rtfCont.Name == "fldrslt")
                    {
                        if (elem.Elements[0] is RTFDomText rtfDomTxt)
                        {
                            EditableRun linkRun = GetRun(rtfDomTxt);
                            FlowDocument.CopyRunPropsToHyperlinkText(linkRun, ref newEHL);
                        }
                    }

                    break;
            }
        }

        return newEHL;
    }

    private static EditableRun GetRun(RTFDomText rtfDomTxt)
    {
        EditableRun erun = new(rtfDomTxt.Text)
        {
            FontSize = PointsToPixels(rtfDomTxt.Format.FontSize)
        };

        if (rtfDomTxt.Format.Bold)
            erun.FontWeight = FontWeight.Bold;

        if (rtfDomTxt.Format.Italic)
            erun.FontStyle = FontStyle.Italic;


        // rtf inherently does not support overline
        if (rtfDomTxt.Format.Underline)
        {
            erun.TextDecorations ??= [];
            erun.TextDecorations.Add(new() { Location = TextDecorationLocation.Underline });
        }
        if (rtfDomTxt.Format.Strikeout)
        {
            erun.TextDecorations ??= [];
            erun.TextDecorations.Add(new() { Location = TextDecorationLocation.Strikethrough });
        }


        if (rtfDomTxt.Format.Subscript)
            erun.BaselineAlignment = BaselineAlignment.Subscript;

        if (rtfDomTxt.Format.Superscript)
            erun.BaselineAlignment = BaselineAlignment.Superscript;

        erun.Foreground = new SolidColorBrush(rtfDomTxt.Format.TextColor);
        erun.Background = new SolidColorBrush(rtfDomTxt.Format.BackColor);
        erun.FontFamily = new FontFamily(rtfDomTxt.Format.FontName);
        //linkRun.FontFamily = new FontFamily("Meiryo");
        //Debug.WriteLine("linkRun: " + linkRun.FontFamily + "  (" + linkRun.GetText + ")");

        return erun;
    }

    // Mark around the content of \pntext/\listtext (U+2063, invisible separator). These groups carry the finished
    // list marker of the paragraph ("·" + tab, "1." + tab); the DOM parser returns it as text.
    private const char ListTextMark = (char)0x2063;

    private static readonly string ListTextMarkRtf = $@"{{\u{(int)ListTextMark}?}}";

    // Prepares RTF for the DOM parser. {\*\pn...} (list definition, ignorable destination) is dropped because the
    // parser would otherwise write its \pntxtb/\pntxta text into the paragraph; \pntext and \listtext are marked at
    // start and end. \listtext (Word lists) carries the same as \pntext and is passed on as \pntext; the parser
    // would otherwise append a space after the group.
    internal static string PrepareListText(string rtf)
    {
        StringBuilder result = new(rtf.Length);
        for (int pos = 0; pos < rtf.Length; pos++)
        {
            if (rtf[pos] == '\\' && pos + 1 < rtf.Length)
            {
                result.Append(rtf, pos, 2);
                pos++;
            }
            else if (StartsWith(rtf, pos, @"{\*\pn"))
                pos = GroupEnd(rtf, pos);
            else if (GetListTextWordLength(rtf, pos) is int wordLength and > 0)
            {
                int end = GroupEnd(rtf, pos);
                result.Append('{').Append(ListTextMarkRtf).Append(@"\pntext").Append(rtf, pos + wordLength, end - pos - wordLength).Append(ListTextMarkRtf).Append('}');
                pos = end;
            }
            else
                result.Append(rtf[pos]);
        }
        return result.ToString();
    }

    private static bool StartsWith(string rtf, int pos, string text) => string.CompareOrdinal(rtf, pos, text, 0, text.Length) == 0;

    private static bool StartsWithControlWord(string rtf, int pos, string word) =>
        StartsWith(rtf, pos, word) && (pos + word.Length >= rtf.Length || !char.IsLetter(rtf[pos + word.Length]));

    // Length of "{\pntext" or "{\listtext" at pos, otherwise 0.
    private static int GetListTextWordLength(string rtf, int pos) =>
        StartsWithControlWord(rtf, pos, @"{\pntext") ? 8 : StartsWithControlWord(rtf, pos, @"{\listtext") ? 10 : 0;

    // Index of the closing brace of the group at pos; escaped braces do not count.
    private static int GroupEnd(string rtf, int pos)
    {
        int depth = 0;
        for (; pos < rtf.Length; pos++)
        {
            switch (rtf[pos])
            {
                case '\\': pos++; break;
                case '{': depth++; break;
                case '}': if (--depth == 0) return pos; break;
            }
        }
        return rtf.Length - 1;
    }

    // Takes the marked list marker off the paragraph start and returns it without tab/spaces.
    private static string TakeListText(List<IEditable> inlines)
    {
        if (inlines.FirstOrDefault() is not EditableRun { Text: string firstText } firstRun || !firstText.StartsWith(ListTextMark))
            return "";

        firstRun.Text = firstText[1..];
        StringBuilder marker = new();
        while (inlines.FirstOrDefault() is EditableRun run)
        {
            string text = run.Text ?? "";
            int end = text.IndexOf(ListTextMark);
            string fontName = run.FontFamily.Name;
            marker.Append((end < 0 ? text : text[..end]).Select(c => MapSymbolFontChar(c, fontName)).ToArray());

            if (end >= 0 && end + 1 < text.Length)
            {
                run.Text = text[(end + 1)..];
                break;
            }
            inlines.RemoveAt(0);
            if (end >= 0)
                break;
        }
        return marker.ToString().Trim();
    }

    // Bullets of the symbol fonts as Unicode (\'B7 in Symbol is the bullet, not the middle dot).
    private static char MapSymbolFontChar(char c, string fontName) => (fontName, c) switch
    {
        ("Symbol", '·') => '•',
        ("Wingdings", '§') => '▪',
        ("Wingdings", 'l') => '●',
        ("Wingdings", 'n') => '■',
        ("Wingdings", 'Ø') => '➢',
        ("Wingdings", 'ü') => '✓',
        _ => c,
    };

    private static string DecodeRtfUnicode(string rtfText)
    {
        return RtfUnicodeRegex().Replace(rtfText, match =>
        {
            int unicodeValue = int.Parse(match.Groups[1].Value);
            return char.ConvertFromUtf32(unicodeValue);
        });
    }

    [GeneratedRegex(@"\\u(-?\d+)\?")]
    private static partial Regex RtfUnicodeRegex();

    private enum BorderType
    {
        Left, Top, Right, Bottom
    }
}


