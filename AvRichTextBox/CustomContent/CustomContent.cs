namespace AvRichTextBox;

/// <summary>Serializable application content, independent of the control used to display it.</summary>
public sealed record CustomContent
{
    public string Type { get; }
    public string Data { get; }
    public string FallbackText { get; }

    public CustomContent(string type, string data, string fallbackText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(fallbackText);
        Type = type;
        Data = data;
        FallbackText = fallbackText;
    }
}
