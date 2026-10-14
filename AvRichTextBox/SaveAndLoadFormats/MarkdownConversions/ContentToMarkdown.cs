using Avalonia.Media;
using System.Text;
using System.Text.RegularExpressions;

namespace AvRichTextBox;

internal static partial class MarkdownConversions
{
    internal static string Write(IEnumerable<Block> blocks)
    {
        StringBuilder result = new();
        int previousQuoteDepth = 0;
        bool first = true;
        foreach (var block in blocks)
        {
            int quoteDepth = block.MarkdownInfo?.QuoteDepth ?? 0;
            if (!first)
                result.Append('\n').Append(string.Concat(Enumerable.Repeat("> ", Math.Min(previousQuoteDepth, quoteDepth)))).Append('\n');
            result.Append(WriteBlock(block));
            previousQuoteDepth = quoteDepth;
            first = false;
        }
        return result.ToString();
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
            else
                content = new string('#', info.HeadingLevel) + (info.HeadingLevel > 0 ? " " : "") + WriteInlines(paragraph.Inlines);

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

    internal static string WriteInlines(IEnumerable<IEditable> inlines)
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
                var format = new RunFormat(run.FontWeight >= FontWeight.Bold, run.FontStyle == FontStyle.Italic,
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
        return EdgeWhitespace().Replace(result.ToString(), match => string.Concat(match.Value.Select(character => character == '\t' ? "&#9;" : "&#32;")));
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

    private static string Escape(string text)
    {
        StringBuilder result = new();
        const string punctuation = "\\`*_{}[]()#+-.!|>~<\"&";
        foreach (char character in text)
        {
            if (punctuation.Contains(character)) result.Append('\\');
            result.Append(character);
        }
        return result.ToString();
    }

    private static int LongestBackticks(string text) => Backticks().Matches(text).Select(match => match.Length).DefaultIfEmpty(0).Max();

    [GeneratedRegex("`+")]
    private static partial Regex Backticks();

    [GeneratedRegex(@"(?m)^[ \t]+|[ \t]+\z")]
    private static partial Regex EdgeWhitespace();

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
                return cell == null ? "" : string.Join(" ", cell.CellBlocks.OfType<Paragraph>().Select(paragraph => WriteInlines(paragraph.Inlines).Replace("  \n", " ").Replace('\n', ' ')));
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
