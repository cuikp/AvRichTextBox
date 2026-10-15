using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.VisualTree;
using DocumentFormat.OpenXml.Packaging;
using System.Text;
using Xunit;

namespace AvRichTextBox.Tests;

public class CustomContentTests
{
    private static CustomContent Mention(string text = "@User") => new("mention", "{\"id\":123}", text);
    private static IEnumerable<CustomInline> Inlines(FlowDocument document) => document.AllParagraphs.SelectMany(paragraph => paragraph.Inlines).OfType<CustomInline>();

    private static void RegisterMention(FlowDocument document) => document.RegisterCustomContent("mention", definition =>
        new Border { Padding = new Thickness(4), Background = Brushes.LightBlue, Child = new TextBlock { Text = definition.FallbackText } });

    [AvaloniaFact]
    public void InsertCustomInline_ReplacesTokenAndDisplaysTheRegisteredControl()
    {
        using var host = new EditorHost(@"{\rtf1\pard Hello @123!}");
        RegisterMention(host.Document);
        var inline = host.Document.InsertCustomInline(Mention(), 6, 4);
        EditorHost.Pump();
        Assert.Equal("Hello @!\r", host.Document.Text);
        Assert.Equal(Mention(), inline.Definition);
        var border = Assert.IsType<Border>(inline.GetChild());
        Assert.Equal("@User", Assert.IsType<TextBlock>(border.Child).Text);
        Assert.True(border.Bounds.Width > 0);
        Assert.True(border.Bounds.Height > 0);
        Assert.Contains(border, host.Window.GetVisualDescendants());
    }

    [AvaloniaFact]
    public void UnregisteredContent_DisplaysFallbackAndCanBeRegisteredLater()
    {
        using var host = new EditorHost(@"{\rtf1\pard token}");
        var inline = host.Document.InsertCustomInline(Mention(), 0, 5);
        EditorHost.Pump();
        Assert.Equal("@User", Assert.IsType<TextBlock>(inline.GetChild()).Text);
        RegisterMention(host.Document);
        EditorHost.Pump();
        Assert.IsType<Border>(inline.GetChild());
    }

    [AvaloniaFact]
    public void CustomInline_ClonesCreateIndependentControlsAndKeepTheData()
    {
        using var host = new EditorHost();
        RegisterMention(host.Document);
        var original = host.Document.InsertCustomInline(Mention(), 0);
        EditorHost.Pump();
        var clone = original.Clone();
        Assert.Equal(original.Definition, clone.Definition);
        Assert.NotSame(original.GetChild(), clone.GetChild());
        Assert.NotSame(Assert.IsType<Border>(original.GetChild()).Child, Assert.IsType<Border>(clone.GetChild()).Child);
        Assert.Contains(original.GetChild()!, host.Window.GetVisualDescendants());
    }

    [AvaloniaFact]
    public void CustomBlock_InsertionAddsATypingParagraphAndIsOneUndoStep()
    {
        using var host = new EditorHost(@"{\rtf1\pard Before}");
        RegisterMention(host.Document);
        host.Document.InsertCustomBlock(1, Mention());
        EditorHost.Pump();
        Assert.Equal(3, host.Document.Blocks.Count);
        Assert.IsType<CustomBlock>(host.Document.Blocks[1]);
        Assert.IsType<Paragraph>(host.Document.Blocks[2]);
        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Single(host.Document.Blocks);
        Assert.Equal("Before\r", host.Document.Text);
        host.Press(Key.Y, RawInputModifiers.Control);
        Assert.IsType<CustomBlock>(host.Document.Blocks[1]);
        var last = host.Document.AllParagraphs.Last();
        host.Select(last.StartInDoc, 0);
        host.Type("After");
        Assert.EndsWith("After\r", host.Document.Text);
    }

    [AvaloniaFact]
    public void InlineReplacement_SupportsRepeatedUndoRedo()
    {
        using var host = new EditorHost(@"{\rtf1\pard Hello @123!}");
        RegisterMention(host.Document);
        host.Document.InsertCustomInline(Mention(), 6, 4);
        EditorHost.Pump();
        for (int index = 0; index < 3; index++)
        {
            host.Press(Key.Z, RawInputModifiers.Control);
            Assert.Empty(Inlines(host.Document));
            Assert.Equal("Hello @123!\r", host.Document.Text);
            host.Press(Key.Y, RawInputModifiers.Control);
            Assert.Equal(Mention(), Assert.Single(Inlines(host.Document)).Definition);
            Assert.Equal("Hello @!\r", host.Document.Text);
        }
    }

    [AvaloniaFact]
    public void DefinitionChanges_AreUndoableAndRefreshTheDisplay()
    {
        using var host = new EditorHost();
        RegisterMention(host.Document);
        host.Document.InsertCustomInline(Mention(), 0);
        EditorHost.Pump();
        Assert.Single(Inlines(host.Document)).Definition = Mention("@Changed");
        EditorHost.Pump();
        Assert.Equal("@Changed", Assert.IsType<TextBlock>(Assert.IsType<Border>(Assert.Single(Inlines(host.Document)).GetChild()).Child).Text);
        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Equal(Mention(), Assert.Single(Inlines(host.Document)).Definition);
        host.Press(Key.Y, RawInputModifiers.Control);
        Assert.Equal(Mention("@Changed"), Assert.Single(Inlines(host.Document)).Definition);
    }

    [AvaloniaFact]
    public void DeletingCustomInline_RemovesOneAtomicElementAndUndoRestoresIt()
    {
        using var host = new EditorHost(@"{\rtf1\pard ab}");
        host.Document.InsertCustomInline(Mention(), 1);
        EditorHost.Pump();
        host.Select(1, 1);
        host.Press(Key.Delete);
        Assert.Empty(Inlines(host.Document));
        Assert.Equal("ab\r", host.Document.Text);
        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Single(Inlines(host.Document));
        Assert.Equal("a@b\r", host.Document.Text);
    }

    [AvaloniaFact]
    public void Xaml_RoundTripsCustomBlocksInlinesAndEscapedMultilineData()
    {
        using var source = new EditorHost(@"{\rtf1\pard before}");
        var definition = new CustomContent("widget<&\"", "{\"text\":\"<&>\"}\r\nnext\tline", "Fallback <&>\r\n\u03B1");
        source.Document.InsertCustomInline(definition, 0);
        source.Document.InsertCustomBlock(1, definition);
        EditorHost.Pump();
        string xaml = source.Editor.SaveXamlString();
        using var destination = new EditorHost();
        destination.Editor.LoadXamlString(xaml);
        EditorHost.Pump();
        Assert.Equal(2, Inlines(destination.Document).Count());
        Assert.All(Inlines(destination.Document), inline => Assert.Equal(definition, inline.Definition));
        Assert.Single(destination.Document.Blocks.OfType<CustomBlock>());
        Assert.Equal(xaml, destination.Editor.SaveXamlString());
    }

    [AvaloniaFact]
    public void Xaml_UsesNamespaceIdentityInsteadOfAFixedPrefix()
    {
        using var host = new EditorHost();
        host.Editor.LoadXamlString("<Section xmlns:w=\"urn:avrichtextbox:custom-content\"><Paragraph><w:Inline Type=\"mention\" Data=\"123\" Text=\"User\"/></Paragraph></Section>");
        EditorHost.Pump();
        Assert.Equal(new CustomContent("mention", "123", "User"), Assert.Single(Inlines(host.Document)).Definition);
    }

    [AvaloniaFact]
    public void Factories_AreScopedToTheDocumentAndSurviveLoading()
    {
        using var source = new EditorHost();
        using var destination = new EditorHost();
        source.Document.RegisterCustomContent("mention", _ => new TextBlock { Text = "First" });
        destination.Document.RegisterCustomContent("mention", _ => new TextBlock { Text = "Second" });
        source.Document.InsertCustomInline(Mention(), 0);
        destination.Editor.LoadXamlString(source.Editor.SaveXamlString());
        EditorHost.Pump();
        Assert.Equal("First", Assert.IsType<TextBlock>(Assert.Single(Inlines(source.Document)).GetChild()).Text);
        Assert.Equal("Second", Assert.IsType<TextBlock>(Assert.Single(Inlines(destination.Document)).GetChild()).Text);
    }

    [AvaloniaFact]
    public void TextAndRichFormats_ExportFallbackText()
    {
        using var host = new EditorHost(@"{\rtf1\pard before after}");
        host.Document.InsertCustomInline(Mention("User <&>"), 7);
        EditorHost.Pump();
        using var range = new TextRange(host.Document, 0, host.Document.DocEndPoint);
        using var stream = new MemoryStream();
        range.Save(stream, ContentDataFormat.Text);
        Assert.Equal("before User <&>after", Encoding.UTF8.GetString(stream.ToArray()));
        using var rtf = new EditorHost(host.Editor.SaveRtf());
        Assert.Contains("User <&>", rtf.Document.Text);
        Assert.Contains("User &lt;&amp;&gt;", host.Editor.SaveHtml());
        using var wordStream = new MemoryStream();
        using var package = WordprocessingDocument.Create(wordStream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true);
        var part = package.AddMainDocumentPart();
        var paragraph = WordConversions.CreateWordDocParagraph(host.Document.Blocks[0], ref part);
        Assert.Contains("User <&>", paragraph.InnerText);
    }

    [AvaloniaFact]
    public void Markdown_ExportsFallbackTextOfInlinesAndBlocks()
    {
        using var host = new EditorHost(@"{\rtf1\pard before after}");
        host.Document.InsertCustomInline(Mention("@User"), 7);
        host.Document.InsertCustomBlock(1, Mention("@Other"));
        EditorHost.Pump();

        string markdown = host.Editor.SaveMarkdown();

        Assert.Contains("before @Userafter", markdown);
        Assert.Contains("@Other", markdown);
    }

    [AvaloniaFact]
    public void XamlRange_PreservesCustomInlineAndLeavesStreamsOpen()
    {
        using var source = new EditorHost(@"{\rtf1\pard ab}");
        source.Document.InsertCustomInline(Mention(), 1);
        EditorHost.Pump();
        using var range = new TextRange(source.Document, 1, 2);
        using var stream = new MemoryStream();
        range.Save(stream, ContentDataFormat.Xaml);
        stream.Position = 0;
        using var destination = new EditorHost(@"{\rtf1\pard cd}");
        destination.Select(1, 0);
        destination.Document.Selection.Load(stream, ContentDataFormat.Xaml);
        EditorHost.Pump();
        Assert.True(stream.CanRead);
        Assert.Equal("c@d\r", destination.Document.Text);
        Assert.Equal(Mention(), Assert.Single(Inlines(destination.Document)).Definition);
    }

    [AvaloniaFact]
    public void CustomContent_CanBeInsertedInsideATableCell()
    {
        using var host = new EditorHost();
        host.Editor.LoadXamlString("<Section><Table><Table.Columns><TableColumn Width=\"150\"/></Table.Columns><TableRowGroup><TableRow><TableCell><Paragraph><Run>cell</Run></Paragraph></TableCell></TableRow></TableRowGroup></Table></Section>");
        EditorHost.Pump();
        var cell = Assert.Single(Assert.Single(host.Document.Blocks.OfType<Table>()).Cells);
        var paragraph = Assert.Single(cell.CellBlocks.OfType<Paragraph>());
        host.Document.InsertCustomInline(Mention(), paragraph.StartInDoc, 4);
        host.Document.InsertCustomBlock(1, Mention("Block"), cell);
        EditorHost.Pump();
        string xaml = host.Editor.SaveXamlString();
        Assert.Equal(2, Inlines(host.Document).Count());
        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Single(Inlines(host.Document));
        host.Press(Key.Y, RawInputModifiers.Control);
        Assert.Equal(xaml, host.Editor.SaveXamlString());
    }

    [AvaloniaFact]
    public void InvalidInlineRange_DoesNotChangeTheDocument()
    {
        using var host = new EditorHost(@"{\rtf1\pard first\par second}");
        string before = host.Editor.SaveXamlString();
        Assert.Throws<ArgumentException>(() => host.Document.InsertCustomInline(Mention(), 2, 8));
        Assert.Equal(before, host.Editor.SaveXamlString());
        Assert.Empty(Inlines(host.Document));
    }

    [AvaloniaFact]
    public void EmptyCustomBlock_AfterDeletingItsContentStillRoundTrips()
    {
        using var host = new EditorHost();
        var block = host.Document.InsertCustomBlock(0, Mention());
        EditorHost.Pump();
        host.Select(block.StartInDoc, 1);
        host.Press(Key.Delete);
        string xaml = host.Editor.SaveXamlString();
        using var copy = new EditorHost();
        copy.Editor.LoadXamlString(xaml);
        EditorHost.Pump();
        Assert.Equal(xaml, copy.Editor.SaveXamlString());
    }

    [AvaloniaFact]
    public void Clipboard_CopyPastePreservesCustomContentAndSupportsUndoRedo()
    {
        using var host = new EditorHost(@"{\rtf1\pard ab}");
        RegisterMention(host.Document);
        host.Document.InsertCustomInline(Mention(), 1);
        EditorHost.Pump();
        host.Select(1, 1);
        host.Press(Key.C, RawInputModifiers.Control);
        host.Select(host.Document.DocEndPoint, 0);
        host.Press(Key.V, RawInputModifiers.Control);
        EditorHost.Pump();
        Assert.Equal(2, Inlines(host.Document).Count());
        Assert.All(Inlines(host.Document), inline => Assert.Equal(Mention(), inline.Definition));
        Assert.Equal("a@b@\r", host.Document.Text);
        host.Press(Key.Z, RawInputModifiers.Control);
        Assert.Single(Inlines(host.Document));
        host.Press(Key.Y, RawInputModifiers.Control);
        Assert.Equal(2, Inlines(host.Document).Count());
    }

    [AvaloniaFact]
    public void HostedButton_ReceivesPointerInput()
    {
        using var host = new EditorHost();
        int clicks = 0;
        host.Document.RegisterCustomContent("button", _ =>
        {
            var button = new Button { Content = "Open", Width = 80, Height = 30 };
            button.Click += (_, _) => clicks++;
            return button;
        });
        var inline = host.Document.InsertCustomInline(new CustomContent("button", "", "Open"), 0);
        EditorHost.Pump();
        var button = Assert.IsType<Button>(inline.GetChild());
        var point = button.TranslatePoint(new Point(20, 15), host.Window)!.Value;
        host.Window.MouseMove(point);
        host.Window.MouseDown(point, MouseButton.Left);
        host.Window.MouseUp(point, MouseButton.Left);
        EditorHost.Pump();
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void ClonedContent_CanBeShownInASecondEditorAtTheSameTime()
    {
        using var source = new EditorHost();
        RegisterMention(source.Document);
        var block = source.Document.InsertCustomBlock(0, Mention());
        EditorHost.Pump();
        using var destination = new EditorHost();
        RegisterMention(destination.Document);
        destination.Document.InsertBlockAt(0, block.FullClone(false));
        EditorHost.Pump();
        var sourceControl = Assert.Single(Inlines(source.Document)).GetChild();
        var destinationControl = Assert.Single(Inlines(destination.Document)).GetChild();
        Assert.NotSame(sourceControl, destinationControl);
        Assert.Contains(sourceControl!, source.Window.GetVisualDescendants());
        Assert.Contains(destinationControl!, destination.Window.GetVisualDescendants());
    }

    [AvaloniaFact]
    public void InsertCustomBlock_InitializesAnEmptyHeadlessDocument()
    {
        var document = new FlowDocument();
        document.InsertCustomBlock(0, Mention());
        Assert.Equal(2, document.Blocks.Count);
        document.Undo();
        EditorHost.Pump();
        Assert.Single(document.Blocks);
        document.Redo();
        EditorHost.Pump();
        Assert.Equal(2, document.Blocks.Count);
    }

    [AvaloniaFact]
    public void XamlSelection_PreservesAWholeTableContainingCustomContent()
    {
        using var source = new EditorHost();
        source.Editor.LoadXamlString("<Section><Paragraph><Run>before</Run></Paragraph><Table><Table.Columns><TableColumn Width=\"150\"/></Table.Columns><TableRowGroup><TableRow><TableCell><Paragraph><Run>cell</Run></Paragraph></TableCell></TableRow></TableRowGroup></Table><Paragraph><Run>after</Run></Paragraph></Section>");
        EditorHost.Pump();
        var tableParagraph = source.Document.AllParagraphs.Single(paragraph => paragraph.IsCellBlock);
        source.Document.InsertCustomInline(Mention(), tableParagraph.StartInDoc, 4);
        using var stream = new MemoryStream();
        using var selection = new TextRange(source.Document, 0, source.Document.DocEndPoint);
        selection.Save(stream, ContentDataFormat.Xaml);
        stream.Position = 0;
        using var destination = new EditorHost();
        destination.Document.Selection.Load(stream, ContentDataFormat.Xaml);
        EditorHost.Pump();
        Assert.Single(destination.Document.Blocks.OfType<Table>());
        Assert.Equal(Mention(), Assert.Single(Inlines(destination.Document)).Definition);
    }

    [AvaloniaFact]
    public async Task Clipboard_CustomContentRespectsCopyAndReadOnlyOptions()
    {
        using var host = new EditorHost();
        host.Document.InsertCustomInline(Mention(), 0);
        EditorHost.Pump();
        host.Select(0, 1);
        await host.Window.Clipboard!.SetTextAsync("sentinel");
        host.Editor.DisableUserCopy = true;
        host.Press(Key.C, RawInputModifiers.Control);
        Assert.Equal("sentinel", await host.Window.Clipboard!.TryGetTextAsync());
        host.Editor.DisableUserCopy = false;
        host.Press(Key.C, RawInputModifiers.Control);
        host.Editor.IsReadOnly = true;
        host.Press(Key.V, RawInputModifiers.Control);
        Assert.Single(Inlines(host.Document));
    }

    [AvaloniaFact]
    public void Clipboard_CustomBlockRetainsItsBlockIdentity()
    {
        using var host = new EditorHost();
        var block = host.Document.InsertCustomBlock(0, Mention());
        EditorHost.Pump();
        host.Select(block.StartInDoc, 1);
        host.Press(Key.C, RawInputModifiers.Control);
        host.Select(host.Document.DocEndPoint, 0);
        host.Press(Key.V, RawInputModifiers.Control);
        EditorHost.Pump();
        Assert.Equal(2, host.Document.Blocks.OfType<CustomBlock>().Count());
        Assert.Equal(2, Inlines(host.Document).Count());
    }

    [AvaloniaFact]
    public void InsertCustomInline_InitializesAnEmptyHeadlessDocument()
    {
        var document = new FlowDocument();
        document.InsertCustomInline(Mention(), 0);
        Assert.Equal(Mention(), Assert.Single(Inlines(document)).Definition);
        document.Undo();
        EditorHost.Pump();
        Assert.Empty(Inlines(document));
        Assert.Single(document.Blocks);
    }

    [AvaloniaFact]
    public void InvalidBlockIndex_DoesNotInitializeOrChangeAnEmptyDocument()
    {
        var document = new FlowDocument();
        Assert.Throws<ArgumentOutOfRangeException>(() => document.InsertCustomBlock(1, Mention()));
        Assert.Empty(document.Blocks);
        Assert.Empty(document.Undos);
    }
}
