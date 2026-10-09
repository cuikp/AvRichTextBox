using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(AvRichTextBox.Tests.TestAppBuilder))]

namespace AvRichTextBox.Tests
{
    public class TestAppBuilder
    {
        // Skia rendering instead of headless drawing: image export (RenderTargetBitmap) and frame captures need it.
        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }

    public class App : Application
    {
        public override void Initialize()
        {
            // The control templates the RichTextBox relies on come from the Fluent theme; the demo app
            // includes it the same way.
            Styles.Add(new FluentTheme());
        }
    }
}
