using Avalonia.Controls;
using System.Collections.ObjectModel;

namespace AvRichTextBox;

public partial class FlowDocument
{
    private readonly Dictionary<string, Func<CustomContent, Control>> _customContentFactories = new(StringComparer.Ordinal);

    /// <summary>Registers a factory for application content. Each call must return a new control.</summary>
    public void RegisterCustomContent(string type, Func<CustomContent, Control> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(factory);
        _customContentFactories[type] = factory;
        foreach (var inline in AllParagraphs.SelectMany(paragraph => paragraph.Inlines).OfType<CustomInline>())
            if (inline.Definition.Type == type) inline.RefreshContent();
    }

    internal Control? CreateCustomControl(CustomContent definition) =>
        _customContentFactories.TryGetValue(definition.Type, out var factory) ? factory(definition) : null;

    /// <summary>Replaces a source token in one paragraph with an atomic application element.</summary>
    public CustomInline InsertCustomInline(CustomContent definition, int start, int length = 0)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (start < 0 || length < 0 || start > DocEndPoint - length)
            throw new ArgumentOutOfRangeException(nameof(start));
        if (Blocks.Count == 0) NewDocument();
        UpdateBlockAndInlineStarts(0);
        var paragraph = GetContainingParagraph(start);
        if (paragraph == null || start + length > paragraph.EndInDoc)
            throw new ArgumentException("An inline replacement must stay within one paragraph.", nameof(length));
        var collection = paragraph.OwningCell?.CellBlocks ?? Blocks;
        var inline = new CustomInline(definition);
        bool previous = DisableUndoStack;
        try
        {
            DisableUndoStack = true;
            var replacement = SliceCustomParagraph(paragraph, paragraph.StartInDoc, start);
            replacement.Inlines.Add(inline);
            foreach (var suffix in SliceCustomParagraph(paragraph, start + length, paragraph.EndInDoc).Inlines.ToList())
                replacement.Inlines.Add(suffix);
            ApplyCustomEdit(collection, collection.IndexOf(paragraph), 1, [replacement], start, length, 1, paragraph.OwningCell?.Id, previous);
        }
        finally { DisableUndoStack = previous; }
        return inline;
    }

    /// <summary>Inserts an application block, with an editable paragraph after a final block.</summary>
    public CustomBlock InsertCustomBlock(int index, CustomContent definition, Cell? cell = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (index < 0 || index > (cell?.CellBlocks.Count ?? Blocks.Count)) throw new ArgumentOutOfRangeException(nameof(index));
        if (Blocks.Count == 0 && cell == null) NewDocument();
        var collection = cell?.CellBlocks ?? Blocks;
        if (cell != null && !AllParagraphs.Any(paragraph => paragraph.OwningCell == cell))
            throw new ArgumentException("The cell must belong to this document.", nameof(cell));
        int start = index < collection.Count ? collection[index].StartInDoc : collection.LastOrDefault()?.EndInDoc + 1 ?? 0;
        var block = new CustomBlock(definition);
        List<Block> inserted = [block];
        if (index == collection.Count)
        {
            var trailing = new Paragraph();
            trailing.Inlines.Add(new EditableRun(""));
            inserted.Add(trailing);
        }
        bool previous = DisableUndoStack;
        try
        {
            DisableUndoStack = true;
            ApplyCustomEdit(collection, index, 0, inserted, start, 0, inserted.Sum(item => item.BlockLength), cell?.Id, previous);
        }
        finally { DisableUndoStack = previous; }
        return block;
    }

    internal bool SelectionContainsCustomContent(TextRange range) => AllParagraphs.Any(paragraph =>
        paragraph.Inlines.OfType<CustomInline>().Any(inline =>
            paragraph.StartInDoc + inline.TextPositionOfInlineInParagraph < range.End &&
            paragraph.StartInDoc + inline.TextPositionOfInlineInParagraph + 1 > range.Start));

    internal string GetCustomSelectionXaml(TextRange range)
    {
        bool previous = DisableUndoStack;
        try
        {
            DisableUndoStack = true;
            return XamlConversions.SectionTextDefault + string.Concat(GetCustomSelectionBlocks(Blocks, range).Select(block =>
                block is Table table ? XamlConversions.GetTableXaml(table, false) : XamlConversions.GetParagraphXaml((Paragraph)block, false))) + "</Section>";
        }
        finally { DisableUndoStack = previous; }
    }

    private static IEnumerable<Block> GetCustomSelectionBlocks(IEnumerable<Block> blocks, TextRange range)
    {
        foreach (var block in blocks)
        {
            if (block.StartInDoc >= range.End || block.StartInDoc + block.BlockLength <= range.Start) continue;
            if (block is Table table)
            {
                if (range.Start <= table.StartInDoc && range.End >= table.EndInDoc)
                    yield return table.FullClone(false);
                else
                    foreach (var selected in table.Cells.SelectMany(cell => GetCustomSelectionBlocks(cell.CellBlocks, range))) yield return selected;
            }
            else if (block is Paragraph paragraph)
                yield return SliceCustomParagraph(paragraph, range.Start, range.End);
        }
    }

    internal string GetCustomSelectionText(TextRange range)
    {
        bool previous = DisableUndoStack;
        try
        {
            DisableUndoStack = true;
            return string.Concat(AllParagraphs.Where(paragraph => paragraph.StartInDoc < range.End && paragraph.EndInDoc >= range.Start)
                .Select(paragraph => string.Concat(SliceCustomParagraph(paragraph, range.Start, range.End).Inlines
                    .Select(inline => inline is CustomInline custom ? custom.Definition.FallbackText : inline is EditableLineBreak ? "\n" : inline.InlineText))
                    + (range.End > paragraph.EndInDoc ? "\n" : "")));
        }
        finally { DisableUndoStack = previous; }
    }

    internal void InsertCustomSelectionXaml(string xaml, TextRange range)
    {
        bool previous = DisableUndoStack;
        try
        {
            DisableUndoStack = true;
            var blocks = XamlConversions.GetBlocksFromXamlString(xaml, this);
            if (blocks.Count == 0) return;
            UpdateBlockAndInlineStarts(0);
            var first = GetContainingParagraph(range.Start);
            var last = GetContainingParagraph(range.End);
            if (first == null || last == null || first.OwningCell != last.OwningCell)
                throw new NotSupportedException("Custom content paste must stay within one block collection or table cell.");
            var collection = first.OwningCell?.CellBlocks ?? Blocks;
            int index = collection.IndexOf(first);
            int endIndex = collection.IndexOf(last);
            var prefix = SliceCustomParagraph(first, first.StartInDoc, range.Start);
            var suffix = SliceCustomParagraph(last, range.End, last.EndInDoc);
            if (blocks[0] is Paragraph firstParagraph && firstParagraph is not CustomBlock)
            {
                foreach (var inline in firstParagraph.Inlines.ToList()) prefix.Inlines.Add(inline);
                blocks[0] = prefix;
            }
            else if (prefix.Inlines.Count > 0) blocks.Insert(0, prefix);
            if (blocks[^1] is Paragraph lastParagraph && lastParagraph is not CustomBlock)
            {
                foreach (var inline in suffix.Inlines.ToList()) lastParagraph.Inlines.Add(inline);
            }
            else blocks.Add(suffix);
            foreach (var paragraph in blocks.OfType<Paragraph>()) paragraph.EnsureEmptyRuns();
            int oldLength = collection.Skip(index).Take(endIndex - index + 1).Sum(block => block.BlockLength);
            int insertedLength = blocks.Sum(block => block.BlockLength) - oldLength + range.Length;
            ApplyCustomEdit(collection, index, endIndex - index + 1, blocks, range.Start, range.Length, insertedLength, first.OwningCell?.Id, previous);
        }
        finally { DisableUndoStack = previous; }
    }

    private static Paragraph SliceCustomParagraph(Paragraph source, int start, int end)
    {
        var result = source.PropertyClone();
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

    private void ApplyCustomEdit(ObservableCollection<Block> collection, int index, int removeCount, List<Block> inserted,
        int start, int removedLength, int insertedLength, int? cellId, bool undoWasDisabled)
    {
        var before = collection.Skip(index).Take(removeCount).Select(block => block.FullClone(true)).ToList();
        ReplaceCustomBlocks(collection, index, removeCount, inserted);
        var after = inserted.Select(block => block.FullClone(true)).ToList();
        if (!undoWasDisabled)
        {
            Undos.Add(new CustomContentEdit(this, cellId, index, before, after, start, removedLength, insertedLength));
            Redos.Clear();
        }
        UpdateTextRanges(start, insertedLength - removedLength);
        Select(Math.Min(DocEndPoint, start + insertedLength), 0);
    }

    private void ReplaceCustomBlocks(ObservableCollection<Block> collection, int index, int count, List<Block> blocks)
    {
        foreach (var table in blocks.OfType<Table>())
            foreach (var cell in table.Cells) cell.IsClonedCell = false;
        for (int item = 0; item < count; item++) collection.RemoveAt(index);
        for (int item = 0; item < blocks.Count; item++) collection.Insert(index + item, blocks[item]);
        if (Blocks.Count == 0) AddDefaultParagraph(Blocks);
        AllParagraphs = [.. GetAllParagraphs];
        UpdateBlockAndInlineStarts(0);
        UpdateTextLayouts(blocks);
    }

    private sealed class CustomContentEdit(FlowDocument document, int? cellId, int index, List<Block> before,
        List<Block> after, int start, int removedLength, int insertedLength) : IEditDo
    {
        public int EditOffset { get; set; }
        public int UpdateTextRangesFromCharIdx { get; set; }
        public bool UpdateTextRanges => true;
        public bool DoNextUndo => false;
        public bool DoNextRedo => false;
        public void PerformUndo() => Restore(after.Count, before, removedLength - insertedLength, start, removedLength);
        public void PerformRedo() => Restore(before.Count, after, insertedLength - removedLength, start + insertedLength, 0);

        private void Restore(int count, List<Block> source, int offset, int selectionStart, int selectionLength)
        {
            var cell = cellId.HasValue ? document.Blocks.OfType<Table>().SelectMany(table => table.Cells).First(candidate => candidate.Id == cellId) : null;
            document.ReplaceCustomBlocks(cell?.CellBlocks ?? document.Blocks, index, count, source.Select(block => block.FullClone(true)).ToList());
            EditOffset = offset;
            UpdateTextRangesFromCharIdx = start;
            document.Select(selectionStart, selectionLength);
        }
    }
}
