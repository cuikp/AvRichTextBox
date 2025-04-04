using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using static AvRichTextBox.HelperMethods;
using static AvRichTextBox.WordConversions;
using static AvRichTextBox.RtfConversions;
using static AvRichTextBox.XamlConversions;
using RtfDomParser;
using System.Text;
using System.Threading.Tasks;

namespace AvRichTextBox;

public partial class FlowDocument
{
   internal void LoadRtf(string fileName)
   {
      try
      {
         RTFDomDocument rtfdom = new();

         //rtfdom.Load(fileName);

         // Do this to fix malformed `\o "` and orphaned quotes
         string rtfContent = File.ReadAllText(fileName);
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
         catch (Exception ex2) { Debug.WriteLine("error getting flow doc:\n" + ex2.Message); }

      }


      catch (Exception ex3)
      {
         if (ex3.HResult == -2147024864)
            throw new IOException("The file:\n" + fileName + "\ncannot be opened because it is currently in use by another application.", ex3);
         else
            Debug.WriteLine("Error trying to open file: " + ex3.Message);
      }

   }

   internal void SaveRtf(string fileName)
   {
      try
      {
         //string rtfText = GetRtfFromFlowDocumentBlocks(this.Blocks);
         string rtfText = GetRtfFromFlowDocument(this);
         File.WriteAllText(fileName, rtfText, Encoding.Default);
         //Debug.WriteLine(rtfText);
      }
      catch (Exception ex2) { Debug.WriteLine("error getting flow doc:\n" + ex2.Message); }


   }


   internal void SaveXaml(string fileName)
   {
      File.WriteAllText(fileName, GetDocXaml(false, this));
   }

   internal void LoadXaml(string fileName)
   {
      string xamlDocString = File.ReadAllText(fileName);
      ProcessXamlString(xamlDocString, this);
      InitializeDocument();
   }

   internal void SaveWordDoc(string fileName)
   {
      WordConversions.SaveWordDoc(fileName, this);
   }

   internal void LoadWordDoc(string fileName)
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
            throw new IOException("The file:\n" + fileName + "\ncannot be opened because it is currently in use by another application.", ex3);
         else
            Debug.WriteLine("Error trying to open file: " + ex3.Message);
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


