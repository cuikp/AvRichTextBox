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
using RtfDomParser;
using ReactiveUI;


namespace AvRichTextBox;

public partial class FlowDocument
{


   internal void LoadRtf(string fileName)
   {
      RTFDomDocument rtfdom = new ();
      rtfdom.Load(fileName);
   
      try
      {
         ClearDocument();
         GetFlowDocumentFromRtf(rtfdom!, this);
         InitializeDocument();
      }
      catch (Exception ex2) { Debug.WriteLine("error getting flow doc:\n" + ex2.Message); }


   }

   internal static void TestBuildRTF(RTFWriter w)
   {
      w.Encoding = System.Text.Encoding.GetEncoding(936);
      // write header
      w.WriteStartGroup();
      w.WriteKeyword("rtf1");
      w.WriteKeyword("ansi");
      w.WriteKeyword("ansicpg" + w.Encoding.CodePage);
      // wirte font table
      w.WriteStartGroup();
      w.WriteKeyword("fonttbl");
      w.WriteStartGroup();
      w.WriteKeyword("f0");
      w.WriteText("Arial;");
      w.WriteEndGroup();
      w.WriteStartGroup();
      w.WriteKeyword("f1");
      w.WriteText("Times New Roman;");
      w.WriteEndGroup();
      w.WriteEndGroup();
      // write color table
      w.WriteStartGroup();
      w.WriteKeyword("colortbl");
      w.WriteText(";");
      w.WriteKeyword("red0");
      w.WriteKeyword("green0");
      w.WriteKeyword("blue255");
      w.WriteText(";");
      w.WriteEndGroup();
      // write content
      w.WriteKeyword("qc"); // set alignment center
      w.WriteKeyword("f0"); // set font
      w.WriteKeyword("fs30"); // set font size
      w.WriteText("This is the first paragraph text ");
      w.WriteKeyword("cf1"); // set text color
      w.WriteText("Arial ");
      w.WriteKeyword("cf0"); // set default color
      w.WriteKeyword("f1"); // set font
      w.WriteText("Align center ABC12345");
      w.WriteKeyword("par"); // new paragraph
      w.WriteKeyword("pard"); // clear format
      w.WriteKeyword("f1"); // set font 
      w.WriteKeyword("fs20"); // set font size
      w.WriteKeyword("cf1");
      w.WriteText("This is the secend paragraph Arial left alignment ABC12345");
      // finish
      w.WriteEndGroup();
   }

   internal void SaveRtf(string fileName)
   {
      try
      {

         RTFWriter writer = new ("D:\\asdfasdfasdf.rtf");

         TestBuildRTF(writer);

         writer.Close();

         //RTFReader rTFReader = new RTFReader();
         //rTFReader.

         //RTFRawDocument rtfraw = new RTFRawDocument();
         //RTFNode baseNode = new RTFNode(RTFNodeType.Root, "rtf");
         //rtfraw.AppendChild(baseNode);

         //RTFNode parnode = new RTFNodeGroup();
         //rtfraw.AppendChild(parnode);

         //rtfraw.AppendChild(parnode);
         ////RTFNodeGroup group = new RTFNodeGroup();
         ////group.Nodes.
         //rtfraw.AppendChild(new RTFNode(RTFNodeType.Text, "b"));
         //rtfraw.Save("D:\\asdfasdfasdf.rtf");
         
         //RTFDomDocument rtfdom = GetRtfFromFlowDocument(this);
         //Debug.WriteLine("rtfdom=" + rtfdom.ToDomString() + "\n\n\n" + rtfdom.ToString());
         //baseNode.OwnerDocument = rtfdom;

         //Debug.WriteLine("gner-" + rtfdom.);

         //RTFDocumentWriter writer = new RTFDocumentWriter(fileName);
         //writer.WriteStartDocument();
         //writer.WriteStartParagraph(new DocumentFormatInfo() { Align = RTFAlignment.Left });
         //writer.WriteString("hello", new DocumentFormatInfo() { Bold = true, Underline = true });
         //writer.WriteEndParagraph();
         //writer.WriteEndDocument();
         //writer.Close();
         
         //File.WriteAllText(fileName, rtfraw.wr);
      }
      catch (Exception ex2) { Debug.WriteLine("error getting flow doc:\n" + ex2.Message); }


   }


   internal void SaveXaml(string fileName)
   {
      File.WriteAllText(fileName, GetDocXaml(false));
   }

   internal void LoadXaml(string fileName)
   {
      string xamlDocString = File.ReadAllText(fileName);
      ProcessXamlString(xamlDocString);
      InitializeDocument();
   }

   internal void SaveWordDoc(string fileName)
   {
      WordConversions.SaveWordDoc(fileName, this);
   }

   internal void LoadWordDoc(string fileName)
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

   List<Bitmap> consecutiveImageBitmaps = [];

   internal void LoadXamlPackage(string fileName)
   {
      using (FileStream fstream = new (fileName, FileMode.Open))
      {
         using ZipArchive zipArchive = new (fstream, ZipArchiveMode.Read);
         string EntryXamlDocumentName = "";
         ZipArchiveEntry? relsEntry = zipArchive.GetEntry("_rels/.rels");
         if (relsEntry != null)
         {
            using Stream s = relsEntry.Open();
            byte[] relsBytes = new byte[(int)relsEntry.Length];
            s.Read(relsBytes, 0, relsBytes.Length);
            string relString = System.Text.Encoding.UTF8.GetString(relsBytes);
            string RelationshipEntryLine = @"<Relationship Type=.*?/xaml/entry.*?/>";
            Match relLine = Regex.Match(relString, RelationshipEntryLine);
            Match m = Regex.Match(relLine.Value, @"(?<=Target="").*?(?="")");
            EntryXamlDocumentName = m.Value.TrimStart('/');
         }

         //Get all sequentially numbered images for file
         if (EntryXamlDocumentName != "")
         {

            List<ZipArchiveEntry> imageEntries = zipArchive.Entries.Where(ent => Regex.IsMatch(ent.FullName, @"Xaml/Image[0-9]{1,}\.png")).ToList();
            for (int i = 1; i <= imageEntries.Count; i++)
            {
               ZipArchiveEntry? imageEntry = zipArchive.GetEntry("Xaml/Image" + i.ToString() + ".png");
               if (imageEntry != null)
               {
                  try
                  {
                     using Stream s = imageEntry.Open();
                     MemoryStream memStream = new ();
                     s.CopyTo(memStream);
                     memStream.Position = 0;
                     consecutiveImageBitmaps.Add(new Bitmap(memStream));
                  }
                  catch { consecutiveImageBitmaps.Add(null!); Debug.WriteLine("png file in package could not be gotten.: " + imageEntry.FullName); }
               }
            }


            //Get the Docxaml data
            ZipArchiveEntry? xamlDocEntry = zipArchive.GetEntry(EntryXamlDocumentName);

            if (xamlDocEntry != null)
            {
               using Stream docStream = xamlDocEntry.Open();
               byte[] xamlDocBytes = new byte[xamlDocEntry.Length];
               int totalBytesRead = 0;
               while (totalBytesRead < xamlDocBytes.Length)
               {
                  int bytesRead = docStream.Read(xamlDocBytes, totalBytesRead, xamlDocBytes.Length - totalBytesRead);
                  if (bytesRead == 0)
                     throw new InvalidOperationException("End of stream reached before finished reading.");
                  totalBytesRead += bytesRead;
               }
               string xamlDocString = System.Text.Encoding.UTF8.GetString(xamlDocBytes);
               ProcessXamlString(xamlDocString);

            }

         }
      }

      InitializeDocument();

   }


   internal void SaveXamlPackage(string fileName)
   {
      using FileStream fstream = new(fileName, FileMode.Create);
      using ZipArchive zipArchive = new (fstream, ZipArchiveMode.Create);
      var resource = AssetLoader.Open(new Uri(@"avares://AvRichTextBox/XamlPackageData/.rels"));
      var reader = new StreamReader(resource);
      ZipArchiveEntry relsEntry = zipArchive.CreateEntry("_rels/.rels");
      byte[] relsBytes = System.Text.Encoding.UTF8.GetBytes(reader.ReadToEnd());
      using (var s = relsEntry.Open())
      { s.Write(relsBytes, 0, relsBytes.Length); }

      resource = AssetLoader.Open(new Uri(@"avares://AvRichTextBox/XamlPackageData/[Content_Types].xml"));
      reader = new StreamReader(resource);
      ZipArchiveEntry contentsEntry = zipArchive.CreateEntry("[Content_Types].xml");
      byte[] contentsBytes = System.Text.Encoding.UTF8.GetBytes(reader.ReadToEnd());
      using (var s = contentsEntry.Open())
      { s.Write(contentsBytes, 0, contentsBytes.Length); }


      //Save images, if any  
      List<Paragraph> imageContainingParagraphs = Blocks.Where(b => b.IsParagraph && ((Paragraph)b).Inlines.Where(iline =>
          iline.GetType() == typeof(EditableInlineUIContainer) && ((EditableInlineUIContainer)iline).Child.GetType() == typeof(Image)).Any()).ToList().ConvertAll(bb => (Paragraph)bb);

      if (imageContainingParagraphs.Any())
      {
         int imageNo = 0; //Consecutively define image nos.
         List<UniqueBitmap> uniqueBitmaps = [];
         foreach (Paragraph p in imageContainingParagraphs)
         {
            foreach (EditableInlineUIContainer imageUIContainer in p.Inlines.Where(iline => iline.GetType() == typeof(EditableInlineUIContainer) &&
                       ((EditableInlineUIContainer)iline).Child.GetType() == typeof(Image)))
            {
               Image? thisImg = imageUIContainer.Child as Image;

               Bitmap imgbitmap = (Bitmap)thisImg!.Source!;

               //Debug.WriteLine("Imagesource is null ? : " + (thisImg.Source == null));
               if (imgbitmap == null)
               {
                  //Create dummy bitmap to maintain doc/image structure
                  imageNo += 1;
                  Bitmap dummyBMP = new RenderTargetBitmap(new PixelSize(10, 10));
                  uniqueBitmaps.Add(new UniqueBitmap(dummyBMP, (int)thisImg.Width, (int)thisImg.Height, imageNo));
                  imageUIContainer.ImageNo = imageNo;
               }
               else
               {
                  UniqueBitmap foundUniqueBitmap = uniqueBitmaps.Where(bmp => bmp.uBitmap == imgbitmap).FirstOrDefault()!;
                  if (foundUniqueBitmap == null)
                  {  //add as new unique bitmap
                     imageNo += 1;
                     uniqueBitmaps.Add(new UniqueBitmap(imgbitmap, (int)thisImg.Width, (int)thisImg.Height, imageNo));
                     imageUIContainer.ImageNo = imageNo;
                  }
                  else
                  {
                     if (foundUniqueBitmap.maxWidth < thisImg.Width)
                        foundUniqueBitmap.maxWidth = (int)thisImg.Width;
                     if (foundUniqueBitmap.maxHeight < thisImg.Height)
                        foundUniqueBitmap.maxHeight = (int)thisImg.Height;
                     imageUIContainer.ImageNo = foundUniqueBitmap.consecutiveIndex;
                  }

               }
            }
         }

         //Write all unique bitmaps to zip
         imageNo = 1;
         foreach (UniqueBitmap uB in uniqueBitmaps)
         {
            //if (uB.uBitmap != null)
            //{
            var memoryStream = new MemoryStream();
            if (uB.uBitmap != null)
               ResizeAndSaveBitmap(uB.uBitmap, uB.maxWidth, uB.maxHeight, memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            string imageName = "Xaml/Image" + imageNo.ToString() + ".png";
            ZipArchiveEntry imageEntry = zipArchive.CreateEntry(imageName);
            using (var s = imageEntry.Open())
            {
               byte[] imgbytes = new byte[memoryStream.Length];
               memoryStream.Read(imgbytes, 0, imgbytes.Length);
               s.Write(imgbytes, 0, imgbytes.Length);
            }

            //}
            imageNo++;
         }
      }

      ZipArchiveEntry docEntry = zipArchive.CreateEntry("Xaml/Document.xaml");
      using (var s = docEntry.Open())
      {
         byte[] docBytes = System.Text.Encoding.UTF8.GetBytes(GetDocXaml(true));
         s.Write(docBytes, 0, docBytes.Length);
      }

      //Debug.WriteLine("done");


   }


}


