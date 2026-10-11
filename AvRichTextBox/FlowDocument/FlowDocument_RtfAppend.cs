namespace AvRichTextBox;

// Headless RTF append: joins two RTF documents through the RTF DOM reader (LoadRtf) and writes RTF again
// (SaveRtf), so the formatting is kept (no plain-text round trip).
public partial class FlowDocument
{
    // insertNewLine=false continues the last paragraph of the base document without a paragraph break,
    // insertNewLine=true appends with exactly ONE paragraph break (the behavior of RichEdit-based
    // "append RTF to RTF" helpers).
    public static string AppendRtfToRtf(string baseRtf, string appendRtf, bool insertNewLine = false)
    {
        FlowDocument fd = new FlowDocument();
        if (!string.IsNullOrEmpty(baseRtf))
        {
            fd.LoadRtf(baseRtf);
        }

        if (!string.IsNullOrEmpty(appendRtf))
        {
            FlowDocument fdAppend = new FlowDocument();
            fdAppend.LoadRtf(appendRtf);

            // Without a break the inlines of the first appended paragraph move into the last base paragraph;
            // only further appended blocks start new paragraphs.
            if (!insertNewLine && fd.Blocks.Count > 0 && fdAppend.Blocks.Count > 0
                && fd.Blocks[fd.Blocks.Count - 1] is Paragraph lastParagraph
                && fdAppend.Blocks[0] is Paragraph firstAppended)
            {
                foreach (IEditable inline in new List<IEditable>(firstAppended.Inlines))
                {
                    firstAppended.Inlines.Remove(inline);
                    lastParagraph.Inlines.Add(inline);
                }

                fdAppend.Blocks.Remove(firstAppended);
            }

            // Take over the blocks of the appended document; the owner (MyFlowDoc) moves to the target document
            // so serialization stays consistent.
            foreach (Block b in new List<Block>(fdAppend.Blocks))
            {
                fdAppend.Blocks.Remove(b);
                b.MyFlowDoc = fd;
                fd.Blocks.Add(b);
            }
        }

        return fd.SaveRtf();
    }
}
