using System.Text;
using System.Xml;

namespace AvRichTextBox;

internal static class CustomContentXaml
{
    internal const string Namespace = "urn:avrichtextbox:custom-content";

    internal static bool IsInline(XmlNode node) => node.LocalName == "Inline" && node.NamespaceURI == Namespace;
    internal static bool IsBlock(XmlNode node) => node.Attributes?["Block", Namespace]?.Value == "true";

    internal static string Write(CustomInline inline)
    {
        StringBuilder text = new();
        using (var writer = XmlWriter.Create(text, new XmlWriterSettings { OmitXmlDeclaration = true, NewLineHandling = NewLineHandling.Entitize }))
        {
            writer.WriteStartElement("custom", "Inline", Namespace);
            writer.WriteAttributeString("Type", inline.Definition.Type);
            writer.WriteAttributeString("Data", inline.Definition.Data);
            writer.WriteAttributeString("Text", inline.Definition.FallbackText);
            writer.WriteEndElement();
        }
        return text.ToString();
    }

    internal static CustomInline Read(XmlNode node) => new(new CustomContent(
        node.Attributes?["Type"]?.Value ?? throw new FormatException("Custom content requires a Type attribute."),
        node.Attributes?["Data"]?.Value ?? "",
        node.Attributes?["Text"]?.Value ?? ""));
}
