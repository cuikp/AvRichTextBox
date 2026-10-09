using DocumentFormat.OpenXml.Packaging;
using DynamicData;
using HtmlAgilityPack;
using RtfDomParserAv;
using System.Text;
using System.Text.RegularExpressions;
using static AvRichTextBox.RtfConversions;
using static AvRichTextBox.WordConversions;
using static AvRichTextBox.XamlConversions;

namespace AvRichTextBox;

public partial class FlowDocument
{
   [GeneratedRegex("\\\\o \".*?\"")]
   private static partial Regex RemoveOverstrikeRegex();

   [GeneratedRegex(@"^(\s*\{\\rtf\d+)\b", RegexOptions.CultureInvariant)]
   private static partial Regex RtfHeaderRegex();

   [GeneratedRegex(@"\\deff(\d+)", RegexOptions.CultureInvariant)]
   private static partial Regex DefaultFontRegex();

   [GeneratedRegex(@"\\plain(?![a-zA-Z0-9])", RegexOptions.CultureInvariant)]
   private static partial Regex PlainRegex();

   internal void LoadRtf(string rtfContent)
	{
      RTFDomDocument rtfdom = new();

      // The parser reads text only after a paragraph or formatting control word. RTF starts with the default
      // paragraph format anyway; \pard makes it explicit without adding a paragraph or changing later formatting.
      rtfContent = RtfHeaderRegex().Replace(rtfContent, "$1\\pard", 1);
      rtfContent = SelectDefaultFont(rtfContent);

		// Do this to fix malformed `\o "` (overstrike) with/without orphaned quotes (old rtf feature that can break parsing)
		if (rtfContent.Contains("\\o "))
		{
			rtfContent = rtfContent.Replace("\\o \"}", "\\o \"\"}").Replace(" \"}", " }");
			rtfContent = RemoveOverstrikeRegex().Replace(rtfContent, "\\o\"\"");
		}
		rtfContent = NormalizeCharacterEscapes(PrepareListText(rtfContent));
		using MemoryStream rtfStream = new(Encoding.UTF8.GetBytes(rtfContent));
		using StreamReader streamReader = new(rtfStream);

		rtfdom.Load(streamReader.BaseStream);

		try
		{
			ClearDocument();
			GetFlowDocumentFromRtf(rtfdom!, this);
			InitializeDocument();
		}
		catch (Exception ex2)
		{
			Debug.WriteLine($"error getting flow doc:\n{ex2.Message}");
			// The document is already cleared: without a paragraph the editor accepts no input.
			InitializeDocument();
		}
	}

   // The parser ignores \deffN: text without an explicit \f got the parser's own default font. Select the default
   // font explicitly where RTF falls back to it: after the font table (start of the body) and after every \plain.
   internal static string SelectDefaultFont(string rtfContent)
   {
      Match defaultFont = DefaultFontRegex().Match(rtfContent);
      int tableStart = rtfContent.IndexOf("{\\fonttbl", StringComparison.Ordinal);
      if (!defaultFont.Success || tableStart < 0) return rtfContent;

      int tableEnd = -1;
      int depth = 0;
      for (int i = tableStart; i < rtfContent.Length && tableEnd < 0; i++)
      {
         switch (rtfContent[i])
         {
            case '\\': i++; break;
            case '{': depth++; break;
            case '}': if (--depth == 0) tableEnd = i; break;
         }
      }
      if (tableEnd < 0) return rtfContent;

      string fontWord = "\\f" + defaultFont.Groups[1].Value;
      string body = PlainRegex().Replace(rtfContent[(tableEnd + 1)..], "\\plain" + fontWord);
      string delimiter = body.Length > 0 && char.IsLetterOrDigit(body[0]) ? " " : "";
      return rtfContent[..(tableEnd + 1)] + fontWord + delimiter + body;
   }

	internal void LoadRtfFromFile(string fileName)
	{
		try
		{
			string rtfContent = File.ReadAllText(fileName);
			LoadRtf(rtfContent);
		}
		catch (Exception ex3)
		{
			if (ex3.HResult == -2147024864)
				throw new IOException($"The file:\n{fileName}\ncannot be opened because it is currently in use by another application.", ex3);
			else
				Debug.WriteLine($"Error trying to open file: {ex3.Message}");
		}
	}

	internal void SaveRtfToFile(string fileName)
	{
		try
		{
			string rtfText = SaveRtf();
			File.WriteAllText(fileName, rtfText, Encoding.Default);
			//Debug.WriteLine(rtfText);
		}
		catch (Exception ex2) { Debug.WriteLine($"error getting flow doc:\n{ex2.Message}"); }
	}
	
	internal string SaveRtf()
	{
		return GetRtfFromFlowDocument(this);
	}

	private static string NormalizeCharacterEscapes(string rtfContent)
	{
		rtfContent = AnsiHexEscapeRegex().Replace(rtfContent, match =>
		{
			byte ansiByte = Convert.ToByte(match.Groups[1].Value, 16);
			return GetGroupedUnicodeEscape(HelperMethods.GetWindows1252Char(ansiByte));
		});

		return UnicodeEscapeRegex().Replace(rtfContent, match =>
		{
			int unicodeValue = int.Parse(match.Groups[1].Value);
			return @"{\u" + unicodeValue + "?}";
		});
	}

	private static string GetGroupedUnicodeEscape(char c)
	{
		return @"{\u" + (int)c + "?}";
	}

	[GeneratedRegex(@"(?<!\{)\\u(-?\d+)(.)")]
	private static partial Regex UnicodeEscapeRegex();

	[GeneratedRegex(@"\\'([0-9a-fA-F]{2})")]
	private static partial Regex AnsiHexEscapeRegex();


	internal void SaveXamlToFile(string fileName)
	{
		File.WriteAllText(fileName, SaveXaml());
	}

	internal void LoadXamlFromFile(string fileName)
	{
		string xamlDocString = File.ReadAllText(fileName);
		LoadXaml(xamlDocString);
	}

	internal string SaveXaml()
	{
		return GetDocXaml(false, this);
	}

	internal void LoadXaml(string xamlContent)
	{
      ClearDocument();
      ProcessXamlString(xamlContent, this);
		InitializeDocument();
	}

	internal void SaveHtmlDocToFile(string fileName)
	{
		HtmlDocument hdoc = HtmlConversions.GetHtmlFromFlowDocument(this);
		hdoc.Save(fileName);
	}
	
	internal string SaveHtml()
	{
		HtmlDocument hdoc = HtmlConversions.GetHtmlFromFlowDocument(this);
		return hdoc.DocumentNode.OuterHtml;
	}

	internal void LoadHtmlDocFromFile(string fileName)
	{  
      try
		{

			LoadHtml(File.ReadAllText(fileName));
		}
		catch (Exception ex3)
		{
			if (ex3.HResult == -2147024864)
				throw new IOException($"The file:\n{fileName}\ncannot be opened because it is currently in use by another application.\n{ex3.Message}");
			else
				Debug.WriteLine($"Error trying to open file: {ex3.Message}");
		}

	}
	
	internal void LoadHtml(string htmlContent)
	{
		try
		{
			ClearDocument();
			HtmlDocument hdoc = new();
			hdoc.LoadHtml(htmlContent);
			HtmlConversions.GetFlowDocumentFromHtml(hdoc, this);
			InitializeDocument();
		}
		catch (Exception ex2) { Debug.WriteLine("error getting flow doc:\n" + ex2.Message); }
	}


	internal void SaveWordDocToFile(string fileName)
	{
		WordConversions.SaveWordDoc(fileName, this);
	}

	internal void LoadWordDocFromFile(string fileName)
	{
		try
		{
			using WordprocessingDocument WordDoc = WordprocessingDocument.Open(fileName, false);
			try
			{
				ClearDocument();
				GetFlowDocument(WordDoc.MainDocumentPart!, this);
				InitializeDocument();
			}
			catch (Exception ex2) { Debug.WriteLine("error getting flow doc:\n" + ex2.Message); }
		}
		catch (Exception ex3)
		{
			if (ex3.HResult == -2147024864)
				throw new IOException($"The file:\n{fileName}\ncannot be opened because it is currently in use by another application.", ex3);
			else
				Debug.WriteLine($"Error trying to open file: {ex3.Message}");
		}


	}

	internal async void LoadXamlPackage(string fileName)
	{

        ClearDocument();

		if (await XamlConversions.LoadXamlPackage(fileName, this))
		{
			InitializeDocument();
		}
		else
			NewDocument();
		

	}

	internal void SaveXamlPackage(string fileName)
	{
		XamlConversions.SaveXamlPackage(fileName, this);
	}


}


