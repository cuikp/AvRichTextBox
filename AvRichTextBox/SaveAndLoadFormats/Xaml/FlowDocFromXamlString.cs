using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

internal partial class XamlConversions
{
    // WPF lengths in XAML (FontSize, Width, Height, BorderThickness) optionally carry a unit: px (default), pt, in,
    // cm; HTML converters write CSS font sizes as "10pt". The number is culture invariant. Result in
    // device-independent pixels (96 per inch), as WPF resolves the value.
    internal static double ParseXamlLength(string value)
    {
        var text = value.Trim();
        var factor = 1.0;
        if (text.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^2];
        }
        else if (text.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
        {
            factor = 96.0 / 72.0;
            text = text[..^2];
        }
        else if (text.EndsWith("in", StringComparison.OrdinalIgnoreCase))
        {
            factor = 96.0;
            text = text[..^2];
        }
        else if (text.EndsWith("cm", StringComparison.OrdinalIgnoreCase))
        {
            factor = 96.0 / 2.54;
            text = text[..^2];
        }
        return double.Parse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture) * factor;
    }

    readonly static List<Bitmap> consecutiveImageBitmaps = [];

    [GeneratedRegex(@"Xaml/Image[0-9]{1,}\.png")]
    private static partial Regex FindXamlImageEntriesRegex();

    [GeneratedRegex(@"<Relationship Type=.*?/xaml/entry.*?/>")]
    private static partial Regex XamlRelationshipEntryRegex();

    [GeneratedRegex(@"(?<=Target="").*?(?="")")]
    private static partial Regex XamlTargetEntryRegex();

    [GeneratedRegex("(?<=Image)[0-9]{1,}")]
    private static partial Regex ImageNoMatchRegex();

    internal static async Task<bool> LoadXamlPackage(string fileName, FlowDocument fdoc)
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
        catch (Exception ex) { Debug.WriteLine($"Error loading Xaml Package: {ex.Message}"); return false; }

        return true;

    }

    internal static void ProcessXamlString(string docXamlString, FlowDocument fdoc)
    {  //Debug.WriteLine("xaml:\n" + docXamlString);

        fdoc.Blocks.Clear();
        foreach (Block block in GetBlocksFromXamlString(docXamlString, fdoc))
            fdoc.Blocks.Add(block);
    }

    // Indent per list level and hanging first-line indent of the marker, as RichEdit stores a WPF list
    // (\li720, nested \li1440, \fi-360).
    private const double ListLevelIndentTwips = 720;
    private const double ListMarkerIndentTwips = 360;

    // Inheritable TextElement properties: WPF passes them from Section, List, ListItem, Paragraph and Span on to
    // the runs, so a paragraph with FontWeight="bold" makes its text bold.
    private static readonly HashSet<string> InheritedAttributeNames =
        ["FontFamily", "FontSize", "FontWeight", "FontStyle", "FontStretch", "Foreground", "TextDecorations", "TextAlignment"];

    internal static List<Block> GetBlocksFromXamlString(string xamlString, FlowDocument fdoc)
    {
        List<Block> blocks = [];
        XmlDocument xamlDocument = new();
        xamlDocument.LoadXml(xamlString);
        if (xamlDocument.ChildNodes.Count == 1 && xamlDocument.ChildNodes[0] is XmlNode sectionNode && sectionNode.Name == "Section")
            AddBlocks(sectionNode, blocks, fdoc, Inherit([], sectionNode), 0);
        return blocks;
    }

    private static void AddBlocks(XmlNode containerNode, List<Block> blocks, FlowDocument fdoc, Dictionary<string, string> inherited, int listLevel)
    {
        foreach (XmlNode blockNode in containerNode.ChildNodes.OfType<XmlNode>())
        {
            switch (blockNode.Name)
            {
                case "Paragraph":
                    blocks.Add(GetParagraph(blockNode, fdoc, inherited, listLevel));
                    break;

                case "Table":
                    blocks.Add(GetTable(blockNode, fdoc));
                    break;

                case "Section":
                    AddBlocks(blockNode, blocks, fdoc, Inherit(inherited, blockNode), listLevel);
                    break;

                case "List":
                    AddList(blockNode, blocks, fdoc, Inherit(inherited, blockNode), listLevel + 1);
                    break;
            }
        }
    }

    // Each ListItem becomes its paragraphs; the first one carries the marker.
    private static void AddList(XmlNode listNode, List<Block> blocks, FlowDocument fdoc, Dictionary<string, string> inherited, int listLevel)
    {
        string markerStyle = listNode.Attributes?["MarkerStyle"]?.Value ?? "Disc";
        int index = int.TryParse(listNode.Attributes?["StartIndex"]?.Value, out int startIndex) ? startIndex : 1;

        foreach (XmlNode itemNode in listNode.ChildNodes.OfType<XmlNode>().Where(n => n.Name == "ListItem"))
        {
            int firstItemBlock = blocks.Count;
            AddBlocks(itemNode, blocks, fdoc, Inherit(inherited, itemNode), listLevel);
            if (firstItemBlock < blocks.Count && blocks[firstItemBlock] is Paragraph { HasListMarker: false } itemPar)
            {
                itemPar.ListMarker = ListMarkerText(markerStyle, index);
                itemPar.FirstLineIndent = -TwipToPix(ListMarkerIndentTwips);
            }
            index++;
        }
    }

    private static string ListMarkerText(string markerStyle, int index) => markerStyle switch
    {
        "None" => "",
        "Circle" => "○",
        "Square" => "■",
        "Box" => "□",
        "Decimal" => $"{index}.",
        "LowerLatin" => $"{(char)('a' + (index - 1) % 26)}.",
        "UpperLatin" => $"{(char)('A' + (index - 1) % 26)}.",
        "LowerRoman" => $"{ToRoman(index).ToLowerInvariant()}.",
        "UpperRoman" => $"{ToRoman(index)}.",
        _ => "•",
    };

    private static string ToRoman(int number)
    {
        (int Value, string Digits)[] romanDigits = [(1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"), (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")];
        StringBuilder roman = new();
        foreach ((int value, string digits) in romanDigits)
            for (; number >= value; number -= value)
                roman.Append(digits);
        return roman.ToString();
    }

    // Inherited properties plus the inheritable attributes of the node (its own override inherited ones).
    private static Dictionary<string, string> Inherit(Dictionary<string, string> inherited, XmlNode node, string? impliedName = null, string? impliedValue = null)
    {
        Dictionary<string, string> result = new(inherited);
        if (impliedName != null)
            result[impliedName] = impliedValue!;
        foreach (XmlAttribute att in node.Attributes?.OfType<XmlAttribute>() ?? [])
            if (InheritedAttributeNames.Contains(att.Name))
                result[att.Name] = att.Value;
        return result;
    }

    // All attributes that apply to the node: inherited ones it does not set itself, then its own.
    private static IEnumerable<(string Name, string Value)> EffectiveAttributes(Dictionary<string, string> inherited, XmlNode? node)
    {
        Dictionary<string, string> own = node?.Attributes?.OfType<XmlAttribute>().ToDictionary(a => a.Name, a => a.Value) ?? [];
        foreach (var kv in inherited.Where(kv => !own.ContainsKey(kv.Key)))
            yield return (kv.Key, kv.Value);
        foreach (var kv in own)
            yield return (kv.Key, kv.Value);
    }


    internal static Table GetTable(XmlNode tableNode, FlowDocument fdoc)
    {
        Table newTable = new();

        foreach (XmlAttribute xmlatt in tableNode.Attributes!.OfType<XmlAttribute>())
        {
            switch (xmlatt.Name)
            {
                case "CellSpacing":
                    break;
                case "Margin":
                    newTable.Margin = new();
                    break;
                case "Padding":

                    break;

                case "TableAlignment":
                    switch (xmlatt.Value)
                    {
                        case "Center":
                            newTable.TableAlignment = HorizontalAlignment.Center;
                            break;
                        case "Left":
                            newTable.TableAlignment = HorizontalAlignment.Left;
                            break;
                        case "Right":
                            newTable.TableAlignment = HorizontalAlignment.Right;
                            break;
                    }
                    break;

                case "BorderThickness":
                    string[] vals = xmlatt.Value.Split(',', StringSplitOptions.TrimEntries);
                    if (vals.Length == 4)
                        newTable.BorderThickness = new(ParseXamlLength(vals[0]), ParseXamlLength(vals[1]), ParseXamlLength(vals[2]), ParseXamlLength(vals[3]));
                    else if(vals.Length > 0)
                        newTable.BorderThickness = new(ParseXamlLength(vals[0]));
                    break;

                case "BorderBrush":
                    newTable.BorderBrush = new SolidColorBrush(Color.Parse(xmlatt.Value));
                    break;
            }
        }

        foreach (XmlNode inlineNode in tableNode.ChildNodes.OfType<XmlNode>())
        {
            switch (inlineNode.Name)
            {
                case "Table.Columns":
                    foreach (XmlNode coldefNode in inlineNode.ChildNodes.OfType<XmlNode>())
                    {
                        if (coldefNode.Name == "TableColumn" && coldefNode.Attributes is XmlAttributeCollection atts)
                        {
                            foreach (XmlAttribute xmlatt in atts.OfType<XmlAttribute>().Where(widthAtt => widthAtt.Name == "Width"))
                            {
                                double colWidth = ParseXamlLength(xmlatt.Value);
                                newTable.ColDefs.Add(new ColumnDefinition(colWidth, GridUnitType.Pixel));
                            }
                        }
                    }
                    break;

                case "TableRowGroup":

                    int[] firstAvailableRow = [.. Enumerable.Repeat(0, newTable.ColDefs.Count)];

                    int rowno = 0;
                    foreach (XmlNode rowNode in inlineNode.ChildNodes.OfType<XmlNode>().Where(n => n.Name == "TableRow"))
                    {

                        newTable.RowDefs.Add(new RowDefinition());

                        int colno = 0;

                        foreach (XmlNode cellNode in rowNode.ChildNodes.OfType<XmlNode>().Where(n => n.Name == "TableCell"))
                        {
                            Cell newCell = new()
                            {
                                ColSpan = 1,
                                RowSpan = 1,
                            };

                            if (cellNode.Attributes is XmlAttributeCollection atts)
                            {
                                foreach (XmlAttribute xmlatt in atts.OfType<XmlAttribute>())
                                {
                                    switch (xmlatt.Name)
                                    {
                                        case "ColumnSpan": newCell.ColSpan = Int32.Parse(xmlatt.Value); break;
                                        case "RowSpan": newCell.RowSpan = Int32.Parse(xmlatt.Value); break;
                                        case "Padding": newCell.Padding = Thickness.Parse(xmlatt.Value); break;
                                        case "BorderThickness": newCell.BorderThickness = Thickness.Parse(xmlatt.Value); break;

                                        case "BorderBrush":
                                            if (xmlatt.Value is string bbS)
                                                newCell.BorderBrush = bbS == "" ? Brushes.Black : new SolidColorBrush(Color.Parse(bbS));
                                            break;

                                        case "Background":
                                            if (xmlatt.Value is string cbS)
                                                newCell.CellBackground = cbS == "" ? Brushes.Transparent : new SolidColorBrush(Color.Parse(cbS));
                                            break;

                                        case "VerticalAlignment":
                                            switch (xmlatt.Value)
                                            {
                                                case "Top": newCell.CellVerticalAlignment = VerticalAlignment.Top; break;
                                                case "Center": newCell.CellVerticalAlignment = VerticalAlignment.Center; break;
                                                case "Bottom": newCell.CellVerticalAlignment = VerticalAlignment.Bottom; break;
                                            }
                                            break;
                                    }
                                         
                                }
                            }

                            foreach (XmlNode xmlNode in cellNode.ChildNodes.OfType<XmlNode>())
                            {
                                switch (xmlNode.Name)
                                {
                                    case "Paragraph":
                                        Paragraph getNewPar = GetParagraph(xmlNode, fdoc);
                                        newCell.CellBlocks.Add(getNewPar);
                                        break;
                                    case "Table":
                                        Table getNewTable = GetTable(xmlNode, fdoc);
                                        newCell.CellBlocks.Add(getNewTable);
                                        break;
                                }
                            }

                            while (rowno < firstAvailableRow[colno])
                            {
                                colno++;
                            }

                            for (int cspan = 0; cspan < newCell.ColSpan; cspan++)
                            {
                                int lastSpannedCol = colno + cspan;
                                if (lastSpannedCol < firstAvailableRow.Length)
                                    firstAvailableRow[lastSpannedCol] = rowno + newCell.RowSpan;
                            }

                            newCell.RowNo = rowno;
                            newCell.ColNo = colno;
                            newTable.Cells.Add(newCell);
                            colno += newCell.ColSpan;
                        }

                        rowno++;
                    }

                    break;
            }
        }


        return newTable;

    }


    internal static Paragraph GetParagraph(XmlNode parNode, FlowDocument fdoc) => GetParagraph(parNode, fdoc, [], 0);

    internal static Paragraph GetParagraph(XmlNode parNode, FlowDocument fdoc, Dictionary<string, string> inherited, int listLevel)
    {
        Paragraph newPar = new()
        {
            LeftIndent = TwipToPix(listLevel * ListLevelIndentTwips)
        };

        foreach ((string name, string value) in EffectiveAttributes(inherited, parNode))
        {
            switch (name)
            {
                case "TextAlignment":
                    switch (value)
                    {
                        case "Left": newPar.TextAlignment = TextAlignment.Left; break;
                        case "Justify": newPar.TextAlignment = TextAlignment.Justify; break;
                        case "Right": newPar.TextAlignment = TextAlignment.Right; break;
                        case "Center": newPar.TextAlignment = TextAlignment.Center; break;
                    }
                    break;

                case "FontFamily":
                    newPar.FontFamily = new FontFamily(value);
                    break;

                case "FontSize":
                    newPar.FontSize = ParseXamlLength(value);
                    break;

                case "FontWeight":
                    if (Enum.TryParse<FontWeight>(value, true, out FontWeight parWeight))
                        newPar.FontWeight = parWeight;
                    break;

                case "FontStyle":
                    if (Enum.TryParse<FontStyle>(value, true, out FontStyle parStyle))
                        newPar.FontStyle = parStyle;
                    break;

                case "Background":
                    newPar.Background = new SolidColorBrush(Color.Parse(value));
                    break;

                case "Margin":
                    newPar.Margin = Thickness.Parse(value);
                    break;
            }
        }

        AddInlines(parNode, newPar, Inherit(inherited, parNode));

        newPar.EnsureEmptyRuns();

        return newPar;
    }

    // Inline content of a paragraph or span. Span, Bold, Italic and Underline carry only formatting that their
    // runs inherit; their text is taken flat into the paragraph.
    private static void AddInlines(XmlNode containerNode, Paragraph par, Dictionary<string, string> inherited)
    {
        foreach (XmlNode inlineNode in containerNode.ChildNodes.OfType<XmlNode>())
        {
            switch (inlineNode.Name)
            {
                // Text directly in the paragraph without <Run> (WPF: implicit run with the paragraph format). HTML
                // converters produce it for text before/between formatted passages and for plain text.
                case "#text":
                case "#cdata-section":
                    par.Inlines.Add(CreateRun(inlineNode.Value ?? "", inherited));
                    break;

                case "Run":
                    par.Inlines.Add(CreateRun(inlineNode.InnerText, inherited, inlineNode));
                    break;

                case "Span":
                    AddInlines(inlineNode, par, Inherit(inherited, inlineNode));
                    break;

                case "Bold":
                    AddInlines(inlineNode, par, Inherit(inherited, inlineNode, "FontWeight", "Bold"));
                    break;

                case "Italic":
                    AddInlines(inlineNode, par, Inherit(inherited, inlineNode, "FontStyle", "Italic"));
                    break;

                case "Underline":
                    AddInlines(inlineNode, par, Inherit(inherited, inlineNode, "TextDecorations", "Underline"));
                    break;

                case "Hyperlink":
                    par.Inlines.Add(GetHyperlink(inlineNode, Inherit(inherited, inlineNode)));
                    break;

                case "LineBreak":
                    par.Inlines.Add(new EditableLineBreak());
                    break;

                case "InlineUIContainer":
                    par.Inlines.Add(GetInlineUIContainer(inlineNode));
                    break;

                default:
                    Debug.WriteLine("unknown par xmlnode: " + inlineNode.Name);
                    break;
            }
        }
    }

    private static EditableHyperlink GetHyperlink(XmlNode linkNode, Dictionary<string, string> inherited)
    {
        EditableHyperlink elink = new();
        if (linkNode.Attributes?["NavigateUri"] is XmlAttribute uriAtt)
            elink.NavigateUri = uriAtt.Value;

        foreach (XmlNode runNode in linkNode.ChildNodes.OfType<XmlNode>().Where(n => n.Name == "Run"))
        {
            EditableRun linkRun = CreateRun(runNode.InnerText, inherited, runNode);
            FlowDocument.CopyRunPropsToHyperlinkText(linkRun, ref elink);
        }

        return elink;
    }

    private static EditableInlineUIContainer GetInlineUIContainer(XmlNode containerNode)
    {
        EditableInlineUIContainer eIUC = new(null!);

        if (containerNode.Attributes?["FontFamily"] is XmlAttribute fontAtt)
            eIUC.FontFamily = new FontFamily(fontAtt.Value);

        if (containerNode.ChildNodes.Count != 1 || containerNode.ChildNodes[0] is not XmlNode controlNode || controlNode.Name != "Image")
            return eIUC;

        Image img = new();
        foreach (XmlAttribute attC in controlNode.Attributes!)
        {
            switch (attC.Name)
            {
                case "Width":
                    img.Width = ParseXamlLength(attC.Value);
                    break;

                case "Height":
                    img.Height = ParseXamlLength(attC.Value);
                    break;

                case "Stretch":
                    img.Stretch = Stretch.Fill; // leave fixed for now
                    break;
            }
        }

        if (controlNode.ChildNodes.Count == 1 && controlNode.ChildNodes[0] is XmlNode sourceNode && sourceNode.Name == "Image.Source"
            && sourceNode.ChildNodes.Count == 1 && sourceNode.ChildNodes[0] is XmlNode bitmapNode && bitmapNode.Name == "BitmapImage"
            && bitmapNode.Attributes?.OfType<XmlAttribute>().FirstOrDefault(batt => batt.Name == "UriSource") is XmlAttribute uriSourceAtt)
        {
            Match imgNoMatch = ImageNoMatchRegex().Match(uriSourceAtt.Value);
            if (imgNoMatch.Success)
                img.Source = consecutiveImageBitmaps[int.Parse(imgNoMatch.Value) - 1];
        }

        eIUC.SetChild(img);
        return eIUC;
    }

    internal static EditableRun GetRun(XmlNode inlineNode) => CreateRun(inlineNode.InnerText, [], inlineNode);

    private static EditableRun CreateRun(string text, Dictionary<string, string> inherited, XmlNode? runNode = null)
    {
        EditableRun erun = new(text);
        foreach ((string name, string value) in EffectiveAttributes(inherited, runNode))
            ApplyRunAttribute(erun, name, value);
        return erun;
    }

    private static void ApplyRunAttribute(EditableRun erun, string name, string value)
    {
        switch (name)
        {
            case "FontFamily":
                // "$Default" inherits the paragraph font.
                if (value != "$Default")
                    erun.FontFamily = new FontFamily(value);
                break;

            case "FontWeight":
                // WPF XAML reads the values case-insensitively; HTML converters write CSS values in lower case ("bold").
                if (Enum.TryParse<FontWeight>(value, true, out FontWeight runWeight))
                    erun.FontWeight = runWeight;
                break;

            case "FontSize":
                erun.FontSize = ParseXamlLength(value);
                break;

            case "FontStyle":
                if (Enum.TryParse<FontStyle>(value, true, out FontStyle runStyle))
                    erun.FontStyle = runStyle;
                break;

            case "TextDecorations":
                erun.TextDecorations = [];
                foreach (string tDecRaw in value.Split(','))
                {
                    switch (tDecRaw.Trim())
                    {
                        case "Underline": erun.TextDecorations.Add(new() { Location = TextDecorationLocation.Underline }); break;
                        case "Overline": erun.TextDecorations.Add(new() { Location = TextDecorationLocation.Overline }); break;
                        case "Baseline": erun.TextDecorations.Add(new() { Location = TextDecorationLocation.Baseline }); break;
                        case "Strikethrough": erun.TextDecorations.Add(new() { Location = TextDecorationLocation.Strikethrough }); break;
                    }
                }
                break;

            case "Foreground":
                erun.Foreground = new SolidColorBrush(Color.Parse(value));
                break;

            case "Background":
                erun.Background = new SolidColorBrush(Color.Parse(value));
                break;

            case "FontStretch":
                if (Enum.TryParse<FontStretch>(value, true, out FontStretch runStretch))
                    erun.FontStretch = runStretch;
                break;

            case "BaselineAlignment":
                if (Enum.TryParse<BaselineAlignment>(value, true, out BaselineAlignment runAlignment))
                    erun.BaselineAlignment = runAlignment;
                break;
        }
    }

}


