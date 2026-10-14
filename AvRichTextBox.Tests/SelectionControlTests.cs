using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace AvRichTextBox.Tests;

public class SelectionControlTests
{
    private const string TextRtf = @"{\rtf1\pard alpha beta gamma\par second line}";

    private static Point PointAt(EditorHost host, int position, Paragraph? paragraph = null)
    {
        paragraph ??= host.Document.Blocks.OfType<Paragraph>().First();
        EditableParagraph visual = host.Editor.GetVisualDescendants().OfType<EditableParagraph>()
            .Single(p => ReferenceEquals(p.DataContext, paragraph));
        Rect caret = visual.TextLayout.HitTestTextPosition(position);
        return visual.TranslatePoint(new Point(caret.X + 1, caret.Y + caret.Height / 2), host.Window)!.Value;
    }

    private static void Click(EditorHost host, Point point, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        host.Window.MouseMove(point, modifiers);
        host.Window.MouseDown(point, MouseButton.Left, modifiers);
        host.Window.MouseUp(point, MouseButton.Left, modifiers);
        EditorHost.Pump();
    }

    [AvaloniaFact]
    public void SelectionEnabled_DefaultAndBinding_AreSupported()
    {
        using var host = new EditorHost(TextRtf);
        Assert.True(host.Editor.IsSelectionEnabled);
        var source = new CheckBox { IsChecked = true };
        using var binding = host.Editor.Bind(RichTextBox.IsSelectionEnabledProperty,
            new Binding(nameof(CheckBox.IsChecked)) { Source = source });

        source.IsChecked = false;
        Assert.False(host.Editor.IsSelectionEnabled);
        source.IsChecked = true;
        Assert.True(host.Editor.IsSelectionEnabled);
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void MouseDrag_RespectsSelectionEnabled(bool enabled)
    {
        using var host = new EditorHost(TextRtf);
        host.Editor.IsSelectionEnabled = enabled;
        Point start = PointAt(host, 1);
        Point end = PointAt(host, 10);
        host.Window.MouseMove(start);
        host.Window.MouseDown(start, MouseButton.Left);
        host.Window.MouseMove(end, RawInputModifiers.LeftMouseButton);
        host.Window.MouseUp(end, MouseButton.Left);
        EditorHost.Pump();

        Assert.Equal(enabled, host.Document.Selection.Length > 0);
        Assert.Equal(enabled, host.Editor.SelectionPath.IsVisible);
    }

    [AvaloniaTheory]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    public void MultipleClicks_RespectSelectionEnabled(int count, bool enabled)
    {
        using var host = new EditorHost(TextRtf);
        host.Editor.IsSelectionEnabled = enabled;
        Point point = PointAt(host, 2);
        for (int click = 0; click < count; click++)
            Click(host, point);

        Assert.Equal(enabled, host.Document.Selection.Length > 0);
    }

    [AvaloniaFact]
    public void ShiftClick_WhenDisabled_DoesNotExtendTheSelection()
    {
        using var host = new EditorHost(TextRtf);
        host.Editor.IsSelectionEnabled = false;
        host.Select(1, 0);
        Click(host, PointAt(host, 10), RawInputModifiers.Shift);

        Assert.Equal(0, host.Document.Selection.Length);
    }

    [AvaloniaTheory]
    [InlineData(Key.A, RawInputModifiers.Control)]
    [InlineData(Key.Left, RawInputModifiers.Shift)]
    [InlineData(Key.Right, RawInputModifiers.Shift)]
    [InlineData(Key.Up, RawInputModifiers.Shift)]
    [InlineData(Key.Down, RawInputModifiers.Shift)]
    [InlineData(Key.Home, RawInputModifiers.Shift)]
    [InlineData(Key.End, RawInputModifiers.Shift)]
    [InlineData(Key.PageUp, RawInputModifiers.Shift)]
    [InlineData(Key.PageDown, RawInputModifiers.Shift)]
    [InlineData(Key.Left, RawInputModifiers.Control | RawInputModifiers.Shift)]
    [InlineData(Key.Right, RawInputModifiers.Control | RawInputModifiers.Shift)]
    [InlineData(Key.Home, RawInputModifiers.Control | RawInputModifiers.Shift)]
    [InlineData(Key.End, RawInputModifiers.Control | RawInputModifiers.Shift)]
    public void SelectionShortcut_WhenDisabled_DoesNotSelectText(Key key, RawInputModifiers modifiers)
    {
        using var host = new EditorHost(TextRtf);
        host.Editor.IsSelectionEnabled = false;
        host.Select(7, 0);
        host.Press(key, modifiers);

        Assert.Equal(0, host.Document.Selection.Length);
        Assert.False(host.Editor.SelectionPath.IsVisible);
    }

    [AvaloniaFact]
    public void DisablingDuringDrag_ClearsSelectionAndStopsTheDrag()
    {
        using var host = new EditorHost(TextRtf);
        Point start = PointAt(host, 1);
        Point end = PointAt(host, 10);
        host.Window.MouseMove(start);
        host.Window.MouseDown(start, MouseButton.Left);
        host.Window.MouseMove(end, RawInputModifiers.LeftMouseButton);
        Assert.True(host.Document.Selection.Length > 0);

        host.Editor.IsSelectionEnabled = false;
        Assert.Equal(0, host.Document.Selection.Length);
        Assert.False(host.Editor.SelectionPath.IsVisible);
        host.Editor.IsSelectionEnabled = true;
        host.Window.MouseMove(PointAt(host, 14), RawInputModifiers.LeftMouseButton);
        host.Window.MouseUp(PointAt(host, 14), MouseButton.Left);

        Assert.Equal(0, host.Document.Selection.Length);
    }

    [AvaloniaFact]
    public void ProgrammaticSelection_WhenDisabled_HidesHighlightingAndCanBeReenabled()
    {
        using var host = new EditorHost(TextRtf);
        host.Editor.IsSelectionEnabled = false;
        host.Select(1, 4);

        Assert.Equal(4, host.Document.Selection.Length);
        Assert.False(host.Editor.SelectionPath.IsVisible);
        Assert.Empty(Assert.IsType<PathGeometry>(host.Editor.SelectionPath.Data).Figures!);

        host.Editor.IsSelectionEnabled = true;
        Assert.True(host.Editor.SelectionPath.IsVisible);
        Assert.NotEmpty(Assert.IsType<PathGeometry>(host.Editor.SelectionPath.Data).Figures!);
    }

    [AvaloniaFact]
    public void TableSelection_WhenDisabled_HidesCellHighlighting()
    {
        using var host = new EditorHost(@"{\rtf1\pard before\par\trowd\cellx2000\cellx4000\pard\intbl left\cell\pard\intbl right\cell\row\pard after}");
        Table table = Assert.Single(host.Document.Blocks.OfType<Table>());
        Cell cell = table.Cells[0];
        host.Editor.IsSelectionEnabled = false;
        host.Select(cell.CellBlocks[0].StartInDoc, cell.CellBlocks[0].BlockLength);

        Assert.False(cell.Selected);
        Assert.False(host.Editor.SelectionPath.IsVisible);
        host.Editor.IsSelectionEnabled = true;
        Assert.True(cell.Selected);
    }

    [AvaloniaTheory]
    [InlineData(false, RawInputModifiers.None, true)]
    [InlineData(true, RawInputModifiers.Control, true)]
    [InlineData(true, RawInputModifiers.None, false)]
    public void Hyperlink_WhenSelectionDisabled_RespectsClickModifiers(bool requiresControl, RawInputModifiers modifiers, bool opens)
    {
        using var host = new EditorHost(@"{\rtf1\pard {\field{\*\fldinst HYPERLINK ""https://example.com""}{\fldrslt example}}}");
        host.Editor.IsSelectionEnabled = false;
        host.Editor.IsReadOnly = true;
        host.Editor.CtrlKeyOpensHyperlink = requiresControl;
        EditableHyperlink link = Assert.Single(host.Runs.OfType<EditableHyperlink>());
        string? opened = null;
        link.LinkOpening += (_, e) => { opened = e.NavigateUri; e.Handled = true; };

        Click(host, PointAt(host, 3), modifiers);

        Assert.Equal(opens ? "https://example.com" : null, opened);
        Assert.Equal(0, host.Document.Selection.Length);
    }

    [AvaloniaTheory]
    [InlineData(Key.C)]
    [InlineData(Key.X)]
    public async Task CopyOrCut_WhenSelectionDisabled_LeavesTheClipboardAndTextUnchanged(Key key)
    {
        using var host = new EditorHost(TextRtf);
        var clipboard = host.Window.Clipboard!;
        var data = new DataTransfer();
        data.Add(DataTransferItem.CreateText("unchanged"));
        await clipboard.SetDataAsync(data);
        host.Editor.IsSelectionEnabled = false;
        host.Select(1, 4);
        string original = host.Document.Text;
        host.Press(key, RawInputModifiers.Control);

        Assert.Equal("unchanged", await clipboard.TryGetTextAsync());
        Assert.Equal(original, host.Document.Text);
    }

    [AvaloniaFact]
    public void SelectionDisabled_StillAllowsCaretMovementAndTyping()
    {
        using var host = new EditorHost(TextRtf);
        host.Editor.IsSelectionEnabled = false;
        host.Select(6, 0);
        host.Press(Key.Right);
        host.Type("X");

        Assert.StartsWith("alpha bXeta gamma", host.Document.Text);
        Assert.Equal(0, host.Document.Selection.Length);
    }

    [AvaloniaFact]
    public void HyperlinkInTable_WhenSelectionDisabled_StillOpens()
    {
        using var host = new EditorHost(@"{\rtf1\pard before\par\trowd\cellx3000\pard\intbl {\field{\*\fldinst HYPERLINK ""https://example.com""}{\fldrslt example}}\cell\row\pard after}");
        host.Editor.IsSelectionEnabled = false;
        host.Editor.IsReadOnly = true;
        Paragraph paragraph = host.Document.AllParagraphs.Single(p => p.IsCellBlock);
        EditableHyperlink link = Assert.Single(paragraph.Inlines.OfType<EditableHyperlink>());
        bool opened = false;
        link.LinkOpening += (_, e) => { opened = true; e.Handled = true; };

        Click(host, PointAt(host, 3, paragraph));

        Assert.True(opened);
        Assert.Equal(0, host.Document.Selection.Length);
    }
}
