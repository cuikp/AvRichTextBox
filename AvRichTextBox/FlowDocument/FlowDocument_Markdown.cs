using System.Collections.ObjectModel;
using System.Text;

namespace AvRichTextBox;

public partial class FlowDocument
{
    /// <summary>Replaces the document with parsed Markdown. No control is required.</summary>
    public void LoadMarkdown(string markdown)
    {
        bool previous = DisableUndoStack;
        try
        {
            DisableUndoStack = true;
            var blocks = MarkdownConversions.Parse(markdown);
            int blockId = BlockIdCounter;
            int inlineId = InlineIdCounter;
            ClearDocument();
            BlockIdCounter = blockId;
            InlineIdCounter = inlineId;
            foreach (var block in blocks) Blocks.Add(block);
            Redos.Clear();
            InitializeDocument();
        }
        finally { DisableUndoStack = previous; }
    }

    /// <summary>Exports the document as Markdown.</summary>
    public string SaveMarkdown() => MarkdownConversions.Write(Blocks);

    /// <summary>Loads UTF-8 Markdown from the current stream position and leaves the stream open.</summary>
    public void LoadMarkdown(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, leaveOpen: true);
        LoadMarkdown(reader.ReadToEnd());
    }

    /// <summary>Writes UTF-8 Markdown at the current stream position and leaves the stream open.</summary>
    public void SaveMarkdown(Stream stream)
    {
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        writer.Write(SaveMarkdown());
    }

    internal string SaveMarkdown(TextRange range)
    {
        if (range.Length == 0) return "";
        bool previous = DisableUndoStack;
        try
        {
            DisableUndoStack = true;
            return MarkdownConversions.Write(MarkdownConversions.Slice(Blocks, range.Start, range.End));
        }
        finally { DisableUndoStack = previous; }
    }

    /// <summary>Replaces the selection with Markdown as one undoable edit.</summary>
    public void InsertMarkdown(string markdown) => InsertMarkdown(markdown, Selection);

    internal void InsertMarkdown(string markdown, TextRange range)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        if (markdown.Length == 0) return;
        UpdateBlockAndInlineStarts(0);
        if (range.Start < 0 || range.End < range.Start || range.End > DocEndPoint)
            throw new ArgumentOutOfRangeException(nameof(range));
        var first = GetContainingParagraph(range.Start);
        var last = GetContainingParagraph(range.End);
        if (first == null || last == null) throw new ArgumentOutOfRangeException(nameof(range));
        if (first.OwningCell != last.OwningCell)
            throw new NotSupportedException("Markdown insertion requires a selection within one block collection or table cell.");
        ObservableCollection<Block> collection = first.OwningCell?.CellBlocks ?? Blocks;
        int index = collection.IndexOf(first);
        int endIndex = collection.IndexOf(last);
        if (index < 0 || endIndex < index)
            throw new NotSupportedException("Markdown insertion requires paragraph selection boundaries.");

        int start = range.Start;
        int selectedLength = range.Length;
        bool previous = DisableUndoStack;
        try
        {
            DisableUndoStack = true;
            // Prepare the complete replacement before changing the document.
            var replacement = MarkdownConversions.Parse(markdown);
            if (replacement.Count == 0) return;
            var before = collection.Skip(index).Take(endIndex - index + 1).Select(block => block.FullClone(true)).ToList();
            var prefix = MarkdownConversions.Slice(first, first.StartInDoc, start);
            var suffix = MarkdownConversions.Slice(last, range.End, last.EndInDoc);
            if (replacement[0] is Paragraph firstInserted)
            {
                if (prefix.Inlines.Count > 0)
                {
                    foreach (var inline in firstInserted.Inlines.ToList()) prefix.Inlines.Add(inline);
                    replacement[0] = prefix;
                }
            }
            else if (prefix.Inlines.Count > 0) replacement.Insert(0, prefix);
            if (replacement[^1] is Paragraph lastInserted)
            {
                foreach (var inline in suffix.Inlines.ToList()) lastInserted.Inlines.Add(inline);
            }
            else replacement.Add(suffix);
            foreach (var paragraph in replacement.OfType<Paragraph>()) paragraph.EnsureEmptyRuns();

            int insertedLength = replacement.Sum(block => block.BlockLength) - before.Sum(block => block.BlockLength) + selectedLength;
            ReplaceMarkdownBlocks(collection, index, before.Count, replacement);
            int caret = start + insertedLength;
            var after = replacement.Select(block => block.FullClone(true)).ToList();
            if (!previous)
            {
                Undos.Add(new MarkdownEdit(this, first.OwningCell?.Id, index, before, after, start, selectedLength, insertedLength));
                Redos.Clear();
            }
            UpdateTextRanges(start, insertedLength - selectedLength);
            Select(caret, 0);
            range.Start = caret;
            range.End = caret;
        }
        finally { DisableUndoStack = previous; }
    }

    private void ReplaceMarkdownBlocks(ObservableCollection<Block> collection, int index, int count, List<Block> blocks)
    {
        foreach (var table in blocks.OfType<Table>())
            foreach (var cell in table.Cells) cell.IsClonedCell = false;
        for (int item = 0; item < count; item++) collection.RemoveAt(index);
        for (int item = 0; item < blocks.Count; item++) collection.Insert(index + item, blocks[item]);
        AllParagraphs = [.. GetAllParagraphs];
        UpdateBlockAndInlineStarts(0);
        UpdateTextLayouts(blocks);
    }

    private sealed class MarkdownEdit(FlowDocument document, int? cellId, int index, List<Block> before,
        List<Block> after, int start, int selectedLength, int insertedLength) : IEditDo
    {
        public int EditOffset { get; set; }
        public int UpdateTextRangesFromCharIdx { get; set; }
        public bool UpdateTextRanges => true;
        public bool DoNextUndo => false;
        public bool DoNextRedo => false;

        public void PerformUndo() => Restore(after.Count, before, selectedLength - insertedLength, start, selectedLength);
        public void PerformRedo() => Restore(before.Count, after, insertedLength - selectedLength, start + insertedLength, 0);

        private void Restore(int count, List<Block> source, int offset, int selectionStart, int selectionLength)
        {
            var cell = cellId.HasValue ? document.Blocks.OfType<Table>().SelectMany(table => table.Cells).FirstOrDefault(candidate => candidate.Id == cellId) : null;
            if (cellId.HasValue && cell == null) throw new InvalidOperationException("The Markdown edit's table cell no longer exists.");
            document.ReplaceMarkdownBlocks(cell?.CellBlocks ?? document.Blocks, index, count, source.Select(block => block.FullClone(true)).ToList());
            EditOffset = offset;
            UpdateTextRangesFromCharIdx = start;
            document.Select(selectionStart, selectionLength);
        }
    }
}
