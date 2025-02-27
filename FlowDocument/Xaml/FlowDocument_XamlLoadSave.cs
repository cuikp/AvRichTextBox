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
using System.Text;


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

   internal void SaveRtf(string fileName)
   {
      try
      {
         string rtfText = GetRtfFromFlowDocumentBlocks(this.Blocks);
         File.WriteAllText(fileName, rtfText, Encoding.Default);
         //Debug.WriteLine(rtfText);
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

            List<ZipArchiveEntry> imageEntries = zipArchive.Entries.Where(ent => FindXamlImageEntriesRegex().IsMatch(ent.FullName)).ToList();
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

      if (imageContainingParagraphs.Count != 0)
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

   [GeneratedRegex(@"Xaml/Image[0-9]{1,}\.png")]
   public static partial Regex FindXamlImageEntriesRegex();
}


