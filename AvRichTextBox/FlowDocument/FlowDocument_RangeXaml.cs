using System.IO;
using static AvRichTextBox.XamlConversions;
using System.Text;


namespace AvRichTextBox;

public partial class FlowDocument
{
   public void SaveRangeToXamlStream(TextRange trange, Stream stream)
   {
      StringBuilder rangeXamlBuilder = new(SectionTextDefault);
      rangeXamlBuilder.Append(GetParagraphRunsXaml(CreateNewInlinesForRange(trange), false));
      rangeXamlBuilder.Append("</Section>");
      byte[] stringBytes = Encoding.UTF8.GetBytes(rangeXamlBuilder.ToString());
      stream.Write(stringBytes, 0, stringBytes.Length);

   }

   internal void LoadXamlStreamIntoRange (Stream stream, TextRange trange)
   {
      byte[] streamBytes = new byte[stream.Length];
      stream.Read(streamBytes, 0, streamBytes.Length);
      string xamlString = Encoding.UTF8.GetString(streamBytes, 0, streamBytes.Length);
      //ProcessXamlString(xamlString);

   }

}


