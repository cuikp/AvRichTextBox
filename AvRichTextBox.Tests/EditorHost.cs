using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace AvRichTextBox.Tests;

/// <summary>
/// A shown window hosting one RichTextBox. Keyboard input goes through the headless platform to the focused
/// element, so it takes the same KeyDown/TextInput path as real typing.
/// </summary>
internal sealed class EditorHost : IDisposable
{
    public Window Window { get; }
    public RichTextBox Editor { get; }
    public FlowDocument Document => Editor.FlowDocument;

    public EditorHost(string? rtf = null)
    {
        Editor = new RichTextBox();
        Window = new Window { Width = 600, Height = 400, Content = Editor };
        Window.Show();
        Pump();

        if (rtf != null)
        {
            Editor.LoadRtf(rtf);
            Pump();
        }

        Editor.Focus();
        Pump();
    }

    public static void Pump()
    {
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
    }

    public void Type(string text)
    {
        Window.KeyTextInput(text);
        Pump();
    }

    public void Press(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Window.KeyPress(key, modifiers, PhysicalKey.None, null);
        Window.KeyRelease(key, modifiers, PhysicalKey.None, null);
        Pump();
    }

    public void Select(int start, int length)
    {
        Document.Select(start, length);
        Pump();
    }

    public IEnumerable<EditableRun> Runs =>
        Document.Blocks.OfType<Paragraph>().SelectMany(paragraph => paragraph.Inlines).OfType<EditableRun>();

    public void Dispose() => Window.Close();
}
