using Avalonia.Headless.XUnit;
using System.Globalization;
using Xunit;

namespace AvRichTextBox.Tests;

public class HtmlImportTests
{
    // The HTML reader takes the column count from <colgroup>.
    private const string TableHtml =
        "<html><body><table style=\"width:{0}px\"><colgroup><col/></colgroup><tr><td>a</td></tr></table></body></html>";

    private static double LoadedTableWidth(string width)
    {
        using var host = new EditorHost();
        host.Editor.LoadHtml(string.Format(CultureInfo.InvariantCulture, TableHtml, width));
        EditorHost.Pump();

        return Assert.Single(host.Document.Blocks.OfType<Table>()).Width;
    }

    [AvaloniaFact]
    public void LoadHtml_TableWidth_IsReadCultureInvariant()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            double invariantWidth = LoadedTableWidth("100.5");
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal(invariantWidth, LoadedTableWidth("100.5"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}
