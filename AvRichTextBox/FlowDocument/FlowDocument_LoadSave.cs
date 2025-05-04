using DocumentFormat.OpenXml.Packaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using static AvRichTextBox.WordConversions;
using static AvRichTextBox.RtfConversions;
using static AvRichTextBox.XamlConversions;
using RtfDomParserAv;
using System.Text;
using HtmlAgilityPack;

namespace AvRichTextBox;

public partial class FlowDocument
{
	internal void LoadRtf(string rtfContent)
	{
		RTFDomDocument rtfdom = new();
		//rtfdom.Load(fileName);

		// Do this to fix malformed `\o "` and orphaned quotes
		if (rtfContent.Contains("\\o "))
		{
			rtfContent = rtfContent.Replace("\\o \"}", "\\o \"\"}").Replace(" \"}", " }");
			rtfContent = Regex.Replace(rtfContent, "\\\\o \".*?\"", "\\o\"\"");
		}
		using MemoryStream rtfStream = new(Encoding.UTF8.GetBytes(rtfContent));
		using StreamReader streamReader = new(rtfStream);

		rtfdom.Load(streamReader.BaseStream);

		try
		{
			ClearDocument();
			GetFlowDocumentFromRtf(rtfdom!, this);
			InitializeDocument();
		}
		catch (Exception ex2) { Debug.WriteLine($"error getting flow doc:\n{ex2.Message}"); }
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
			//string rtfText = GetRtfFromFlowDocumentBlocks(this.Blocks);
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

	internal void LoadXamlPackage(string fileName)
	{

		XamlConversions.LoadXamlPackage(fileName, this);

		InitializeDocument();

	}


	internal void SaveXamlPackage(string fileName)
	{
		XamlConversions.SaveXamlPackage(fileName, this);
	}

}


