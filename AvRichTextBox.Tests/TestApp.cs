using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(AvRichTextBox.Tests.TestAppBuilder))]

namespace AvRichTextBox.Tests
{
    public class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
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
