using Avalonia;
using Avalonia.Media;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System;
using System.Diagnostics;
using System.Linq;
using static AvRichTextBox.HelperMethods;

namespace AvRichTextBox;

internal static partial class WordConversions
{
   internal static MainDocumentPart? mainDocPart;
   internal static string DefaultEastAsiaFont = "";
   internal static string DefaultAsciiFont = "";

   internal static void GetFlowDocument(MainDocumentPart mDocPart, FlowDocument fdoc)
   {

      try
      {

         mainDocPart = mDocPart;

         //Implement doc-wide properties someday:

         // Dim docVariablesList As List(Of DocumentVariables) = mainDocPart.DocumentSettingsPart.Settings.Descendants(Of DocumentVariables)().ToList()
         // 'foreach (DocumentVariables docVars in docVariablesList)
         // '    foreach (DocumentVariable docVar in docVars)
         //mainDocPart.DocumentSettingsPart.Settings

         //StyleDefinitionsPart? styles = mainDocPart.StyleDefinitionsPart!;
         //if (styles != null)
         //{
         //   var defParProps = styles.Styles!.DocDefaults!.ParagraphPropertiesDefault;

         //   //if (defParProps.ParagraphPropertiesBaseStyle != null)
         //   //{
         //   //   if (defParProps.ParagraphPropertiesBaseStyle.SpacingBetweenLines == null)
         //   //      fdoc.LineHeight = 50;
         //   //   else
         //   //      fdoc.LineHeight = TwipToPix(Convert.ToDouble(defParProps.ParagraphPropertiesBaseStyle.SpacingBetweenLines.Line.Value));
         //   //}

         //   //var runFonts = styles.Styles.DocDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.RunFonts;

         //if (runFonts != null)
         //{
         //   DefaultAsciiFont = (runFonts.Ascii == null) ? "Times New Roman" : runFonts.Ascii.Value;
         //   DefaultEastAsiaFont = (runFonts.EastAsia == null) ? "ＭＳ 明朝" : runFonts.EastAsia.Value;
         //   if (DefaultEastAsiaFont == "Mincho") DefaultEastAsiaFont = "ＭＳ 明朝";
         //   fdoc.FontFamily = new FontFamily(DefaultAsciiFont + ", " + DefaultEastAsiaFont);
         //}

         //   //foreach (DocumentFormat.OpenXml.Wordprocessing.Style dStyle in styles.Styles.Descendants<DocumentFormat.OpenXml.Wordprocessing.Style>())
         //   //{
         //   //   if (dStyle.StyleName.Val == "Normal")
         //   //      fdoc.FontSize = (dStyle.StyleRunProperties == null || dStyle.StyleRunProperties.FontSize == null) ? 18 : PointsToPixels(Convert.ToDouble(dStyle.StyleRunProperties.FontSize.Val)) / 2;
         //   //}
         //}

         fdoc.PagePadding = new Thickness(50); //set a small default padding

         DocumentFormat.OpenXml.Wordprocessing.PageMargin? pMarg = mainDocPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.PageMargin>().FirstOrDefault()!;

         if (pMarg != null)
         {
            double docmargT = TwipToPix(Convert.ToDouble((int)pMarg.Top!));
            double docmargR = TwipToPix(Convert.ToDouble((uint)pMarg.Right!));
            double docmargB = TwipToPix(Convert.ToDouble((int)pMarg.Bottom!));
            double docmargL = TwipToPix(Convert.ToDouble((uint)pMarg.Left!));
            fdoc.PagePadding = new Thickness(docmargL, docmargT, docmargR, docmargB);
         }

         //DocumentFormat.OpenXml.Wordprocessing.PageSize pSize = mainDocPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.PageSize>().FirstOrDefault();
         //if (pSize != null)
         //{
         //   fdoc.PageWidth = TwipToPix(Convert.ToDouble((uint)pSize.Width));
         //   fdoc.PageHeight = TwipToPix(Convert.ToDouble((uint)pSize.Height));
         //}


         OpenXmlElement? docBody = mainDocPart.Document.Body!;

         if (docBody != null)
         {
            foreach (OpenXmlElement section in docBody.Elements())
            {  //MessageBox.Show(section.LocalName);

               switch (section.LocalName)
               {
                  case "sectPr":

                     //DocumentFormat.OpenXml.Wordprocessing.PageMargin? spMarg = section.Descendants<DocumentFormat.OpenXml.Wordprocessing.PageMargin>().FirstOrDefault();
                     if (pMarg != null)
                     {
                        double docmargT = TwipToPix(Convert.ToDouble((int)pMarg.Top!));
                        double docmargR = TwipToPix(Convert.ToDouble((uint)pMarg.Right!));
                        double docmargB = TwipToPix(Convert.ToDouble((int)pMarg.Bottom!));
                        double docmargL = TwipToPix(Convert.ToDouble((uint)pMarg.Left!));
                        fdoc.PagePadding = new Thickness(docmargL, docmargT, docmargR, docmargB);
                     }

                     //DocumentFormat.OpenXml.Wordprocessing.PageSize spSize = section.Descendants<DocumentFormat.OpenXml.Wordprocessing.PageSize>().FirstOrDefault();
                     //if (pSize != null)
                     //{
                     //   fdoc.PageWidth = TwipToPix(Convert.ToDouble((uint)pSize.Width));
                     //   fdoc.PageHeight = TwipToPix(Convert.ToDouble((uint)pSize.Height));
                     //}

                     //DocumentFormat.OpenXml.Wordprocessing.SpacingBetweenLines spLineSpacing = section.Descendants<DocumentFormat.OpenXml.Wordprocessing.SpacingBetweenLines>().FirstOrDefault();
                     //// var linespacingRuleelm As OpenXmlElement = section.ChildElements.Where(Function(ce) ce.LocalName = "lineRule")(0)

                     //if (spLineSpacing != null)
                     //{
                     //   //MessageBox.Show("section linespacing not null");
                     //   fdoc.LineHeight = TwipToPix(Convert.ToDouble(spLineSpacing.Line));
                     //}

                     break;


                  case "tbl":

                     //DocumentFormat.OpenXml.Wordprocessing.Table wtable = (DocumentFormat.OpenXml.Wordprocessing.Table)section;

                     //System.Windows.Documents.Table newTable = GetTable(wtable);
                     ////System.Windows.Documents.Table newTable = GetTable(section);

                     //foreach (TableRow tr in newTable.RowGroups[0].Rows)
                     //{
                     //   foreach (TableCell cel in tr.Cells)
                     //   {
                     //      cel.BorderBrush = Brushes.Black;
                     //      cel.BorderThickness = new Thickness(0.3);
                     //      cel.Padding = new Thickness(10);
                     //      cel.LineHeight = fdoc.LineHeight;
                     //   }
                     //}

                     //fdoc.Blocks.Add(newTable);
                     break;

                  case "p":

                     try
                     {
                        Paragraph para = GetParagraph(section);
                        //para.FontFamily = fdoc.FontFamily;
                        para.Margin = new Thickness(0);
                        if (para.Inlines.Count == 0)
                           para.Inlines.Add(new EditableRun(""));
                        fdoc.Blocks.Add(para);
                     }
                     catch (Exception paraEx) { Debug.WriteLine($"Could not get paragraph:\n{paraEx.Message}"); }

                     break;

                  default:
                     Debug.WriteLine("no section name");
                     break;

               }
            }

         }

      }
      catch (Exception ex) { Debug.WriteLine($"Error getting flow doc: \n{ex.Message}"); }

   }


}


