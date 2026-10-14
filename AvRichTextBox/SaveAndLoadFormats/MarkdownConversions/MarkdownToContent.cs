using Avalonia.Controls;
using Avalonia.Media;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Globalization;
using MarkdownTable = Markdig.Extensions.Tables.Table;

namespace AvRichTextBox;

internal static partial class MarkdownConversions
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
        .DisableHtml()
        .Build();

    internal static List<Block> Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        List<Block> blocks = [];
        AddBlocks(Markdown.Parse(markdown, Pipeline), blocks, new());
        return blocks;
    }

    private static void AddBlocks(ContainerBlock source, List<Block> target, MarkdownBlockInfo info)
    {
        foreach (var block in source)
        {
            switch (block)
            {
                case ListBlock list:
                    int number = int.TryParse(list.OrderedStart, NumberStyles.None, CultureInfo.InvariantCulture, out int start) ? start : 1;
                    foreach (ListItemBlock item in list)
                    {
                        List<Block> itemBlocks = [];
                        AddBlocks(item, itemBlocks, info with { ListIndent = info.ListIndent + 4 });
                        if (itemBlocks.FirstOrDefault() is Paragraph first)
                        {
                            first.ListMarker = list.IsOrdered ? number.ToString(CultureInfo.InvariantCulture) + "." : "•";
                            first.MarkdownInfo = first.MarkdownInfo! with { ListIndent = info.ListIndent };
                            first.LeftIndent = info.ListIndent * 8 + 24;
                            first.FirstLineIndent = -24;
                        }
                        target.AddRange(itemBlocks);
                        number++;
                    }
                    break;
                case QuoteBlock quote:
                    AddBlocks(quote, target, info with { QuoteDepth = info.QuoteDepth + 1 });
                    break;
                case MarkdownTable table:
                    target.Add(ReadTable(table, info));
                    break;
                case CodeBlock code:
                    var codeParagraph = NewParagraph(info with
                    {
                        IsCodeBlock = true,
                        CodeLanguage = code is FencedCodeBlock fenced ? fenced.Info ?? "" : ""
                    });
                    codeParagraph.FontFamily = new FontFamily("monospace");
                    codeParagraph.Inlines.Add(new EditableRun(code.Lines.ToString())
                    {
                        FontFamily = codeParagraph.FontFamily
                    });
                    target.Add(codeParagraph);
                    break;
                case ThematicBreakBlock:
                    var rule = NewParagraph(info with { IsThematicBreak = true });
                    rule.BorderThickness = new Thickness(0, 0, 0, 1);
                    rule.Inlines.Add(new EditableRun(""));
                    target.Add(rule);
                    break;
                case LeafBlock leaf:
                    int headingLevel = leaf is HeadingBlock heading ? heading.Level : 0;
                    var paragraph = NewParagraph(info with { HeadingLevel = headingLevel });
                    if (headingLevel > 0)
                    {
                        paragraph.FontSize = 16 + (7 - headingLevel) * 4;
                        paragraph.FontWeight = FontWeight.Bold;
                    }
                    AddInlines(leaf.Inline, paragraph, new(Bold: headingLevel > 0, FontSize: paragraph.FontSize));
                    paragraph.EnsureEmptyRuns();
                    target.Add(paragraph);
                    break;
                case ContainerBlock container:
                    AddBlocks(container, target, info);
                    break;
            }
        }
    }

    private static Paragraph NewParagraph(MarkdownBlockInfo info) => new()
    {
        MarkdownInfo = info,
        LeftIndent = info.QuoteDepth * 24 + info.ListIndent * 8,
        BorderThickness = info.QuoteDepth > 0 ? new Thickness(2, 0, 0, 0) : new Thickness(0)
    };

    private sealed record InlineFormat(bool Bold = false, bool Italic = false, bool Strike = false, double FontSize = 16);

    private static void AddInlines(ContainerInline? source, Paragraph target, InlineFormat format, LinkInline? link = null)
    {
        if (source == null) return;
        foreach (var inline in source)
        {
            switch (inline)
            {
                case EmphasisInline emphasis:
                    AddInlines(emphasis, target, format with
                    {
                        Bold = format.Bold || (emphasis.DelimiterChar != '~' && emphasis.DelimiterCount >= 2),
                        Italic = format.Italic || (emphasis.DelimiterChar != '~' && emphasis.DelimiterCount % 2 == 1),
                        Strike = format.Strike || emphasis.DelimiterChar == '~'
                    }, link);
                    break;
                case LinkInline childLink:
                    if (childLink.IsImage)
                        AddRun(PlainText(childLink), target, format, childLink.Url, new(IsImage: true, Title: childLink.Title ?? ""));
                    else if (childLink.FirstChild == null)
                        AddRun("", target, format, childLink.Url, new(IsImage: childLink.IsImage, Title: childLink.Title ?? ""));
                    else
                        AddInlines(childLink, target, format, childLink);
                    break;
                case AutolinkInline autoLink:
                    AddRun(autoLink.Url, target, format, autoLink.IsEmail ? "mailto:" + autoLink.Url : autoLink.Url);
                    break;
                case LiteralInline literal:
                    AddRun(literal.Content.ToString(), target, format, link?.Url, new(IsImage: link?.IsImage == true, Title: link?.Title ?? ""));
                    break;
                case HtmlEntityInline entity:
                    AddRun(entity.Transcoded.ToString(), target, format, link?.Url, new(IsImage: link?.IsImage == true, Title: link?.Title ?? ""));
                    break;
                case CodeInline code:
                    AddRun(code.Content, target, format, link?.Url, new(IsCode: true, IsImage: link?.IsImage == true, Title: link?.Title ?? ""));
                    break;
                case LineBreakInline lineBreak:
                    if (lineBreak.IsHard) target.Inlines.Add(new EditableLineBreak());
                    else AddRun(" ", target, format, link?.Url);
                    break;
                case ContainerInline container:
                    AddInlines(container, target, format, link);
                    break;
            }
        }
    }

    private static string PlainText(ContainerInline container) => string.Concat(container.Select(inline => inline switch
    {
        LiteralInline literal => literal.Content.ToString(),
        HtmlEntityInline entity => entity.Transcoded.ToString(),
        CodeInline code => code.Content,
        LineBreakInline => " ",
        ContainerInline child => PlainText(child),
        _ => ""
    }));

    private static void AddRun(string text, Paragraph target, InlineFormat format, string? url = null, MarkdownInlineInfo? info = null)
    {
        EditableRun run = url == null ? new EditableRun(text) : new EditableHyperlink(text, url);
        run.MarkdownInfo = info;
        run.FontSize = format.FontSize;
        run.FontWeight = format.Bold ? FontWeight.Bold : FontWeight.Normal;
        run.FontStyle = format.Italic ? FontStyle.Italic : FontStyle.Normal;
        if (format.Strike && run is not EditableHyperlink)
            run.TextDecorations = new TextDecorationCollection { new TextDecoration { Location = TextDecorationLocation.Strikethrough } };
        if (info?.IsCode == true) run.FontFamily = new FontFamily("monospace");
        target.Inlines.Add(run);
    }

    private static Table ReadTable(MarkdownTable source, MarkdownBlockInfo info)
    {
        var rows = source.OfType<TableRow>().ToList();
        int columns = rows.Max(row => row.Count);
        var table = new Table { MarkdownInfo = info };
        for (int column = 0; column < columns; column++) table.ColDefs.Add(new ColumnDefinition(150, GridUnitType.Pixel));
        for (int row = 0; row < rows.Count; row++) table.RowDefs.Add(new RowDefinition());
        for (int row = 0; row < rows.Count; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                var cell = new Cell { RowNo = row, ColNo = column };
                List<Block> blocks = [];
                if (column < rows[row].Count)
                    AddBlocks((TableCell)rows[row][column], blocks, new());
                if (blocks.Count == 0) blocks.Add(NewParagraph(new()));
                foreach (var paragraph in blocks.OfType<Paragraph>())
                {
                    paragraph.EnsureEmptyRuns();
                    if (column < source.ColumnDefinitions.Count)
                        paragraph.TextAlignment = source.ColumnDefinitions[column].Alignment switch
                        {
                            TableColumnAlign.Center => TextAlignment.Center,
                            TableColumnAlign.Right => TextAlignment.Right,
                            _ => TextAlignment.Left
                        };
                    if (rows[row].IsHeader)
                        foreach (var run in paragraph.Inlines.OfType<EditableRun>()) run.FontWeight = FontWeight.Bold;
                }
                foreach (var block in blocks) cell.CellBlocks.Add(block);
                table.Cells.Add(cell);
            }
        }
        return table;
    }
}
