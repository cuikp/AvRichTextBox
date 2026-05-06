using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Data.SqlTypes;
using System.IO.Compression;
using System.Text;
using System.Xml;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

public partial class XamlConversions
{

   internal static string SectionTextDefault => "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">";
   internal static Uri packageRelsUri = new(@"avares://AvRichTextBox/SaveAndLoadFormats/Xaml/XamlPackageData/.rels");
   internal static Uri contentTypesUri = new(@"avares://AvRichTextBox/SaveAndLoadFormats/Xaml/XamlPackageData/[Content_Types].xml");

   //string ParagraphTextDefault => "<Paragraph LineHeight=\"18.666666666666668\" FontFamily=\"Times New Roman, ‚l‚r –¾’©\" Margin=\"0,0,0,0\" Padding=\"0,0,0,0\">";

   internal static void SaveXamlPackage(string fileName, FlowDocument fdoc)
   {
      try
      {
         using FileStream fstream = new(fileName, FileMode.Create);
         using ZipArchive zipArchive = new(fstream, ZipArchiveMode.Create);
         var resource = AssetLoader.Open(packageRelsUri);
         var reader = new StreamReader(resource);
         ZipArchiveEntry relsEntry = zipArchive.CreateEntry("_rels/.rels");
         byte[] relsBytes = Encoding.UTF8.GetBytes(reader.ReadToEnd());
         using (var s = relsEntry.Open())
         { s.Write(relsBytes, 0, relsBytes.Length); }

         resource = AssetLoader.Open(contentTypesUri);
         reader = new StreamReader(resource);
         ZipArchiveEntry contentsEntry = zipArchive.CreateEntry("[Content_Types].xml");
         byte[] contentsBytes = Encoding.UTF8.GetBytes(reader.ReadToEnd());
         using (var s = contentsEntry.Open())
         { s.Write(contentsBytes, 0, contentsBytes.Length); }


         //Save images, if any  
         List<Paragraph> imageContainingParagraphs = [.. fdoc.AllParagraphs.Where(p => p.Inlines.Where(iline => iline is EditableInlineUIContainer eIUC && eIUC.Child is Image).Any())];

         if (imageContainingParagraphs.Count != 0)
         {
            int imageNo = 0; //Consecutively define image nos.
            List<UniqueBitmap> uniqueBitmaps = [];
            foreach (Paragraph p in imageContainingParagraphs)
            {
               foreach (EditableInlineUIContainer imageUIContainer in p.Inlines.Where(iline => iline.GetType() == typeof(EditableInlineUIContainer) &&
                          ((EditableInlineUIContainer)iline).Child.GetType() == typeof(Image)))
               {
                  if (imageUIContainer.Child is Image thisImg)
                  {

                     Bitmap? imgbitmap = (Bitmap)thisImg.Source!;

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
            byte[] docBytes = Encoding.UTF8.GetBytes(GetDocXaml(true, fdoc));
            s.Write(docBytes, 0, docBytes.Length);
         }

         //Debug.WriteLine("done saving");

      }
      catch (Exception ex) { throw new Exception("Error trying to save Xaml package: (" + ex.Message + ")"); }

   }


   internal static string GetDocXaml(bool isXamlPackage, FlowDocument fdoc)
   {
      XmlDocument xamlDocument = new();
                  
      StringBuilder selXaml = new (SectionTextDefault);

      /////  make more general to use for any textrange 
      //selXaml.Append(GetBlocksXaml(new TextRange(this, 0, this.DocEndPoint)));
      
      foreach (Block block in fdoc.Blocks)
      {
         switch (block)
         {
            case Paragraph par:

               selXaml.Append(GetParagraphXaml(par, isXamlPackage));
               break;

            case Table table:

               selXaml.Append(GetTableXaml(table, isXamlPackage));
               break;
         }
      }

      selXaml.Append("</Section>");

      return selXaml.ToString();

   }

   internal static string GetTableXaml(Table table, bool isXamlPackage)
   {

      StringBuilder tableXaml = new("<Table ");

      
      string TablePropertiesString =
         "CellSpacing=\"0\"" +
         $" Margin=\"{table.Margin}\"" +
         $" Padding=\"0,0,0,0\"" +
         $" TableAlignment=\"{table.TableAlignment}\"" +
         $" BorderThickness=\"{table.BorderThickness}\"" +
         $" BorderBrush=\"{table.BorderBrush}\"";
      tableXaml.Append(TablePropertiesString);
      tableXaml.Append('>');

      tableXaml.Append("<Table.Columns>");

      foreach (ColumnDefinition coldef in table.ColDefs)
      {
         tableXaml.Append($"<TableColumn Width=\"{coldef.Width}\"/>");
      }

      tableXaml.Append("</Table.Columns>");
      tableXaml.Append("<TableRowGroup>");

      for (int rowno = 0; rowno < table.RowDefs.Count; rowno++)
      {
         StringBuilder tableRowString = new("<TableRow>");
         for (int colno = 0; colno < table.ColDefs.Count; colno++)
         {
            if (table.Cells.FirstOrDefault(c => c.RowNo == rowno && c.ColNo == colno) is Cell thisCell)
            {
               StringBuilder cellString = new("<TableCell ");
               string cellPropertiesString =
                  $"ColumnSpan=\"{thisCell.ColSpan}\"" +
                  $" RowSpan=\"{thisCell.RowSpan}\"" +
                  $" Padding=\"{thisCell.Padding}\"" +
                  $" BorderThickness=\"{thisCell.BorderThickness}\"" +
                  $" BorderBrush=\"{thisCell.BorderBrush}\"" +
                  $" CellBackground=\"{thisCell.CellBackground}\"";
               cellString.Append(cellPropertiesString);
               cellString.Append('>');

               if (thisCell.CellContent is Paragraph par)
               {
                  cellString.Append(GetParagraphXaml(par, isXamlPackage));
               }

               cellString.Append("</TableCell>");
               tableRowString.Append(cellString);
            }
         }

         tableRowString.Append("</TableRow>");
         tableXaml.Append(tableRowString);
      }

      tableXaml.Append("</TableRowGroup>");
      tableXaml.Append("</Table>");

      return tableXaml.ToString();

   }


   internal static string GetParagraphXaml(Paragraph par, bool isXamlPackage)
   {
      StringBuilder parXaml = new("<Paragraph ");
      parXaml.Append(
         $"FontFamily=\"{par.FontFamily.Name}\"" +
         $" FontWeight=\"{par.FontWeight}\"" +
         $" FontStyle=\"{par.FontStyle}\"" +
         $" FontSize=\"{par.FontSize}\"" +
         $" Margin=\"{par.Margin}\"" +
         $" Background=\"{par.Background}\"" +
         $" TextAlignment=\"{par.TextAlignment}\"" +
         $" VerticalAlignment=\"{par.VerticalAlignment}\"" +
         ">");

      parXaml.Append(GetParagraphRunsXaml(par.Inlines, isXamlPackage));

      parXaml.Append("</Paragraph>");

      return parXaml.ToString();
   }

   internal static string GetParagraphRunsXaml(IEnumerable<IEditable> parInlines, bool isXamlPackage)
   {
      StringBuilder runXamlBuilder = new();

      foreach (IEditable ied in parInlines)
      {
         switch (ied)
         {
            case EditableRun erun:

               StringBuilder RunHeader = new();
               if (erun is EditableHyperlink elink)
               {
                  RunHeader.Append("<Hyperlink ");
                  RunHeader.Append($"NavigateUri=\"{elink.NavigateUri}\">");
               }

               RunHeader.Append("<Run ");
               RunHeader.Append(GetRunAttributesString(erun));
               runXamlBuilder.Append(RunHeader);
               runXamlBuilder.Append(erun.Text!.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"));
               runXamlBuilder.Append("</Run>");

               if (erun is EditableHyperlink)
                  runXamlBuilder.Append("</Hyperlink>");

               break;

            case EditableLineBreak:
               runXamlBuilder.Append("<LineBreak/>");
               break;

            case EditableInlineUIContainer eIUC:

               if (isXamlPackage)
               {
                  string InlineUIHeader = $"<InlineUIContainer FontFamily=\"{eIUC.FontFamily.Name}\" BaselineAlignment=\"{eIUC.BaselineAlignment}\">";
                  runXamlBuilder.Append(InlineUIHeader);

                  if (eIUC.Child.GetType() == typeof(Image))
                  {
                     Image childImage = (Image)eIUC.Child;
                     string ImageHeader = $"<Image Stretch=\"Fill\" Width=\"{childImage.Width}\" Height=\"{childImage.Height}\">";
                     runXamlBuilder.Append(ImageHeader);

                     runXamlBuilder.Append(
                         "<Image.Source>" +
                         $"<BitmapImage UriSource=\"./Image{eIUC.ImageNo}.png\" CacheOption=\"OnLoad\" />" +
                         "</Image.Source>"
                     );

                     runXamlBuilder.Append("</Image>");
                  }

                  runXamlBuilder.Append("</InlineUIContainer>");
               }

               break;
         }
      }

      return runXamlBuilder.ToString();
   }


   internal static string GetRunAttributesString(EditableRun erun)
   {
      StringBuilder attSB = new ();

      attSB.Append($"FontFamily=\"{erun.FontFamily}\"");
      attSB.Append($" FontWeight=\"{erun.FontWeight}\"");
      attSB.Append($" FontSize=\"{erun.FontSize}\"");
      attSB.Append($" FontStyle=\"{erun.FontStyle}\"");
      attSB.Append($" FontStretch=\"{erun.FontStretch}\"");
      attSB.Append($" BaselineAlignment=\"{erun.BaselineAlignment}\"");

      if (erun.Foreground != null)
         attSB.Append($" Foreground=\"{erun.Foreground}\"");
      if (erun.Background != null)
         attSB.Append($" Background=\"{erun.Background}\"");

      if (erun.TextDecorations != null)
      {
         string textDecString = " TextDecorations=\"";
         foreach (TextDecoration tdec in erun.TextDecorations)
         {
            switch (tdec.Location)
            {
               case TextDecorationLocation.Underline:
                  textDecString += "Underline, ";
                  break;
               case TextDecorationLocation.Strikethrough:
                  textDecString += "Strikethrough, ";
                  break;
               case TextDecorationLocation.Overline:
                  textDecString += "Overline, ";
                  break;
            }
         }
         textDecString = textDecString.TrimEnd([' ', ',']) + "\"";
         attSB.Append(textDecString);
      }

      
      //Closing bracket
      attSB.Append('>');

      return attSB.ToString();
   }



}


