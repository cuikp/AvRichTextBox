using Avalonia.Media;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AvRichTextBox;

internal static partial class MarkdownConversions
{
    internal static string Write(IEnumerable<Block> blocks)
    {
        StringBuilder result = new();
        int previousQuoteDepth = 0;
        Block? previous = null;
        Dictionary<int, ListItemKind> lastItems = [];
        foreach (var block in blocks)
        {
            int quoteDepth = block.MarkdownInfo?.QuoteDepth ?? 0;
            bool tight = ContinuesTightList(block, previous, lastItems);
            if (previous != null)
            {
                result.Append('\n');
                if (!tight)
                    result.Append(string.Concat(Enumerable.Repeat("> ", Math.Min(previousQuoteDepth, quoteDepth)))).Append('\n');
            }
            result.Append(WriteBlock(block));
            previousQuoteDepth = quoteDepth;
            previous = block;
        }
        return result.ToString();
    }

    private readonly record struct ListItemKind(bool Ordered, int Number);

    /// <summary>
    /// Items of one tight list follow each other without a blank line. An item continues the list of the last item at
    /// its level when both are bullets, or numbered with the next number; the first item of a nested list continues its
    /// parent item. Loose lists and anything after text inside an item keep the blank line.
    /// </summary>
    private static bool ContinuesTightList(Block block, Block? previous, Dictionary<int, ListItemKind> lastItems)
    {
        if (block is not Paragraph { HasListMarker: true } item)
        {
            // Text inside a list item keeps the list context; any other block ends all lists.
            if ((block.MarkdownInfo?.ListIndent ?? 0) == 0)
                lastItems.Clear();
            return false;
        }

        int level = item.MarkdownInfo?.ListIndent ?? 0;
        int number = 0;
        bool ordered = item.ListMarker.EndsWith('.') && int.TryParse(item.ListMarker[..^1], NumberStyles.None, CultureInfo.InvariantCulture, out number);
        foreach (int deeperLevel in lastItems.Keys.Where(key => key > level).ToList())
            lastItems.Remove(deeperLevel);

        bool continues = lastItems.TryGetValue(level, out var last)
            ? last.Ordered == ordered && (!ordered || number == last.Number + 1)
            : previous is Paragraph { HasListMarker: true } parent && (parent.MarkdownInfo?.ListIndent ?? 0) < level;
        lastItems[level] = new(ordered, number);

        return continues && item.MarkdownInfo?.LooseList != true && previous is Paragraph { HasListMarker: true };
    }

    private static string WriteBlock(Block block)
    {
        var info = block.MarkdownInfo ?? new();
        string content;
        if (block is Table table)
        {
            content = WriteTable(table);
        }
        else if (block is Paragraph paragraph)
        {
            if (info.IsCodeBlock)
            {
                string code = string.Concat(paragraph.Inlines.Select(inline => inline is EditableLineBreak ? "\n" : inline.InlineText));
                string fence = new('`', Math.Max(3, LongestBackticks(code) + 1));
                string language = info.CodeLanguage.Split(['\r', '\n', '`'])[0];
                content = fence + language + "\n" + code + "\n" + fence;
            }
            else if (info.IsThematicBreak && paragraph.Inlines.All(inline => inline.InlineLength == 0))
                content = "---";
            else if (info.HeadingLevel > 0)
            {
                // Heading text is bold by definition, and a trailing '#' run would be read as a closing sequence.
                string text = TrailingHashes().Replace(WriteInlines(paragraph.Inlines, implicitBold: true), "\\$0");
                content = new string('#', info.HeadingLevel) + " " + text;
            }
            else
                content = WriteInlines(paragraph.Inlines);

            if (paragraph.HasListMarker)
            {
                string marker = paragraph.ListMarker.EndsWith('.') && int.TryParse(paragraph.ListMarker[..^1], out _) ? paragraph.ListMarker : "-";
                var lines = content.Split('\n');
                content = marker + " " + lines[0] + string.Concat(lines.Skip(1).Select(line => "\n" + new string(' ', marker.Length + 1) + line));
            }
        }
        else return "";

        string prefix = string.Concat(Enumerable.Repeat("> ", info.QuoteDepth)) + new string(' ', info.ListIndent);
        return string.Join("\n", content.Split('\n').Select(line => prefix + line));
    }

    private sealed record RunFormat(bool Bold, bool Italic, bool Strike, MarkdownInlineInfo? Info, string? Url);

    // implicitBold: the block is bold by definition (heading, table header row), so bold runs need no markers.
    internal static string WriteInlines(IEnumerable<IEditable> inlines, bool implicitBold = false)
    {
        StringBuilder result = new();
        StringBuilder text = new();
        RunFormat? previous = null;
        void Flush()
        {
            if (previous != null) result.Append(WriteRun(text.ToString(), previous));
            text.Clear();
        }

        foreach (var inline in inlines)
        {
            if (inline is EditableRun run)
            {
                var format = new RunFormat(!implicitBold && run.FontWeight >= FontWeight.Bold, run.FontStyle == FontStyle.Italic,
                    run.TextDecorations?.Any(decoration => decoration.Location == TextDecorationLocation.Strikethrough) == true,
                    run.MarkdownInfo, (run as EditableHyperlink)?.NavigateUri);
                if (format != previous) { Flush(); previous = format; }
                text.Append(run.InlineText);
            }
            else
            {
                Flush();
                previous = null;
                if (inline is EditableLineBreak) result.Append("  \n");
                // Embedded UI controls have no Markdown representation.
            }
        }
        Flush();
        string written = EdgeWhitespace().Replace(result.ToString(), match => string.Concat(match.Value.Select(character => character == '\t' ? "&#9;" : "&#32;")));
        return LineStartMarker().Replace(written, "$1\\$2");
    }

    private static string WriteRun(string text, RunFormat format)
    {
        if (text.Length == 0 && format.Url == null) return "";
        string content;
        if (format.Info?.IsCode == true)
        {
            string ticks = new('`', LongestBackticks(text) + 1);
            bool pad = text.StartsWith('`') || text.EndsWith('`') || (text.StartsWith(' ') && text.EndsWith(' ') && text.Trim().Length > 0);
            content = ticks + (pad ? " " : "") + text.Replace('\n', ' ') + (pad ? " " : "") + ticks;
        }
        else
        {
            string core = text.Trim();
            int leading = text.Length - text.TrimStart().Length;
            int trailing = text.Length - text.TrimEnd().Length;
            if (core.Length == 0) content = text;
            else
            {
                content = Escape(core);
                string emphasis = (format.Bold ? "**" : "") + (format.Italic ? "*" : "");
                content = emphasis + content + emphasis;
                if (format.Strike) content = "~~" + content + "~~";
                content = text[..leading] + content + (trailing > 0 ? text[^trailing..] : "");
            }
        }

        if (format.Url != null)
        {
            string url = format.Url.Replace("\\", "%5C").Replace("<", "%3C").Replace(">", "%3E").Replace("\r", "%0D").Replace("\n", "%0A");
            string title = string.IsNullOrEmpty(format.Info?.Title) ? "" : " \"" + format.Info.Title.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace('\r', ' ').Replace('\n', ' ') + "\"";
            content = (format.Info?.IsImage == true ? "!" : "") + "[" + content + "](<" + url + ">" + title + ")";
        }
        return content;
    }

    /// <summary>
    /// Escapes characters that would start inline markup. Block markers ('#', '>', '-', '+', "1.") only count at the
    /// start of a line and are escaped there (LineStartMarker), so ordinary punctuation inside a sentence stays as is.
    /// </summary>
    private static string Escape(string text)
    {
        StringBuilder result = new();
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            char before = index > 0 ? text[index - 1] : ' ';
            char after = index + 1 < text.Length ? text[index + 1] : ' ';
            bool escape = character switch
            {
                '\\' or '`' or '*' or '[' or ']' or '|' => true,
                // An underscore between letters or digits cannot open or close emphasis.
                '_' => !(char.IsLetterOrDigit(before) && char.IsLetterOrDigit(after)),
                // Only a double tilde marks strikethrough.
                '~' => before == '~' || after == '~',
                // '<' starts an autolink only before a letter, '/', '!' or '?'.
                '<' => char.IsLetter(after) || after is '/' or '!' or '?',
                // '&' starts a character reference only in the form "&name;" or "&#123;".
                '&' => EntityAt().IsMatch(text, index),
                _ => false,
            };
            if (escape) result.Append('\\');
            result.Append(character);
        }
        return result.ToString();
    }

    private static int LongestBackticks(string text) => Backticks().Matches(text).Select(match => match.Length).DefaultIfEmpty(0).Max();

    [GeneratedRegex("`+")]
    private static partial Regex Backticks();

    [GeneratedRegex(@"(?m)^[ \t]+|[ \t]+\z")]
    private static partial Regex EdgeWhitespace();

    // A heading, quote, list item, ordered item ("1." after the digits of group 1), or a line of only '-' or '='
    // (thematic break, setext underline) at the start of a line; the backslash goes before group 2.
    [GeneratedRegex(@"(?m)^(\d{0,9})(#{1,6}(?=[ \t]|$)|>|[-+](?=[ \t]|$)|(?<=\d)[.)](?=[ \t]|$)|-+[ \t]*$|=+[ \t]*$)")]
    private static partial Regex LineStartMarker();

    [GeneratedRegex(@"(?<=^|[ \t])#+(?=[ \t]*$)")]
    private static partial Regex TrailingHashes();

    [GeneratedRegex(@"\G&(?:#[0-9]{1,7}|#[xX][0-9a-fA-F]{1,6}|[A-Za-z][A-Za-z0-9]{1,31});")]
    private static partial Regex EntityAt();

    private static string WriteTable(Table table)
    {
        int columns = table.ColDefs.Count;
        if (columns == 0) return "";
        List<string> rows = [];
        for (int row = 0; row < table.RowDefs.Count; row++)
        {
            var cells = Enumerable.Range(0, columns).Select(column =>
            {
                var cell = table.Cells.FirstOrDefault(candidate => candidate.RowNo == row && candidate.ColNo == column);
                // The header row is bold by definition.
                return cell == null ? "" : string.Join(" ", cell.CellBlocks.OfType<Paragraph>().Select(paragraph => WriteInlines(paragraph.Inlines, implicitBold: row == 0).Replace("  \n", " ").Replace('\n', ' ')));
            });
            rows.Add("| " + string.Join(" | ", cells) + " |");
            if (row == 0)
            {
                rows.Add("| " + string.Join(" | ", Enumerable.Range(0, columns).Select(column =>
                {
                    var cell = table.Cells.FirstOrDefault(candidate => candidate.RowNo == 0 && candidate.ColNo == column);
                    return cell?.CellBlocks.OfType<Paragraph>().FirstOrDefault()?.TextAlignment switch
                    {
                        TextAlignment.Center => ":---:",
                        TextAlignment.Right => "---:",
                        _ => "---"
                    };
                })) + " |");
            }
        }
        return string.Join("\n", rows);
    }

    internal static Paragraph Slice(Paragraph source, int start, int end)
    {
        var result = source.PropertyClone();
        result.LeftIndent = source.LeftIndent;
        result.FirstLineIndent = source.FirstLineIndent;
        result.ListMarker = source.ListMarker;
        int position = source.StartInDoc;
        foreach (var inline in source.Inlines)
        {
            int from = Math.Max(0, start - position);
            int to = Math.Min(inline.InlineLength, end - position);
            if (from < to)
            {
                var copy = inline.Clone();
                if (copy is EditableRun run) run.InlineText = inline.InlineText[from..to];
                result.Inlines.Add(copy);
            }
            position += inline.InlineLength;
        }
        return result;
    }

    internal static List<Block> Slice(IEnumerable<Block> blocks, int start, int end)
    {
        List<Block> result = [];
        foreach (var block in blocks)
        {
            if (block.StartInDoc >= end || block.StartInDoc + block.BlockLength <= start) continue;
            if (block is Paragraph paragraph) result.Add(Slice(paragraph, start, end));
            else if (block is Table table)
            {
                if (start <= table.StartInDoc && end >= table.EndInDoc) result.Add(table.FullClone(false));
                else result.AddRange(table.Cells.SelectMany(cell => Slice(cell.CellBlocks, start, end)));
            }
        }
        return result;
    }
}
