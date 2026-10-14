using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Xunit;

namespace AvRichTextBox.Tests;

public class HyperlinkFormattingTests
{
    private const string LinkUri = "https://example.com";

    private static StyledProperty<IBrush> HyperlinkBrushProperty =>
        Assert.IsType<StyledProperty<IBrush>>(AvaloniaPropertyRegistry.Instance.FindRegistered(typeof(RichTextBox), "HyperlinkBrush"));

    private static EditableHyperlink Link(EditorHost host, string text = "link") =>
        Assert.Single(host.Runs.OfType<EditableHyperlink>(), link => link.LinkDisplayText == text);

    private static void AssertColor(Color expected, IBrush? brush) =>
        Assert.Equal(expected, Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color);

    private static EditableHyperlink InsertLink(EditorHost host, string text = "link", int position = 0)
    {
        host.Select(position, 0);
        host.Editor.OpenHyperlinkPopup();
        host.Editor.FindControl<TextBox>("HyperlinkTextBox")!.Text = text;
        host.Editor.FindControl<TextBox>("HyperlinkUrlBox")!.Text = LinkUri;
        host.Editor.HyperlinkOkButton_Click(null, new RoutedEventArgs());
        EditorHost.Pump();
        return Link(host, text);
    }

    private static void Load(EditorHost host, string format, string content)
    {
        switch (format)
        {
            case "rtf": host.Editor.LoadRtf(content); break;
            case "xaml": host.Editor.LoadXamlString(content); break;
            case "html": host.Editor.LoadHtml(content); break;
            default: throw new ArgumentOutOfRangeException(nameof(format));
        }
        EditorHost.Pump();
    }

    [AvaloniaFact]
    public void NewHyperlink_HasBlueForegroundAndItsOwnUnderlineFollowingTheForeground()
    {
        var first = new EditableHyperlink("first", LinkUri);
        var second = new EditableHyperlink("second", LinkUri);

        AssertColor(Colors.Blue, first.Foreground);
        TextDecoration underline = Assert.Single(first.TextDecorations!);
        Assert.Equal(TextDecorationLocation.Underline, underline.Location);
        Assert.Null(underline.Stroke);
        Assert.NotSame(first.TextDecorations, second.TextDecorations);
    }

    [AvaloniaFact]
    public void SelectionFormatting_CanChangeHyperlinkColorAndRemoveUnderline()
    {
        using var host = new EditorHost(@"{\rtf1\pard before }");
        EditableHyperlink link = InsertLink(host);
        int start = host.Document.GetAbsPositionOfInlineInDoc(link);
        host.Select(start, link.LinkDisplayText.Length);

        host.Document.Selection.ApplyFormatting(Inline.ForegroundProperty, new SolidColorBrush(Colors.OrangeRed));
        host.Document.Selection.ApplyFormatting(Inline.TextDecorationsProperty, TextDecorationLocation.Underline);
        EditorHost.Pump();

        AssertColor(Colors.OrangeRed, Link(host).Foreground);
        Assert.Empty(Link(host).TextDecorations ?? []);
    }

    [AvaloniaFact]
    public void HyperlinkBrush_AppliesOnlyWhenInsertingANewLink()
    {
        using var host = new EditorHost(@"{\rtf1\pard before }");
        AssertColor(Colors.Blue, host.Editor.GetValue(HyperlinkBrushProperty));
        host.Editor.SetValue(HyperlinkBrushProperty, Brushes.LimeGreen);
        EditableHyperlink first = InsertLink(host, "first");
        AssertColor(Colors.LimeGreen, first.Foreground);

        first.Foreground = Brushes.OrangeRed;
        host.Editor.SetValue(HyperlinkBrushProperty, Brushes.Gold);
        EditableHyperlink second = InsertLink(host, "second", host.Document.DocEndPoint - 2);

        AssertColor(Colors.OrangeRed, first.Foreground);
        AssertColor(Colors.Gold, second.Foreground);
    }

    [AvaloniaFact]
    public void HyperlinkBrush_CanUseADynamicResourceWithoutRecoloringExistingLinks()
    {
        using var host = new EditorHost(@"{\rtf1\pard before }");
        host.Editor.Resources["TestHyperlinkBrush"] = Brushes.LimeGreen;
        using var binding = host.Editor.Bind(HyperlinkBrushProperty, new DynamicResourceExtension("TestHyperlinkBrush"));
        EditableHyperlink first = InsertLink(host, "first");

        host.Editor.Resources["TestHyperlinkBrush"] = Brushes.OrangeRed;
        EditorHost.Pump();
        EditableHyperlink second = InsertLink(host, "second", host.Document.DocEndPoint - 2);

        AssertColor(Colors.LimeGreen, first.Foreground);
        AssertColor(Colors.OrangeRed, second.Foreground);
    }

    [AvaloniaTheory]
    [InlineData("rtf", @"{\rtf1{\colortbl;\red255\green69\blue0;}\pard{\field{\*\fldinst HYPERLINK ""https://example.com""}{\fldrslt\cf1\strike link}}}")]
    [InlineData("xaml", "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph><Hyperlink NavigateUri=\"https://example.com\"><Run Foreground=\"#FFFF4500\" TextDecorations=\"Strikethrough\">link</Run></Hyperlink></Paragraph></Section>")]
    [InlineData("html", "<html><body><p><a href=\"https://example.com\" style=\"color:#FF4500;text-decoration:line-through;\">link</a></p></body></html>")]
    public void LoadHyperlink_PreservesExplicitColorAndDecorations(string format, string content)
    {
        using var host = new EditorHost();
        Load(host, format, content);

        EditableHyperlink link = Link(host);
        Assert.Equal(LinkUri, link.NavigateUri);
        AssertColor(Colors.OrangeRed, link.Foreground);
        Assert.Equal(TextDecorationLocation.Strikethrough, Assert.Single(link.TextDecorations!).Location);
    }

    [AvaloniaTheory]
    [InlineData("rtf", false)]
    [InlineData("rtf", true)]
    [InlineData("xaml", false)]
    [InlineData("xaml", true)]
    [InlineData("html", false)]
    [InlineData("html", true)]
    public void SaveAndLoadHyperlink_KeepsUserFormatting(string format, bool removeDecorations)
    {
        using var source = new EditorHost(@"{\rtf1\pard before }");
        EditableHyperlink original = InsertLink(source);
        original.Foreground = new SolidColorBrush(Colors.OrangeRed);
        original.TextDecorations = removeDecorations ? null : [new() { Location = TextDecorationLocation.Strikethrough }];

        string saved = format switch
        {
            "rtf" => source.Editor.SaveRtf(),
            "xaml" => source.Editor.SaveXamlString(),
            "html" => source.Editor.SaveHtml(),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        using var reloaded = new EditorHost();
        Load(reloaded, format, saved);

        EditableHyperlink link = Link(reloaded);
        Assert.Equal(LinkUri, link.NavigateUri);
        AssertColor(Colors.OrangeRed, link.Foreground);
        if (removeDecorations)
            Assert.Empty(link.TextDecorations ?? []);
        else
            Assert.Equal(TextDecorationLocation.Strikethrough, Assert.Single(link.TextDecorations!).Location);
    }

    [AvaloniaFact]
    public void CloneHyperlink_KeepsUserFormatting()
    {
        var original = new EditableHyperlink("link", LinkUri)
        {
            Foreground = Brushes.OrangeRed,
            TextDecorations = null
        };

        EditableHyperlink clone = original.Clone();

        AssertColor(Colors.OrangeRed, clone.Foreground);
        Assert.Null(clone.TextDecorations);
        Assert.Equal(LinkUri, clone.NavigateUri);
        Assert.Equal("link", clone.LinkDisplayText);
    }
}
